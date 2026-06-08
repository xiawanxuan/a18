using UnityEngine;
using SoftFluidPuzzle.FluidRendering;

namespace SoftFluidPuzzle.PressureMechanisms
{
    public class FluidValve : MechanismBase
    {
        [Header("Valve Settings")]
        public FluidEmitter targetEmitter;
        public FluidSystem targetFluidSystem;

        [Header("Flow Control")]
        public float minFlowRate = 0f;
        public float maxFlowRate = 50f;
        public float pressureMultiplier = 1f;

        [Header("Visual")]
        public Transform valveWheel;
        public float maxRotation = 180f;
        public Vector3 rotationAxis = Vector3.forward;

        public float CurrentFlowRate => Mathf.Lerp(minFlowRate, maxFlowRate, _currentProgress);

        protected override void OnProgressUpdate(float progress)
        {
            if (targetEmitter != null)
            {
                targetEmitter.particlesPerSecond = Mathf.Lerp(minFlowRate, maxFlowRate, progress);
            }

            if (valveWheel != null)
            {
                float angle = maxRotation * progress;
                valveWheel.localRotation = Quaternion.Euler(rotationAxis * angle);
            }
        }

        public void SetFlow(float flowAmount)
        {
            float progress = Mathf.InverseLerp(minFlowRate, maxFlowRate, flowAmount);
            SetState(progress > 0.5f);
        }

        public void OpenFully()
        {
            SetState(true);
        }

        public void CloseCompletely()
        {
            SetState(false);
        }

        public bool IsOpen => _currentProgress > 0.01f;
        public bool IsClosed => _currentProgress <= 0.01f;
    }
}
