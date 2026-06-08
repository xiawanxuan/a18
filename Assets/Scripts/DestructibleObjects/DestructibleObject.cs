using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.DestructibleObjects
{
    [RequireComponent(typeof(Collider))]
    public class DestructibleObject : MonoBehaviour
    {
        [Header("Destruction Settings")]
        public float health = 100f;
        public float impactThreshold = 10f;
        public float pressureThreshold = 50f;
        public bool destructibleByFluid = true;
        public bool destructibleByImpact = true;
        public DestructionType destructionType = DestructionType.Shatter;

        [Header("Fragments")]
        public int fragmentCount = 8;
        public float fragmentMinSize = 0.3f;
        public float fragmentMaxSize = 0.8f;
        public float explosionForce = 10f;
        public float explosionRadius = 2f;

        [Header("Visuals")]
        public Material fragmentMaterial;
        public bool spawnParticles = true;
        public GameObject destructionEffectPrefab;

        [Header("Audio")]
        public AudioClip destructionSound;
        public float soundVolume = 1f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onDamaged;
        public UnityEngine.Events.UnityEvent onDestroyed;

        private float _currentHealth;
        private bool _isDestroyed;
        private Collider _collider;
        private Renderer _renderer;
        private Vector3 _lastVelocity;

        public float CurrentHealth => _currentHealth;
        public float HealthPercentage => _currentHealth / health;
        public bool IsDestroyed => _isDestroyed;

        public enum DestructionType
        {
            Shatter,
            Split,
            Melt,
            Explode
        }

        private void Awake()
        {
            _currentHealth = health;
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            if (_renderer != null && _renderer.material != null)
            {
                if (fragmentMaterial == null)
                {
                    fragmentMaterial = _renderer.material;
                }
            }
        }

        public void TakeDamage(float damage)
        {
            if (_isDestroyed) return;

            _currentHealth -= damage;
            onDamaged?.Invoke();

            UpdateDamageVisual();

            if (_currentHealth <= 0f)
            {
                DestroyObject();
            }
        }

        public void ApplyPressure(float pressure)
        {
            if (_isDestroyed || !destructibleByFluid) return;

            if (pressure > pressureThreshold)
            {
                float damage = (pressure - pressureThreshold) * 0.1f;
                TakeDamage(damage * Time.deltaTime);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isDestroyed || !destructibleByImpact) return;

            float impactForce = collision.impulse.magnitude;

            if (impactForce > impactThreshold)
            {
                float damage = impactForce - impactThreshold;
                TakeDamage(damage);

                if (destructionType == DestructionType.Explode && _currentHealth <= 0f)
                {
                    TriggerExplosion(collision.contacts[0].point);
                }
            }
        }

        public void DestroyObject()
        {
            if (_isDestroyed) return;

            _isDestroyed = true;
            onDestroyed?.Invoke();

            EventBus.Publish(GameEvents.OnObjectDestroyed, gameObject.name);

            switch (destructionType)
            {
                case DestructionType.Shatter:
                    CreateShatterFragments();
                    break;
                case DestructionType.Split:
                    CreateSplitFragments();
                    break;
                case DestructionType.Melt:
                    StartCoroutine(MeltCoroutine());
                    break;
                case DestructionType.Explode:
                    CreateExplosionFragments();
                    break;
            }

            if (spawnParticles && destructionEffectPrefab != null)
            {
                Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
            }

            PlayDestructionSound();

            DisableOriginalObject();
        }

        private void CreateShatterFragments()
        {
            Vector3 center = transform.position;

            for (int i = 0; i < fragmentCount; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 0.5f;
                Vector3 position = center + offset;

                GameObject fragmentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fragmentObj.name = "Fragment_" + i;
                fragmentObj.transform.position = position;

                float size = Random.Range(fragmentMinSize, fragmentMaxSize);
                fragmentObj.transform.localScale = new Vector3(size, size, size) * 0.5f;
                fragmentObj.transform.rotation = Random.rotation;

                if (fragmentMaterial != null)
                {
                    Renderer renderer = fragmentObj.GetComponent<Renderer>();
                    renderer.material = fragmentMaterial;
                }

                DestructionFragment fragment = fragmentObj.AddComponent<DestructionFragment>();
                fragment.mass = size * 0.5f;
                fragment.InitializeFromExplosion(center, explosionForce, explosionRadius);
            }
        }

        private void CreateSplitFragments()
        {
            int splits = 4;
            Vector3 center = transform.position;
            Vector3 originalSize = transform.localScale;

            for (int i = 0; i < splits; i++)
            {
                GameObject fragmentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fragmentObj.name = "SplitFragment_" + i;

                float angle = (float)i / splits * Mathf.PI * 2f;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * 0.5f,
                    Random.Range(-0.3f, 0.3f),
                    Mathf.Sin(angle) * 0.5f
                );
                fragmentObj.transform.position = center + offset;
                fragmentObj.transform.localScale = originalSize * 0.5f;
                fragmentObj.transform.rotation = Random.rotation;

                if (fragmentMaterial != null)
                {
                    Renderer renderer = fragmentObj.GetComponent<Renderer>();
                    renderer.material = fragmentMaterial;
                }

                DestructionFragment fragment = fragmentObj.AddComponent<DestructionFragment>();
                fragment.mass = 1f;
                fragment.InitializeFromExplosion(center, explosionForce * 0.5f, explosionRadius);
            }
        }

        private System.Collections.IEnumerator MeltCoroutine()
        {
            float meltDuration = 2f;
            float elapsed = 0f;

            Vector3 originalScale = transform.localScale;

            while (elapsed < meltDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / meltDuration;

                transform.localScale = Vector3.Lerp(originalScale, new Vector3(originalScale.x, 0.01f, originalScale.z), t);

                if (_renderer != null && _renderer.material != null)
                {
                    Color color = _renderer.material.color;
                    color.a = Mathf.Lerp(1f, 0f, t);
                    _renderer.material.color = color;
                }

                yield return null;
            }

            if (_collider != null)
            {
                _collider.enabled = false;
            }
        }

        private void CreateExplosionFragments()
        {
            int count = fragmentCount * 2;
            Vector3 center = transform.position;

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Random.onUnitSphere * 0.3f;
                Vector3 position = center + offset;

                GameObject fragmentObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fragmentObj.name = "ExplosionFragment_" + i;
                fragmentObj.transform.position = position;

                float size = Random.Range(fragmentMinSize * 0.5f, fragmentMaxSize * 0.7f);
                fragmentObj.transform.localScale = Vector3.one * size;

                if (fragmentMaterial != null)
                {
                    Renderer renderer = fragmentObj.GetComponent<Renderer>();
                    renderer.material = fragmentMaterial;
                }

                DestructionFragment fragment = fragmentObj.AddComponent<DestructionFragment>();
                fragment.mass = size * 0.3f;
                fragment.explosionRadius = explosionRadius * 1.5f;
                fragment.InitializeFromExplosion(center, explosionForce * 1.5f, explosionRadius * 1.5f);
            }
        }

        private void TriggerExplosion(Vector3 position)
        {
            Collider[] hitColliders = Physics.OverlapSphere(position, explosionRadius);

            foreach (Collider collider in hitColliders)
            {
                Rigidbody rb = collider.attachedRigidbody;
                if (rb != null && rb.gameObject != gameObject)
                {
                    rb.AddExplosionForce(explosionForce * 2f, position, explosionRadius);
                }

                DestructibleObject destObj = collider.GetComponent<DestructibleObject>();
                if (destObj != null && destObj != this)
                {
                    float distance = Vector3.Distance(position, collider.transform.position);
                    float damage = explosionForce * (1f - distance / explosionRadius);
                    destObj.TakeDamage(damage);
                }
            }
        }

        private void UpdateDamageVisual()
        {
            if (_renderer == null || _renderer.material == null) return;

            float damagePercent = 1f - HealthPercentage;
            Color color = _renderer.material.color;
            Color damageTint = Color.Lerp(color, Color.gray, damagePercent * 0.3f);
            _renderer.material.color = damageTint;
        }

        private void PlayDestructionSound()
        {
            if (destructionSound != null)
            {
                AudioSource.PlayClipAtPoint(destructionSound, transform.position, soundVolume);
            }
        }

        private void DisableOriginalObject()
        {
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_renderer != null && destructionType != DestructionType.Melt)
            {
                _renderer.enabled = false;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        public void RepairObject()
        {
            _isDestroyed = false;
            _currentHealth = health;

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_renderer != null)
            {
                _renderer.enabled = true;
                Color color = _renderer.material.color;
                color.a = 1f;
                _renderer.material.color = color;
            }

            StopAllCoroutines();
        }

        private void OnDrawGizmosSelected()
        {
            if (_isDestroyed)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, explosionRadius);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, explosionRadius * 0.5f);
            }
        }
    }
}
