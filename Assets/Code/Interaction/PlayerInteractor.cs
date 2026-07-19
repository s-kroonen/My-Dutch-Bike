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
        public float tightenPerSecond = 0.6f;

        private LoosePart _heldPart;
        private BikeRideController _mountedBike;
        private Transform _cameraOriginalParent;
        private Vector3 _cameraOriginalLocalPos;
        private Quaternion _cameraOriginalLocalRot;
        private string _prompt = "";

        private void Update()
        {
            _prompt = "";

            if (_mountedBike != null)
            {
                _prompt = "[E] Dismount";
                if (Input.GetKeyDown(KeyCode.E))
                    Dismount();
                return;
            }

            if (!Physics.Raycast(eye.transform.position, eye.transform.forward, out var hit, interactRange))
                return;

            if (_heldPart == null)
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

                var fastener = hit.collider.GetComponent<FastenerPoint>();
                if (fastener != null)
                {
                    var partState = fastener.owner.State.Find(fastener.partDefId);
                    var fastenerState = partState?.fasteners.Find(f => f.fastenerSlotId == fastener.fastenerSlotId);
                    float tightness = fastenerState != null ? fastenerState.tightness : 0f;
                    _prompt = $"[LMB] tighten / [RMB] loosen {fastener.fastenerSlotId} ({tightness:P0})";

                    if (Input.GetMouseButton(0))
                        fastener.owner.SetFastenerTightness(fastener.partDefId, fastener.fastenerSlotId, tightenPerSecond * Time.deltaTime);
                    else if (Input.GetMouseButton(1))
                        fastener.owner.SetFastenerTightness(fastener.partDefId, fastener.fastenerSlotId, -tightenPerSecond * Time.deltaTime);
                    return;
                }

                var bike = hit.collider.GetComponentInParent<BikeAssembly>();
                if (bike != null)
                {
                    _prompt = bike.IsFullyAssembledAndSecured() ? "[E] Mount" : "Bike incomplete";
                    if (Input.GetKeyDown(KeyCode.E))
                        Mount(bike);
                }
            }
            else
            {
                _prompt = $"Holding {_heldPart.definition.displayName} — [Q] drop";

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    Drop();
                    return;
                }

                var socket = hit.collider.GetComponent<SocketMarker>();
                if (socket != null)
                {
                    bool canInstall = socket.owner.CanInstall(_heldPart.definition, socket.socketId, out var reason);
                    _prompt = canInstall ? $"[E] Place on {socket.socketId}" : $"Can't place: {reason}";
                    if (canInstall && Input.GetKeyDown(KeyCode.E))
                        PlaceOnSocket(socket);
                }
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
            var col = loose.GetComponent<Collider>();
            if (col != null)
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
            var col = _heldPart.GetComponent<Collider>();
            if (col != null)
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
