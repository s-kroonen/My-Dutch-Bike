using MyDutchBike.Bike;
using MyDutchBike.Player;
using UnityEngine;

namespace MyDutchBike.Interaction
{
    /// <summary>
    /// First-person raycast interaction: pick up loose parts, place them on matching sockets,
    /// tighten/loosen fasteners with an always-equipped wrench, and mount/dismount a completed bike.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public Camera eye;
        public FirstPersonController firstPerson;
        public Transform holdPoint;
        public float interactRange = 3f;
        [Tooltip("Half-angle (degrees) of the aim cone used to pick which socket a held part snaps to.")]
        public float placeAngleDegrees = 25f;
        [Tooltip("Half-angle (degrees) of the aim cone for targeting fasteners — keep tight so aiming at the frame still lets you mount.")]
        public float fastenerAngleDegrees = 8f;
        public float tightenPerSecond = 0.6f;

        private LoosePart _heldPart;
        private BikeAssembly[] _bikes;
        private BikeRideController _mountedBike;
        private Transform _cameraOriginalParent;
        private Vector3 _cameraOriginalLocalPos;
        private Quaternion _cameraOriginalLocalRot;
        private string _prompt = "";
        private bool _cursorLocked = true;

        private void Awake()
        {
            ApplyCursorLock(true);
            _bikes = FindObjectsByType<BikeAssembly>(FindObjectsInactive.Exclude);
        }

        private void ClearAllHighlights()
        {
            foreach (var bike in _bikes)
            {
                if (bike == null)
                    continue;
                bike.ClearSocketHighlight();
                bike.SetTargetedFastener(null);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _cursorLocked)
                ApplyCursorLock(true);
        }

        private void ApplyCursorLock(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ApplyCursorLock(!_cursorLocked);

            _prompt = "";
            ClearAllHighlights();

            if (_mountedBike != null)
            {
                _prompt = "[E] Dismount";
                if (Input.GetKeyDown(KeyCode.E))
                    Dismount();
                return;
            }

            // Holding a part: placement uses an aim cone (below), so it must run whether or not the
            // forward ray happens to hit the tiny socket collider — handle it before the raycast gate.
            if (_heldPart != null)
            {
                HandleHolding();
                return;
            }

            Vector3 origin = eye.transform.position;
            Vector3 direction = eye.transform.forward;
            bool hasHit = Physics.Raycast(origin, direction, out var hit, interactRange);

            // Picking up a loose part: direct ray hit (loose parts are big and out in the open).
            if (hasHit)
            {
                var loose = hit.collider.GetComponentInParent<LoosePart>();
                if (loose != null)
                {
                    _prompt = $"[E] Pick up {loose.definition.displayName}";
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        PickUp(loose);
                        return;
                    }
                }
            }

            // Tightening: aim-cone (tight, so looking at the frame body still lets you mount) because
            // the fastener colliders are too small/buried to reliably hit with a ray.
            var fastener = FindAimedFastener(origin, direction, out var fastenerBike);
            var installed = hasHit ? hit.collider.GetComponentInParent<InstalledPart>() : null;
            var aimedBike = fastenerBike != null ? fastenerBike
                : installed != null ? installed.owner
                : hasHit ? hit.collider.GetComponentInParent<BikeAssembly>() : null;

            // [LMB]/[RMB] adjust the targeted fastener (works whether building or disassembling).
            string tightenPrompt = null;
            if (fastener != null)
            {
                fastenerBike.SetTargetedFastener(fastener);
                float tightness = fastenerBike.GetFastenerTightness(fastener.partDefId, fastener.fastenerSlotId);
                tightenPrompt = $"[LMB] tighten / [RMB] loosen {fastener.fastenerSlotId} ({tightness:P0})";

                if (Input.GetMouseButton(0))
                    fastenerBike.SetFastenerTightness(fastener.partDefId, fastener.fastenerSlotId, tightenPerSecond * Time.deltaTime);
                else if (Input.GetMouseButton(1))
                    fastenerBike.SetFastenerTightness(fastener.partDefId, fastener.fastenerSlotId, -tightenPerSecond * Time.deltaTime);
            }

            // [E] mounts a finished bike, otherwise removes a fully-loosened part into your hands.
            string actionPrompt = null;
            bool canMount = aimedBike != null && aimedBike.IsFullyAssembledAndSecured();
            if (canMount)
            {
                actionPrompt = "[E] Mount";
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Mount(aimedBike);
                    return;
                }
            }
            else if (installed != null && installed.owner.CanRemove(installed.partDefId, out _))
            {
                actionPrompt = $"[E] Remove {installed.name}";
                if (Input.GetKeyDown(KeyCode.E))
                {
                    var removed = installed.owner.TryRemovePart(installed.partDefId);
                    if (removed != null)
                        PickUp(removed);
                    return;
                }
            }

            if (actionPrompt != null || tightenPrompt != null)
            {
                _prompt = actionPrompt != null && tightenPrompt != null
                    ? $"{actionPrompt}    {tightenPrompt}"
                    : actionPrompt ?? tightenPrompt;
                return;
            }

            if (aimedBike != null && !aimedBike.IsFullyAssembledAndSecured())
                _prompt = $"Incomplete — next: {aimedBike.FirstIncompleteRequirement()}";
        }

        /// <summary>Nearest fastener to the player's aim across all bikes, within a tight cone.</summary>
        private FastenerPoint FindAimedFastener(Vector3 origin, Vector3 direction, out BikeAssembly owner)
        {
            owner = null;
            FastenerPoint best = null;
            float bestAngle = float.MaxValue;

            foreach (var bike in _bikes)
            {
                if (bike == null)
                    continue;

                var point = bike.FindAimedFastener(origin, direction, interactRange, fastenerAngleDegrees, out float angle);
                if (point != null && angle < bestAngle)
                {
                    bestAngle = angle;
                    best = point;
                    owner = bike;
                }
            }

            return best;
        }

        /// <summary>Runs each frame while a part is held: blinks matching open sockets on every bike,
        /// picks the one nearest the player's aim, and installs it on [E].</summary>
        private void HandleHolding()
        {
            var def = _heldPart.definition;
            _prompt = $"Holding {def.displayName} — [Q] drop";

            if (Input.GetKeyDown(KeyCode.Q))
            {
                Drop();
                return;
            }

            Vector3 origin = eye.transform.position;
            Vector3 direction = eye.transform.forward;

            SocketMarker bestSocket = null;
            bool bestInstallable = false;
            string bestReason = "";
            float bestAngle = float.MaxValue;

            foreach (var bike in _bikes)
            {
                if (bike == null)
                    continue;

                var socket = bike.FindAimedSocket(def, origin, direction, interactRange, placeAngleDegrees,
                    out bool installable, out string reason, out float angle);

                // Only blink the socket the player is aiming at if the part can actually go there.
                bike.SetSocketHighlight(def, installable ? socket : null);

                if (socket != null && angle < bestAngle)
                {
                    bestAngle = angle;
                    bestSocket = socket;
                    bestInstallable = installable;
                    bestReason = reason;
                }
            }

            if (bestSocket != null && bestInstallable)
            {
                _prompt = $"[E] Place on {bestSocket.socketId}";
                if (Input.GetKeyDown(KeyCode.E))
                    PlaceOnSocket(bestSocket);
            }
            else if (bestSocket != null)
            {
                _prompt = $"Can't place: {bestReason}";
            }
        }

        private void OnGUI()
        {
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            const float size = 8f, thickness = 2f;
            GUI.DrawTexture(new Rect(cx - size, cy - thickness * 0.5f, size * 2f, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - size, thickness, size * 2f), Texture2D.whiteTexture);

            if (string.IsNullOrEmpty(_prompt))
                return;

            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            style.normal.textColor = Color.white;
            var rect = new Rect(cx - 200f, cy + 16f, 400f, 24f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), _prompt, WithColor(style, Color.black));
            GUI.Label(rect, _prompt, style);
        }

        private static GUIStyle WithColor(GUIStyle style, Color color)
        {
            var copy = new GUIStyle(style);
            copy.normal.textColor = color;
            return copy;
        }

        private void PickUp(LoosePart loose)
        {
            _heldPart = loose;
            // LoosePart's collider(s) live on a child "Visual", so GetComponent on the root misses
            // them — disable every collider under the part or the ray would keep hitting what we hold.
            foreach (var col in loose.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
            var rb = loose.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = true;

            loose.transform.SetParent(holdPoint, false);
            loose.transform.localPosition = Vector3.zero;
            loose.transform.localRotation = Quaternion.identity;
        }

        private void Drop()
        {
            foreach (var col in _heldPart.GetComponentsInChildren<Collider>(true))
                col.enabled = true;
            var rb = _heldPart.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = false;

            _heldPart.transform.SetParent(null, true);
            _heldPart.transform.position = eye.transform.position + eye.transform.forward * 0.6f;
            _heldPart = null;
        }

        private void PlaceOnSocket(SocketMarker socket)
        {
            if (!socket.owner.CanInstall(_heldPart.definition, socket.socketId, out var reason))
            {
                Debug.Log($"Can't place here: {reason}");
                return;
            }

            socket.owner.TryInstallPart(_heldPart.definition, socket.socketId);
            Destroy(_heldPart.gameObject);
            _heldPart = null;
        }

        private void Mount(BikeAssembly bike)
        {
            var ride = bike.GetComponent<BikeRideController>();
            if (ride == null || !ride.TryMount())
                return;

            _mountedBike = ride;
            firstPerson.enabled = false;
            GetComponent<CharacterController>().enabled = false;

            _cameraOriginalParent = eye.transform.parent;
            _cameraOriginalLocalPos = eye.transform.localPosition;
            _cameraOriginalLocalRot = eye.transform.localRotation;

            if (ride.cameraAnchor != null)
            {
                eye.transform.SetParent(ride.cameraAnchor, false);
                eye.transform.localPosition = Vector3.zero;
                eye.transform.localRotation = Quaternion.identity;
            }
        }

        private void Dismount()
        {
            var bikeTransform = _mountedBike.transform;
            _mountedBike.Dismount();
            _mountedBike = null;

            eye.transform.SetParent(_cameraOriginalParent, false);
            eye.transform.localPosition = _cameraOriginalLocalPos;
            eye.transform.localRotation = _cameraOriginalLocalRot;

            transform.position = bikeTransform.position - bikeTransform.forward * 1.5f;
            GetComponent<CharacterController>().enabled = true;
            firstPerson.enabled = true;
        }
    }
}
