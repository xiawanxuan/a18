using UnityEngine;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.PlayerControl
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float acceleration = 10f;
        public float deceleration = 15f;
        public float airControl = 0.5f;

        [Header("Jump")]
        public float jumpForce = 7f;
        public float gravityScale = 2f;
        public int maxJumps = 2;
        public float coyoteTime = 0.1f;
        public float jumpBufferTime = 0.1f;

        [Header("Ground Check")]
        public Transform groundCheck;
        public float groundCheckDistance = 0.2f;
        public LayerMask groundLayer;

        [Header("Rotation")]
        public float rotationSpeed = 10f;

        [Header("Camera")]
        public Transform cameraTarget;
        public float cameraDistance = 5f;
        public float cameraHeight = 2f;
        public float mouseSensitivity = 2f;
        public float minVerticalAngle = -60f;
        public float maxVerticalAngle = 60f;

        private PlayerInput _input;
        private Rigidbody _rigidbody;
        private bool _isGrounded;
        private int _jumpsRemaining;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _cameraPitch;
        private float _cameraYaw;

        public bool IsGrounded => _isGrounded;
        public Vector3 Velocity => _rigidbody.velocity;

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _rigidbody = GetComponent<Rigidbody>();

            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void Start()
        {
            _jumpsRemaining = maxJumps;
            _cameraYaw = transform.eulerAngles.y;

            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform, false);
                groundCheckObj.transform.localPosition = Vector3.down * 0.5f;
                groundCheck = groundCheckObj.transform;
            }
        }

        private void Update()
        {
            UpdateTimers();
            HandleCameraRotation();
            CheckJumpInput();
        }

        private void FixedUpdate()
        {
            CheckGround();
            HandleMovement();
            ApplyCustomGravity();
            HandleJump();
            HandleRotation();
        }

        private void UpdateTimers()
        {
            if (_coyoteTimer > 0f)
                _coyoteTimer -= Time.deltaTime;

            if (_jumpBufferTimer > 0f)
                _jumpBufferTimer -= Time.deltaTime;
        }

        private void CheckGround()
        {
            bool wasGrounded = _isGrounded;

            _isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundCheckDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            );

            if (_isGrounded)
            {
                _jumpsRemaining = maxJumps;
                _coyoteTimer = coyoteTime;
            }
            else if (wasGrounded)
            {
                _coyoteTimer = coyoteTime;
            }
        }

        private void CheckJumpInput()
        {
            if (_input.JumpPressed)
            {
                _jumpBufferTimer = jumpBufferTime;
            }
        }

        private void HandleMovement()
        {
            Vector3 inputDirection = new Vector3(_input.MovementInput.x, 0f, _input.MovementInput.y);

            if (inputDirection.magnitude > 1f)
            {
                inputDirection.Normalize();
            }

            Vector3 worldDirection = Quaternion.Euler(0f, _cameraYaw, 0f) * inputDirection;

            float targetSpeed = moveSpeed * inputDirection.magnitude;
            Vector3 targetVelocity = worldDirection * targetSpeed;

            Vector3 currentVelocity = _rigidbody.velocity;
            currentVelocity.y = 0f;

            float controlFactor = _isGrounded ? 1f : airControl;

            Vector3 velocityChange = targetVelocity - currentVelocity;
            float accelerationRate = inputDirection.magnitude > 0.1f ? acceleration : deceleration;
            velocityChange = Vector3.ClampMagnitude(velocityChange, accelerationRate * controlFactor * Time.fixedDeltaTime);

            _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);

            if (worldDirection.magnitude > 0.1f)
            {
                EventBus.Publish(GameEvents.OnPlayerMoved);
            }
        }

        private void ApplyCustomGravity()
        {
            if (!_isGrounded)
            {
                Vector3 extraGravity = Physics.gravity * (gravityScale - 1f);
                _rigidbody.AddForce(extraGravity, ForceMode.Acceleration);
            }
        }

        private void HandleJump()
        {
            bool canJump = false;

            if (_jumpsRemaining > 0 && _jumpBufferTimer > 0f)
            {
                canJump = true;
            }
            else if (_coyoteTimer > 0f && _jumpBufferTimer > 0f && _jumpsRemaining > 0)
            {
                canJump = true;
            }

            if (canJump)
            {
                Vector3 velocity = _rigidbody.velocity;
                velocity.y = 0f;
                _rigidbody.velocity = velocity;

                _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

                _jumpsRemaining--;
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }
        }

        private void HandleRotation()
        {
            Vector3 inputDirection = new Vector3(_input.MovementInput.x, 0f, _input.MovementInput.y);

            if (inputDirection.magnitude > 0.1f)
            {
                Vector3 worldDirection = Quaternion.Euler(0f, _cameraYaw, 0f) * inputDirection;
                Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        private void HandleCameraRotation()
        {
            if (_input.LookInput.magnitude < 0.01f) return;

            _cameraYaw += _input.LookInput.x * mouseSensitivity;
            _cameraPitch -= _input.LookInput.y * mouseSensitivity;

            _cameraPitch = Mathf.Clamp(_cameraPitch, minVerticalAngle, maxVerticalAngle);
        }

        public Vector3 GetCameraPosition()
        {
            Quaternion rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, cameraHeight, -cameraDistance);
            return transform.position + offset + Vector3.up * cameraHeight;
        }

        public Quaternion GetCameraRotation()
        {
            return Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
        }

        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            _rigidbody.AddForce(force, mode);
        }

        public void ResetVelocity()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
            }
        }
    }
}
