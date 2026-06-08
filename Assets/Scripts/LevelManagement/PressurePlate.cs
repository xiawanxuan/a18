using UnityEngine;
using SoftFluidPuzzle.Core;
using SoftFluidPuzzle.PlayerControl;

namespace SoftFluidPuzzle.LevelManagement
{
    public class PressurePlate : MonoBehaviour, IInteractable
    {
        [Header("Plate Settings")]
        public string plateId = "plate_01";
        public float activationForce = 50f;
        public float pressDepth = 0.2f;
        public float pressSpeed = 5f;

        [Header("Detection")]
        public LayerMask detectionLayers;
        public float detectionRadius = 1f;
        public float detectionHeight = 0.5f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onActivated;
        public UnityEngine.Events.UnityEvent onDeactivated;

        private bool _isPressed;
        private Vector3 _originalPosition;
        private float _currentPressAmount;

        public bool IsPressed => _isPressed;
        public string PlateId => plateId;

        private void Start()
        {
            _originalPosition = transform.position;
        }

        private void FixedUpdate()
        {
            CheckPressure();
            UpdateVisual();
        }

        private void CheckPressure()
        {
            bool wasPressed = _isPressed;

            Collider[] hitColliders = Physics.OverlapBox(
                transform.position + Vector3.up * detectionHeight * 0.5f,
                new Vector3(detectionRadius, detectionHeight * 0.5f, detectionRadius),
                Quaternion.identity,
                detectionLayers
            );

            float totalForce = 0f;

            foreach (Collider collider in hitColliders)
            {
                Rigidbody rb = collider.attachedRigidbody;
                if (rb != null)
                {
                    totalForce += rb.mass * Physics.gravity.magnitude;
                }
                else
                {
                    totalForce += 10f;
                }
            }

            _isPressed = totalForce >= activationForce;

            if (_isPressed && !wasPressed)
            {
                onActivated?.Invoke();
                EventBus.Publish("OnPressurePlateActivated", plateId);
            }
            else if (!_isPressed && wasPressed)
            {
                onDeactivated?.Invoke();
                EventBus.Publish("OnPressurePlateDeactivated", plateId);
            }
        }

        private void UpdateVisual()
        {
            float targetPress = _isPressed ? pressDepth : 0f;

            _currentPressAmount = Mathf.Lerp(
                _currentPressAmount,
                targetPress,
                pressSpeed * Time.fixedDeltaTime
            );

            transform.position = _originalPosition + Vector3.down * _currentPressAmount;
        }

        public bool CanInteract(GameObject interactor)
        {
            return false;
        }

        public void Interact(GameObject interactor)
        {
        }

        public void OnFocusEnter(GameObject interactor)
        {
        }

        public void OnFocusExit(GameObject interactor)
        {
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isPressed ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(
                transform.position + Vector3.up * detectionHeight * 0.5f,
                new Vector3(detectionRadius * 2f, detectionHeight, detectionRadius * 2f)
            );
        }
    }
}
