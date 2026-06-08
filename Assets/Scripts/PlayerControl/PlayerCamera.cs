using UnityEngine;

namespace SoftFluidPuzzle.PlayerControl
{
    public class PlayerCamera : MonoBehaviour
    {
        [Header("References")]
        public PlayerController targetPlayer;

        [Header("Follow Settings")]
        public float followSmoothTime = 0.1f;
        public float rotationSmoothTime = 0.05f;

        [Header("Collision")]
        public bool cameraCollision = true;
        public float collisionRadius = 0.3f;
        public LayerMask collisionLayers;

        private Vector3 _currentVelocity;
        private Vector3 _currentRotationVelocity;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = gameObject.AddComponent<Camera>();
            }
        }

        private void LateUpdate()
        {
            if (targetPlayer == null) return;

            Vector3 targetPosition = targetPlayer.GetCameraPosition();
            Quaternion targetRotation = targetPlayer.GetCameraRotation();

            if (cameraCollision)
            {
                targetPosition = AdjustForCollision(targetPosition, targetPlayer.transform.position + Vector3.up);
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _currentVelocity,
                followSmoothTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSmoothTime * Time.deltaTime * 60f
            );
        }

        private Vector3 AdjustForCollision(Vector3 targetPos, Vector3 pivot)
        {
            Vector3 direction = targetPos - pivot;
            float distance = direction.magnitude;

            if (distance < 0.01f) return targetPos;

            direction.Normalize();

            RaycastHit hit;
            if (Physics.SphereCast(pivot, collisionRadius, direction, out hit, distance, collisionLayers))
            {
                float adjustedDistance = hit.distance - collisionRadius;
                adjustedDistance = Mathf.Max(adjustedDistance, 0.5f);
                return pivot + direction * adjustedDistance;
            }

            return targetPos;
        }

        public void SetTarget(PlayerController player)
        {
            targetPlayer = player;
        }

        public void Shake(float intensity, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(CameraShakeCoroutine(intensity, duration));
        }

        private System.Collections.IEnumerator CameraShakeCoroutine(float intensity, float duration)
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shakeFactor = 1f - (elapsed / duration);
                transform.localPosition = originalPos + Random.insideUnitSphere * intensity * shakeFactor;
                yield return null;
            }

            transform.localPosition = originalPos;
        }
    }
}
