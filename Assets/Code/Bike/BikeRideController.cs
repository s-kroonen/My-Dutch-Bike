using UnityEngine;

namespace MyDutchBike.Bike
{
    /// <summary>
    /// Simple kinematic ride model, tuned per frame archetype via BikeAssembly.mechanics (ADR-0005).
    /// Not a physics simulation — good enough to feel city vs. race handling differ for the M1 slice.
    /// Camera/player handoff is owned by PlayerInteractor; this only moves the bike.
    /// </summary>
    [RequireComponent(typeof(BikeAssembly))]
    public class BikeRideController : MonoBehaviour
    {
        public Transform cameraAnchor;

        private BikeAssembly _assembly;
        private float _currentSpeed;

        public bool IsMounted { get; private set; }

        private void Awake()
        {
            _assembly = GetComponent<BikeAssembly>();
            enabled = false;
        }

        public bool TryMount()
        {
            if (IsMounted || !_assembly.IsFullyAssembledAndSecured())
                return false;

            IsMounted = true;
            enabled = true;
            return true;
        }

        public void Dismount()
        {
            IsMounted = false;
            enabled = false;
            _currentSpeed = 0f;
        }

        private void Update()
        {
            var mechanics = _assembly.mechanics;
            float topSpeed = mechanics != null ? mechanics.topSpeed : 6f;
            float acceleration = mechanics != null ? mechanics.acceleration : 3f;
            float turnRate = mechanics != null ? mechanics.turnRate : 90f;

            float throttle = Input.GetAxis("Vertical");
            float steer = Input.GetAxis("Horizontal");

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, throttle * topSpeed, acceleration * Time.deltaTime);

            if (Mathf.Abs(_currentSpeed) > 0.01f)
            {
                float turnDirection = Mathf.Sign(_currentSpeed);
                transform.Rotate(Vector3.up, steer * turnRate * turnDirection * Time.deltaTime);
            }

            transform.position += transform.forward * (_currentSpeed * Time.deltaTime);
        }
    }
}
