using UnityEngine;

namespace MyDutchBike.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public Camera eye;
        public float moveSpeed = 4f;
        public float lookSpeed = 2f;
        public float gravity = -9.81f;

        [Header("Crouch")]
        public KeyCode crouchKey = KeyCode.LeftControl;
        [Tooltip("CharacterController height while crouched. Standing height is read from the controller at Awake.")]
        public float crouchHeight = 1.0f;
        [Tooltip("Movement speed multiplier applied while crouched.")]
        public float crouchSpeedMultiplier = 0.5f;
        [Tooltip("How fast (metres/second) the capsule + camera lerp between standing and crouched.")]
        public float crouchTransitionSpeed = 8f;

        [Header("Sprint & Jump")]
        public KeyCode sprintKey = KeyCode.LeftShift;
        [Tooltip("Movement speed multiplier while sprinting (ignored when crouched).")]
        public float sprintMultiplier = 1.7f;
        public KeyCode jumpKey = KeyCode.Space;
        [Tooltip("Upward velocity applied on jump.")]
        public float jumpSpeed = 4.5f;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;

        private float _standHeight;
        private float _currentHeight;
        private Vector3 _standCameraLocalPos;
        private bool _crouched;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (eye == null)
                eye = GetComponentInChildren<Camera>();

            _standHeight = _controller.height;
            _currentHeight = _standHeight;
            if (eye != null)
                _standCameraLocalPos = eye.transform.localPosition;
        }

        private void Update()
        {
            if (!_controller.enabled)
                return;

            float yaw = Input.GetAxis("Mouse X") * lookSpeed;
            _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * lookSpeed, -80f, 80f);
            transform.Rotate(Vector3.up, yaw);
            if (eye != null)
                eye.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

            float speed = UpdateCrouch();
            if (!_crouched && Input.GetKey(sprintKey))
                speed *= sprintMultiplier;

            Vector3 move = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
            move = Vector3.ClampMagnitude(move, 1f) * speed;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -0.5f;
                if (Input.GetKeyDown(jumpKey) && !_crouched)
                    _verticalVelocity = jumpSpeed;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }
            move.y = _verticalVelocity;

            _controller.Move(move * Time.deltaTime);
        }

        /// <summary>Lerps capsule height + camera toward the crouch/stand target and returns the
        /// movement speed to use this frame. Won't stand up if something is directly overhead.</summary>
        private float UpdateCrouch()
        {
            bool wantCrouch = Input.GetKey(crouchKey);
            if (!wantCrouch && _currentHeight < _standHeight - 0.01f && !HasHeadroom())
                wantCrouch = true;
            _crouched = wantCrouch;

            float target = wantCrouch ? crouchHeight : _standHeight;
            _currentHeight = Mathf.MoveTowards(_currentHeight, target, crouchTransitionSpeed * Time.deltaTime);
            _controller.height = _currentHeight;
            _controller.center = new Vector3(0f, _currentHeight * 0.5f, 0f);

            if (eye != null)
            {
                float standY = _standCameraLocalPos.y;
                float crouchY = standY - (_standHeight - crouchHeight);
                float t = Mathf.InverseLerp(crouchHeight, _standHeight, _currentHeight);
                var lp = _standCameraLocalPos;
                lp.y = Mathf.Lerp(crouchY, standY, t);
                eye.transform.localPosition = lp;
            }

            return moveSpeed * (wantCrouch ? crouchSpeedMultiplier : 1f);
        }

        /// <summary>True when there's room above the crouched capsule to stand back up. Ignores
        /// trigger colliders (sockets, the mount volume) so they don't trap the player crouched.</summary>
        private bool HasHeadroom()
        {
            float castDistance = _standHeight - crouchHeight + 0.1f;
            Vector3 origin = transform.position + Vector3.up * crouchHeight;
            return !Physics.Raycast(origin, Vector3.up, castDistance, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
