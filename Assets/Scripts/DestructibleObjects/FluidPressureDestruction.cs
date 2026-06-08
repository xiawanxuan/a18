using UnityEngine;
using SoftFluidPuzzle.FluidRendering;

namespace SoftFluidPuzzle.DestructibleObjects
{
    public class FluidPressureDestruction : MonoBehaviour
    {
        [Header("References")]
        public FluidSystem targetFluidSystem;
        public DestructibleObject destructibleObject;

        [Header("Pressure Settings")]
        public float detectionRadius = 2f;
        public float pressureMultiplier = 100f;
        public float minFluidParticles = 5;
        public PressureCalculationMode calculationMode = PressureCalculationMode.ParticleCount;

        [Header("Damage")]
        public float damagePerSecond = 10f;
        public bool continuousDamage = true;

        [Header("Visual Feedback")]
        public bool showPressureGizmo = true;

        private float _currentPressure;

        public enum PressureCalculationMode
        {
            ParticleCount,
            Density,
            ImpactForce
        }

        public float CurrentPressure => _currentPressure;

        private void Start()
        {
            if (destructibleObject == null)
            {
                destructibleObject = GetComponent<DestructibleObject>();
            }
        }

        private void FixedUpdate()
        {
            if (targetFluidSystem == null || destructibleObject == null) return;
            if (destructibleObject.IsDestroyed) return;

            CalculatePressure();
            ApplyDamage();
        }

        private void CalculatePressure()
        {
            _currentPressure = 0f;
            int nearbyParticles = 0;
            float totalDensity = 0f;

            foreach (FluidParticle particle in targetFluidSystem.Particles)
            {
                if (!particle.IsActive) continue;

                float distance = Vector3.Distance(transform.position, particle.Position);
                if (distance < detectionRadius + particle.Radius)
                {
                    nearbyParticles++;
                    totalDensity += particle.Density;

                    switch (calculationMode)
                    {
                        case PressureCalculationMode.ParticleCount:
                            float falloff = 1f - (distance / (detectionRadius + particle.Radius));
                            _currentPressure += falloff * pressureMultiplier;
                            break;

                        case PressureCalculationMode.Density:
                            float densityFactor = particle.Density / 1000f;
                            _currentPressure += densityFactor * pressureMultiplier * 0.1f;
                            break;

                        case PressureCalculationMode.ImpactForce:
                            float impactForce = particle.Velocity.magnitude * particle.Mass;
                            Vector3 toParticle = particle.Position - transform.position;
                            float dot = Vector3.Dot(particle.Velocity.normalized, -toParticle.normalized);
                            if (dot > 0f)
                            {
                                _currentPressure += impactForce * dot * pressureMultiplier * 0.01f;
                            }
                            break;
                    }
                }
            }

            if (nearbyParticles < minFluidParticles)
            {
                _currentPressure = 0f;
            }
        }

        private void ApplyDamage()
        {
            if (_currentPressure <= 0f) return;
            if (!continuousDamage && destructibleObject.CurrentHealth < destructibleObject.health)
            {
                return;
            }

            float damage = damagePerSecond * Time.fixedDeltaTime * (_currentPressure / 100f);
            destructibleObject.TakeDamage(damage);
        }

        public void ApplyPressureBurst(float pressureAmount)
        {
            if (destructibleObject == null || destructibleObject.IsDestroyed) return;

            float damage = damagePerSecond * pressureAmount * 0.1f;
            destructibleObject.TakeDamage(damage);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showPressureGizmo) return;

            float pressurePercent = Mathf.Clamp01(_currentPressure / 100f);
            Color gizmoColor = Color.Lerp(Color.green, Color.red, pressurePercent);
            gizmoColor.a = 0.3f;

            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (destructibleObject != null)
            {
                HandlesUtils.DrawLabel(transform.position + Vector3.up * 2f,
                    string.Format("Pressure: {0:F1}\nHealth: {1:F1}%",
                    _currentPressure,
                    destructibleObject.HealthPercentage * 100f));
            }
        }
    }

    public static class HandlesUtils
    {
        public static void DrawLabel(Vector3 position, string text)
        {
            // This is a placeholder - in actual Unity scene view you'd use Handles.Label
            // We keep this for editor integration later
        }
    }
}
