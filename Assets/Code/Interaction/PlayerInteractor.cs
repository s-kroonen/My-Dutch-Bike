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

        private void Update()
        {
            if (_mountedBike != null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                    Dismount();
                return;
            }

            if (!Physics.Raycast(eye.transform.position, eye.transform.forward, out var hit, interactRange))
                return;

            if (_heldPart == null)
            {
                var loose = hit.collider.GetComponentInParent<LoosePart>();
                if (loose != null && Input.GetKeyDown(KeyCode.E))
                {
                    PickUp(loose);
                    return;
                }

                var fastener = hit.collider.GetComponent<FastenerPoint>();
                if (fastener != null)
                {
                    if (Input.GetMouseButton(0))
                        fastener.owner.SetFastenerTightness(fastener.partDefId, fastener.fastenerSlotId, tightenPerSecond * Time.deltaTime);
                    else if (Input.GetMouseButton(1))
                        fastener.owner.SetFastenerTightness(fastener.partDefId, fastener.fastenerSlotId, -tightenPerSecond * Time.deltaTime);
                    return;
                }

                var bike = hit.collider.GetComponentInParent<BikeAssembly>();
                if (bike != null && Input.GetKeyDown(KeyCode.E))
                    Mount(bike);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    Drop();
                    return;
                }

                var socket = hit.collider.GetComponent<SocketMarker>();
                if (socket != null && Input.GetKeyDown(KeyCode.E))
                    PlaceOnSocket(socket);
            }
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
