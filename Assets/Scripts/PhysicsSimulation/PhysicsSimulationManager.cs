using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.PhysicsSimulation
{
    public class PhysicsSimulationManager : Singleton<PhysicsSimulationManager>
    {
        [Header("Simulation Settings")]
        public float simulationScale = 1f;
        public int maxPhysicsStepsPerFrame = 4;
        public float fixedTimestep = 0.02f;

        [Header("Gravity")]
        public Vector3 gravity = new Vector3(0, -9.81f, 0);

        private List<SoftBody> _softBodies = new List<SoftBody>();
        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        protected override void Awake()
        {
            base.Awake();
            Physics.gravity = gravity;
        }

        public void RegisterSoftBody(SoftBody softBody)
        {
            if (!_softBodies.Contains(softBody))
            {
                _softBodies.Add(softBody);
            }
        }

        public void UnregisterSoftBody(SoftBody softBody)
        {
            _softBodies.Remove(softBody);
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void SetGravity(Vector3 newGravity)
        {
            gravity = newGravity;
            Physics.gravity = newGravity;
        }

        public SoftBody[] GetAllSoftBodies()
        {
            return _softBodies.ToArray();
        }

        public void AddGlobalForce(Vector3 force)
        {
            foreach (SoftBody softBody in _softBodies)
            {
                if (softBody != null)
                {
                    softBody.AddForce(force);
                }
            }
        }

        public void AddExplosionAtPosition(Vector3 position, float force, float radius)
        {
            foreach (SoftBody softBody in _softBodies)
            {
                if (softBody != null)
                {
                    softBody.AddExplosionForce(position, force, radius);
                }
            }
        }

        public void ResetAllSimulations()
        {
            foreach (SoftBody softBody in _softBodies)
            {
                if (softBody != null)
                {
                    softBody.ResetSoftBody();
                }
            }
        }
    }
}
