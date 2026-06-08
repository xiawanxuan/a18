using UnityEngine;

namespace SoftFluidPuzzle.DestructibleObjects
{
    public class DestructionFragment : MonoBehaviour
    {
        [Header("Physics")]
        public float mass = 0.5f;
        public float lifetime = 5f;
        public float fadeOutTime = 2f;
        public bool autoDestroy = true;

        [Header("Forces")]
        public float minExplosionForce = 2f;
        public float maxExplosionForce = 8f;
        public float explosionRadius = 2f;

        private Rigidbody _rigidbody;
        private Collider _collider;
        private Renderer _renderer;
        private float _spawnTime;
        private bool _isFading;
        private Material _materialInstance;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            _rigidbody.mass = mass;
            _rigidbody.useGravity = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            _collider = GetComponent<Collider>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider>();
            }

            _renderer = GetComponent<Renderer>();
            if (_renderer != null && _renderer.material != null)
            {
                _materialInstance = _renderer.material;
            }

            _spawnTime = Time.time;
        }

        public void InitializeFromExplosion(Vector3 explosionCenter, float explosionForce, float explosionRadius)
        {
            if (_rigidbody == null) return;

            Vector3 direction = transform.position - explosionCenter;
            float distance = direction.magnitude;

            if (distance < 0.1f)
            {
                direction = Random.onUnitSphere;
                distance = 0.1f;
            }

            float forceFactor = 1f - Mathf.Clamp01(distance / explosionRadius);
            float force = explosionForce * forceFactor * Random.Range(0.7f, 1.3f);

            _rigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
            _rigidbody.AddTorque(Random.onUnitSphere * Random.Range(1f, 5f), ForceMode.Impulse);
        }

        private void Update()
        {
            if (!autoDestroy) return;

            float elapsed = Time.time - _spawnTime;

            if (elapsed > lifetime - fadeOutTime && !_isFading)
            {
                _isFading = true;
            }

            if (_isFading && _materialInstance != null)
            {
                float fadeProgress = (elapsed - (lifetime - fadeOutTime)) / fadeOutTime;
                Color color = _materialInstance.color;
                color.a = Mathf.Lerp(1f, 0f, fadeProgress);
                _materialInstance.color = color;
            }

            if (elapsed > lifetime)
            {
                Destroy(gameObject);
            }
        }

        public void Freeze()
        {
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        public void ShatterAdditional(float force)
        {
            if (_rigidbody != null)
            {
                _rigidbody.AddExplosionForce(force, transform.position, 0.5f);
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
    }
}
