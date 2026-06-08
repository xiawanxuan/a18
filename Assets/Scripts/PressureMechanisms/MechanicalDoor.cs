using UnityEngine;

namespace SoftFluidPuzzle.PressureMechanisms
{
    public class MechanicalDoor : MechanismBase
    {
        [Header("Door Settings")]
        public Transform doorTransform;
        public DoorType doorType = DoorType.Sliding;
        public Vector3 openOffset = new Vector3(0, 3f, 0);
        public float openRotation = 90f;
        public Vector3 rotationAxis = Vector3.up;

        private Vector3 _closedPosition;
        private Quaternion _closedRotation;

        public enum DoorType
        {
            Sliding,
            Swinging,
            Folding,
            Rising
        }

        private void Start()
        {
            if (doorTransform == null)
            {
                doorTransform = transform;
            }

            _closedPosition = doorTransform.localPosition;
            _closedRotation = doorTransform.localRotation;
        }

        protected override void OnProgressUpdate(float progress)
        {
            if (doorTransform == null) return;

            switch (doorType)
            {
                case DoorType.Sliding:
                case DoorType.Rising:
                case DoorType.Folding:
                    doorTransform.localPosition = _closedPosition + openOffset * progress;
                    break;

                case DoorType.Swinging:
                    float angle = openRotation * progress;
                    doorTransform.localRotation = _closedRotation * Quaternion.Euler(rotationAxis * angle);
                    break;
            }
        }

        public void Open()
        {
            SetState(true);
        }

        public void Close()
        {
            SetState(false);
        }

        public void Toggle()
        {
            SetState(!isActive);
        }

        private void OnDrawGizmosSelected()
        {
            if (doorTransform == null) return;

            Gizmos.color = Color.yellow;
            Vector3 startPos = Application.isPlaying ? _closedPosition : doorTransform.position;

            if (doorType == DoorType.Sliding || doorType == DoorType.Rising)
            {
                Vector3 endPos = startPos + openOffset;
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawWireSphere(startPos, 0.3f);
                Gizmos.DrawWireSphere(endPos, 0.3f);
            }
        }
    }
}
