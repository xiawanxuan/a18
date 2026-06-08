using UnityEngine;
using System.Collections;

namespace SoftFluidPuzzle.PressureMechanisms
{
    public abstract class MechanismBase : MonoBehaviour
    {
        [Header("Mechanism Settings")]
        public string mechanismId = "mechanism_01";
        public bool isActive = false;
        public bool toggleMode = false;
        public float activationDelay = 0f;
        public float deactivationDelay = 0f;

        [Header("Animation")]
        public float animationDuration = 1f;
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio")]
        public AudioClip activationSound;
        public AudioClip deactivationSound;
        public float soundVolume = 0.7f;

        protected float _currentProgress = 0f;
        protected Coroutine _animationCoroutine;
        protected bool _isAnimating = false;

        public float CurrentProgress => _currentProgress;
        public bool IsAnimating => _isAnimating;

        public virtual void Activate()
        {
            if (toggleMode)
            {
                SetState(!isActive);
            }
            else
            {
                SetState(true);
            }
        }

        public virtual void Deactivate()
        {
            if (toggleMode)
            {
                SetState(!isActive);
            }
            else
            {
                SetState(false);
            }
        }

        public virtual void SetState(bool active)
        {
            if (active == isActive && !_isAnimating) return;

            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            isActive = active;
            float delay = active ? activationDelay : deactivationDelay;

            _animationCoroutine = StartCoroutine(AnimateState(active, delay));
        }

        protected virtual IEnumerator AnimateState(bool targetState, float delay)
        {
            _isAnimating = true;

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            PlaySound(targetState ? activationSound : deactivationSound);

            float startProgress = _currentProgress;
            float targetProgress = targetState ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                float curveT = animationCurve.Evaluate(t);
                _currentProgress = Mathf.Lerp(startProgress, targetProgress, curveT);

                OnProgressUpdate(_currentProgress);
                yield return null;
            }

            _currentProgress = targetProgress;
            OnProgressUpdate(_currentProgress);

            _isAnimating = false;
            OnStateChanged(targetState);
        }

        protected abstract void OnProgressUpdate(float progress);

        protected virtual void OnStateChanged(bool active)
        {
        }

        protected void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, soundVolume);
            }
        }

        public virtual void ResetMechanism()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }

            isActive = false;
            _currentProgress = 0f;
            _isAnimating = false;
            OnProgressUpdate(0f);
        }
    }
}
