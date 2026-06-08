using UnityEngine;
using SoftFluidPuzzle.Core;
using SoftFluidPuzzle.PlayerControl;

namespace SoftFluidPuzzle.LevelManagement
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        public int checkpointIndex = 0;
        public bool oneTimeUse = true;
        public float activationRadius = 2f;

        [Header("Visuals")]
        public Renderer indicatorRenderer;
        public Color inactiveColor = Color.gray;
        public Color activeColor = Color.green;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onActivated;

        private bool _isActivated;

        public bool IsActivated => _isActivated;

        private void OnTriggerEnter(Collider other)
        {
            if (_isActivated && oneTimeUse) return;

            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ActivateCheckpoint();
            }
        }

        public void ActivateCheckpoint()
        {
            if (_isActivated && oneTimeUse) return;

            _isActivated = true;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.AddCheckpoint(transform.position);
            }

            UpdateVisual();
            onActivated?.Invoke();

            EventBus.Publish("OnCheckpointActivated", checkpointIndex);
            Debug.Log("Checkpoint " + checkpointIndex + " activated!");
        }

        public void ResetCheckpoint()
        {
            if (!oneTimeUse) return;

            _isActivated = false;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (indicatorRenderer == null) return;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            indicatorRenderer.GetPropertyBlock(mpb);

            Color targetColor = _isActivated ? activeColor : inactiveColor;
            mpb.SetColor("_EmissionColor", targetColor);
            indicatorRenderer.SetPropertyBlock(mpb);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}
