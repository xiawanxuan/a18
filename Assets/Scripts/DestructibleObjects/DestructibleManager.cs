using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.DestructibleObjects
{
    public class DestructibleManager : Singleton<DestructibleManager>
    {
        [Header("Settings")]
        public bool enableDestruction = true;
        public int maxFragments = 200;
        public float globalDamageMultiplier = 1f;

        [Header("Pooling")]
        public bool useObjectPooling = true;
        public int initialPoolSize = 50;

        private List<DestructibleObject> _destructibleObjects = new List<DestructibleObject>();
        private List<DestructionFragment> _activeFragments = new List<DestructionFragment>();
        private Queue<DestructionFragment> _fragmentPool = new Queue<DestructionFragment>();

        public List<DestructibleObject> AllDestructibles => _destructibleObjects;
        public int ActiveFragmentCount => _activeFragments.Count;

        protected override void Awake()
        {
            base.Awake();

            if (useObjectPooling)
            {
                InitializeFragmentPool();
            }
        }

        private void InitializeFragmentPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                DestructionFragment fragment = CreateFragment();
                fragment.gameObject.SetActive(false);
                _fragmentPool.Enqueue(fragment);
            }
        }

        private DestructionFragment CreateFragment()
        {
            GameObject fragmentObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragmentObj.name = "Fragment_Pooled";
            DestructionFragment fragment = fragmentObj.AddComponent<DestructionFragment>();
            fragment.autoDestroy = false;
            return fragment;
        }

        public void RegisterDestructible(DestructibleObject destObj)
        {
            if (!_destructibleObjects.Contains(destObj))
            {
                _destructibleObjects.Add(destObj);
            }
        }

        public void UnregisterDestructible(DestructibleObject destObj)
        {
            _destructibleObjects.Remove(destObj);
        }

        public DestructionFragment GetFragment()
        {
            if (useObjectPooling && _fragmentPool.Count > 0)
            {
                DestructionFragment fragment = _fragmentPool.Dequeue();
                fragment.gameObject.SetActive(true);
                _activeFragments.Add(fragment);
                return fragment;
            }
            else
            {
                if (_activeFragments.Count >= maxFragments)
                {
                    return null;
                }

                DestructionFragment fragment = CreateFragment();
                _activeFragments.Add(fragment);
                return fragment;
            }
        }

        public void ReturnFragment(DestructionFragment fragment)
        {
            if (useObjectPooling && _fragmentPool.Count < maxFragments)
            {
                fragment.gameObject.SetActive(false);
                fragment.transform.SetParent(transform);
                _activeFragments.Remove(fragment);
                _fragmentPool.Enqueue(fragment);
            }
            else
            {
                _activeFragments.Remove(fragment);
                Destroy(fragment.gameObject);
            }
        }

        public void TriggerExplosionAt(Vector3 position, float force, float radius, float damage)
        {
            if (!enableDestruction) return;

            Collider[] hitColliders = Physics.OverlapSphere(position, radius);

            foreach (Collider collider in hitColliders)
            {
                DestructibleObject destObj = collider.GetComponent<DestructibleObject>();
                if (destObj != null)
                {
                    float distance = Vector3.Distance(position, collider.transform.position);
                    float distanceFactor = 1f - Mathf.Clamp01(distance / radius);
                    float actualDamage = damage * distanceFactor * globalDamageMultiplier;

                    destObj.TakeDamage(actualDamage);
                }

                Rigidbody rb = collider.attachedRigidbody;
                if (rb != null)
                {
                    rb.AddExplosionForce(force, position, radius);
                }
            }
        }

        public void RepairAllDestructibles()
        {
            foreach (DestructibleObject destObj in _destructibleObjects)
            {
                if (destObj != null)
                {
                    destObj.RepairObject();
                }
            }
        }

        public void DestroyAllFragments()
        {
            for (int i = _activeFragments.Count - 1; i >= 0; i--)
            {
                if (_activeFragments[i] != null)
                {
                    Destroy(_activeFragments[i].gameObject);
                }
            }
            _activeFragments.Clear();
        }

        public int GetDestroyedCount()
        {
            int count = 0;
            foreach (DestructibleObject destObj in _destructibleObjects)
            {
                if (destObj != null && destObj.IsDestroyed)
                {
                    count++;
                }
            }
            return count;
        }

        public int GetTotalDestructibleCount()
        {
            return _destructibleObjects.Count;
        }

        private void OnDestroy()
        {
            DestroyAllFragments();
        }
    }
}
