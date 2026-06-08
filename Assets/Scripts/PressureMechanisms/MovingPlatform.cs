using UnityEngine;

namespace SoftFluidPuzzle.PressureMechanisms
{
    public class MovingPlatform : MechanismBase
    {
        [Header("Platform Settings")]
        public Transform platformTransform;
        public Vector3 targetOffset = new Vector3(0, 5f, 0);
        public float platformSpeed = 2f;
        public bool moveOnlyWhenActive = true;

        [Header("Waypoints")]
        public Transform[] waypoints;
        public bool loopWaypoints = false;
        public int currentWaypointIndex = 0;

        private Vector3 _startPosition;
        private int _direction = 1;

        public Vector3 StartPosition => _startPosition;

        private void Start()
        {
            if (platformTransform == null)
            {
                platformTransform = transform;
            }

            _startPosition = platformTransform.position;
        }

        protected override void OnProgressUpdate(float progress)
        {
            if (platformTransform == null) return;

            if (waypoints != null && waypoints.Length > 0)
            {
                UpdateWaypointMovement(progress);
            }
            else
            {
                platformTransform.position = _startPosition + targetOffset * progress;
            }
        }

        private void UpdateWaypointMovement(float progress)
        {
            if (waypoints.Length < 2) return;

            int totalSegments = loopWaypoints ? waypoints.Length : waypoints.Length - 1;
            float segmentProgress = progress * totalSegments;
            int segmentIndex = Mathf.FloorToInt(segmentProgress);
            float localProgress = segmentProgress - segmentIndex;

            if (segmentIndex >= totalSegments)
            {
                segmentIndex = totalSegments - 1;
                localProgress = 1f;
            }

            int nextIndex = (segmentIndex + 1) % waypoints.Length;

            if (segmentIndex < waypoints.Length && nextIndex < waypoints.Length)
            {
                platformTransform.position = Vector3.Lerp(
                    waypoints[segmentIndex].position,
                    waypoints[nextIndex].position,
                    localProgress
                );
            }
        }

        public void SetProgress(float progress)
        {
            _currentProgress = Mathf.Clamp01(progress);
            OnProgressUpdate(_currentProgress);
        }

        public void MoveToWaypoint(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length) return;
            currentWaypointIndex = index;
        }

        private void OnDrawGizmosSelected()
        {
            if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null) continue;

                    Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

                    if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }

                    if (loopWaypoints && i == waypoints.Length - 1 && waypoints[0] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
                    }
                }
            }
            else if (platformTransform != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 start = Application.isPlaying ? _startPosition : platformTransform.position;
                Vector3 end = start + targetOffset;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireCube(start, Vector3.one * 0.5f);
                Gizmos.DrawWireCube(end, Vector3.one * 0.5f);
            }
        }
    }
}
