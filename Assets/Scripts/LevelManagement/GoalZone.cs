using UnityEngine;
using SoftFluidPuzzle.Core;
using SoftFluidPuzzle.PlayerControl;

namespace SoftFluidPuzzle.LevelManagement
{
    public class GoalZone : MonoBehaviour
    {
        [Header("Goal Settings")]
        public string goalId = "goal_01";
        public bool requireAllObjectives = true;
        public float triggerDelay = 0.5f;

        [Header("Detection")]
        public LayerMask playerLayer;

        [Header("Visuals")]
        public Renderer goalRenderer;
        public Color idleColor = Color.yellow;
        public Color activeColor = Color.green;
        public float pulseSpeed = 2f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onGoalReached;

        private bool _playerInside;
        private float _triggerTimer;
        private bool _isActivated;

        public bool IsActivated => _isActivated;

        private void Update()
        {
            UpdateVisual();

            if (_playerInside && !_isActivated)
            {
                _triggerTimer += Time.deltaTime;
                if (_triggerTimer >= triggerDelay)
                {
                    ActivateGoal();
                }
            }
            else
            {
                _triggerTimer = 0f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                if (other.GetComponent<PlayerController>() != null)
                {
                    _playerInside = true;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                if (other.GetComponent<PlayerController>() != null)
                {
                    _playerInside = false;
                    _triggerTimer = 0f;
                }
            }
        }

        private void ActivateGoal()
        {
            if (_isActivated) return;

            _isActivated = true;
            onGoalReached?.Invoke();
            EventBus.Publish("OnGoalReached", goalId);

            if (LevelManager.Instance != null)
            {
                if (requireAllObjectives)
                {
                    LevelManager.Instance.CompleteObjective(goalId);
                }
                else
                {
                    LevelManager.Instance.CompleteLevel();
                }
            }

            Debug.Log("Goal reached: " + goalId);
        }

        private void UpdateVisual()
        {
            if (goalRenderer == null) return;

            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            Color targetColor = _isActivated ? activeColor : idleColor;
            Color pulseColor = Color.Lerp(targetColor, Color.white, pulse * 0.3f);

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            goalRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", pulseColor);
            goalRenderer.SetPropertyBlock(mpb);
        }

        public void ResetGoal()
        {
            _isActivated = false;
            _playerInside = false;
            _triggerTimer = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
