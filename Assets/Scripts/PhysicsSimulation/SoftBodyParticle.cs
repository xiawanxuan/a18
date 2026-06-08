using UnityEngine;

namespace SoftFluidPuzzle.PhysicsSimulation
{
    public class SoftBodyParticle : MonoBehaviour
    {
        [Header("Physics Properties")]
        public float mass = 1f;
        public float radius = 0.5f;
        public float drag = 0.02f;
        public float gravityScale = 1f;

        [Header("Collision")]
        public LayerMask collisionLayers;
        public float collisionDamping = 0.5f;
        public float skinWidth = 0.01f;

        private Vector3 _velocity;
        private Vector3 _acceleration;
        private Vector3 _oldPosition;
        private bool _isStatic;
        private Collider[] _collisionResults;
        private const int MaxCollisions = 8;

        public Vector3 Velocity => _velocity;
        public Vector3 Position => transform.position;
        public bool IsStatic => _isStatic;

        private void Awake()
        {
            _collisionResults = new Collider[MaxCollisions];
            _oldPosition = transform.position;
        }

        public void SetStatic(bool isStatic)
        {
            _isStatic = isStatic;
        }

        public void AddForce(Vector3 force)
        {
            if (_isStatic) return;
            _acceleration += force / mass;
        }

        public void AddImpulse(Vector3 impulse)
        {
            if (_isStatic) return;
            _velocity += impulse / mass;
        }

        public void UpdateParticle(float deltaTime)
        {
            if (_isStatic) return;

            _velocity += Physics.gravity * gravityScale * deltaTime;
            _velocity += _acceleration * deltaTime;
            _velocity *= (1f - drag);
            _acceleration = Vector3.zero;

            Vector3 newPosition = transform.position + _velocity * deltaTime;

            ResolveCollisions(ref newPosition);

            _velocity = (newPosition - transform.position) / deltaTime;
            transform.position = newPosition;
        }

        private void ResolveCollisions(ref Vector3 position)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(position, radius, _collisionResults, collisionLayers);

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = _collisionResults[i];
                if (collider.transform == transform) continue;

                Vector3 closestPoint = collider.ClosestPoint(position);
                Vector3 direction = position - closestPoint;

                if (direction.sqrMagnitude < radius * radius)
                {
                    float distance = direction.magnitude;
                    if (distance < 0.001f)
                    {
                        direction = position - collider.transform.position;
                        distance = direction.magnitude;
                        if (distance < 0.001f)
                        {
                            direction = Vector3.up;
                            distance = 0.001f;
                        }
                    }

                    Vector3 normal = direction / distance;
                    float penetration = radius - distance + skinWidth;

                    position += normal * penetration;

                    float velocityAlongNormal = Vector3.Dot(_velocity, normal);
                    if (velocityAlongNormal < 0)
                    {
                        _velocity -= velocityAlongNormal * normal * (1f + collisionDamping);
                    }
                }
            }
        }

        public void ResetVelocity()
        {
            _velocity = Vector3.zero;
            _acceleration = Vector3.zero;
        }

        public void SetVelocity(Vector3 velocity)
        {
            if (_isStatic) return;
            _velocity = velocity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
