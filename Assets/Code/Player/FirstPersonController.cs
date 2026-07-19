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

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (eye == null)
                eye = GetComponentInChildren<Camera>();
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

            Vector3 move = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
            move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;

            if (_controller.isGrounded)
                _verticalVelocity = -0.5f;
            else
                _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            _controller.Move(move * Time.deltaTime);
        }
    }
}
