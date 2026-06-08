using UnityEngine;

namespace SoftFluidPuzzle.PhysicsSimulation
{
    public class SoftBodySpring : MonoBehaviour
    {
        [Header("Spring Settings")]
        public float stiffness = 50f;
        public float damping = 0.5f;
        public float restLength = 1f;
        public float maxStretchFactor = 1.5f;

        [Header("References")]
        public SoftBodyParticle particleA;
        public SoftBodyParticle particleB;

        private Vector3 _springForce;
        private bool _isBroken;

        public bool IsBroken => _isBroken;
        public float CurrentLength => Vector3.Distance(particleA.Position, particleB.Position);

        public void Initialize(SoftBodyParticle a, SoftBodyParticle b, float length)
        {
            particleA = a;
            particleB = b;
            restLength = length;
        }

        public void UpdateSpring(float deltaTime)
        {
            if (_isBroken || particleA == null || particleB == null) return;

            Vector3 direction = particleB.Position - particleA.Position;
            float currentLength = direction.magnitude;

            if (currentLength < 0.001f) return;

            Vector3 springDirection = direction / currentLength;

            float extension = currentLength - restLength;

            if (Mathf.Abs(extension) > restLength * maxStretchFactor)
            {
                _isBroken = true;
                return;
            }

            float springForceMagnitude = stiffness * extension;

            Vector3 relativeVelocity = particleB.Velocity - particleA.Velocity;
            float dampingForceMagnitude = damping * Vector3.Dot(relativeVelocity, springDirection);

            float totalForce = springForceMagnitude + dampingForceMagnitude;

            _springForce = springDirection * totalForce;

            particleA.AddForce(_springForce);
            particleB.AddForce(-_springForce);
        }

        public void Break()
        {
            _isBroken = true;
        }

        public void Repair()
        {
            _isBroken = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (particleA == null || particleB == null) return;

            Gizmos.color = _isBroken ? Color.red : Color.green;
            Gizmos.DrawLine(particleA.Position, particleB.Position);
        }
    }
}
