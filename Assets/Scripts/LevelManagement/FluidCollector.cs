using UnityEngine;
using SoftFluidPuzzle.FluidRendering;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.LevelManagement
{
    public class FluidCollector : MonoBehaviour
    {
        [Header("Settings")]
        public string collectorId = "collector_01";
        public float targetVolume = 50f;
        public float fillAmount = 0f;
        public bool isCompleted = false;

        [Header("Detection")]
        public LayerMask fluidLayers;
        public float collectionRadius = 1f;
        public float collectionRate = 10f;

        [Header("Visuals")]
        public Renderer fillRenderer;
        public Material fillMaterial;
        public Color emptyColor = Color.gray;
        public Color fullColor = Color.blue;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onCompleted;
        public UnityEngine.Events.UnityEvent onFailed;

        private FluidSystem _nearbyFluid;

        public float FillPercentage => Mathf.Clamp01(fillAmount / targetVolume);

        private void Update()
        {
            if (isCompleted) return;

            CollectFluid();
            UpdateVisual();
            CheckCompletion();
        }

        private void CollectFluid()
        {
            if (_nearbyFluid == null)
            {
                FindNearbyFluid();
                return;
            }

            int particlesToCollect = 0;

            for (int i = _nearbyFluid.Particles.Count - 1; i >= 0; i--)
            {
                FluidParticle particle = _nearbyFluid.Particles[i];
                if (!particle.IsActive) continue;

                float distance = Vector3.Distance(transform.position, particle.Position);
                if (distance < collectionRadius + particle.Radius)
                {
                    float collectAmount = collectionRate * Time.deltaTime;
                    fillAmount += particle.Mass;
                    _nearbyFluid.RemoveParticle(i);
                    particlesToCollect++;

                    if (fillAmount >= targetVolume)
                    {
                        break;
                    }
                }
            }
        }

        private void FindNearbyFluid()
        {
            FluidSystem[] fluidSystems = FindObjectsOfType<FluidSystem>();

            foreach (FluidSystem fs in fluidSystems)
            {
                foreach (FluidParticle particle in fs.Particles)
                {
                    if (!particle.IsActive) continue;

                    float distance = Vector3.Distance(transform.position, particle.Position);
                    if (distance < collectionRadius * 3f)
                    {
                        _nearbyFluid = fs;
                        return;
                    }
                }
            }
        }

        private void UpdateVisual()
        {
            if (fillRenderer != null && fillMaterial != null)
            {
                float fillPercent = FillPercentage;
                Color currentColor = Color.Lerp(emptyColor, fullColor, fillPercent);
                fillMaterial.color = currentColor;

                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                fillRenderer.GetPropertyBlock(mpb);
                mpb.SetFloat("_FillAmount", fillPercent);
                fillRenderer.SetPropertyBlock(mpb);
            }
        }

        private void CheckCompletion()
        {
            if (fillAmount >= targetVolume && !isCompleted)
            {
                isCompleted = true;
                onCompleted?.Invoke();
                EventBus.Publish("OnFluidCollectorCompleted", collectorId);
                Debug.Log("Fluid collector completed: " + collectorId);
            }
        }

        public void ResetCollector()
        {
            fillAmount = 0f;
            isCompleted = false;
        }

        public void AddFluid(float amount)
        {
            fillAmount = Mathf.Min(fillAmount + amount, targetVolume);
            CheckCompletion();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isCompleted ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);

            Vector3 fillHeight = Vector3.up * (FillPercentage * 2f);
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
            Gizmos.DrawCube(transform.position + fillHeight * 0.5f, new Vector3(collectionRadius * 1.5f, fillHeight.y, collectionRadius * 1.5f));
        }
    }
}
