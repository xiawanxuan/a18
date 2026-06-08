using UnityEngine;
using SoftFluidPuzzle.FluidRendering;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.PressureMechanisms
{
    public class FluidPressureSensor : MonoBehaviour
    {
        [Header("Settings")]
        public string sensorId = "sensor_01";
        public float activationPressure = 50f;
        public float deactivationPressure = 30f;
        public float detectionRadius = 2f;
        public SensorMode sensorMode = SensorMode.Pressure;
        public FluidSystem targetFluidSystem;

        [Header("Hysteresis")]
        public bool useHysteresis = true;

        [Header("Visual")]
        public Renderer indicatorRenderer;
        public Color inactiveColor = Color.gray;
        public Color activeColor = Color.blue;
        public float glowIntensity = 2f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onActivated;
        public UnityEngine.Events.UnityEvent onDeactivated;
        public UnityEngine.Events.FloatEvent onPressureChanged;

        private bool _isActive;
        private float _currentPressure;

        public bool IsActive => _isActive;
        public float CurrentPressure => _currentPressure;

        public enum SensorMode
        {
            Pressure,
            Volume,
            ParticleCount,
            Impact
        }

        private void Start()
        {
            UpdateIndicator();
        }

        private void Update()
        {
            if (targetFluidSystem == null)
            {
                FindFluidSystem();
                return;
            }

            MeasurePressure();
            CheckActivation();
            UpdateIndicator();
        }

        private void FindFluidSystem()
        {
            FluidSystem[] systems = FindObjectsOfType<FluidSystem>();
            float minDist = float.MaxValue;

            foreach (FluidSystem system in systems)
            {
                float dist = Vector3.Distance(transform.position, system.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    targetFluidSystem = system;
                }
            }
        }

        private void MeasurePressure()
        {
            _currentPressure = 0f;
            int particleCount = 0;
            float totalDensity = 0f;
            float totalImpact = 0f;

            foreach (FluidParticle particle in targetFluidSystem.Particles)
            {
                if (!particle.IsActive) continue;

                float distance = Vector3.Distance(transform.position, particle.Position);
                if (distance < detectionRadius + particle.Radius)
                {
                    float falloff = 1f - Mathf.Clamp01(distance / (detectionRadius + particle.Radius));
                    particleCount++;
                    totalDensity += particle.Density * falloff;

                    switch (sensorMode)
                    {
                        case SensorMode.Pressure:
                            _currentPressure += particle.Density * falloff * 0.01f;
                            break;

                        case SensorMode.Volume:
                            _currentPressure += particle.Mass;
                            break;

                        case SensorMode.ParticleCount:
                            _currentPressure++;
                            break;

                        case SensorMode.Impact:
                            float impactForce = particle.Velocity.magnitude * particle.Mass;
                            Vector3 toParticle = particle.Position - transform.position;
                            float dot = Vector3.Dot(particle.Velocity.normalized, -toParticle.normalized);
                            if (dot > 0f)
                            {
                                totalImpact += impactForce * dot * falloff;
                            }
                            break;
                    }
                }
            }

            if (sensorMode == SensorMode.Impact)
            {
                _currentPressure = totalImpact;
            }

            onPressureChanged?.Invoke(_currentPressure);
        }

        private void CheckActivation()
        {
            bool wasActive = _isActive;

            if (useHysteresis)
            {
                if (!_isActive && _currentPressure >= activationPressure)
                {
                    Activate();
                }
                else if (_isActive && _currentPressure <= deactivationPressure)
                {
                    Deactivate();
                }
            }
            else
            {
                if (!_isActive && _currentPressure >= activationPressure)
                {
                    Activate();
                }
                else if (_isActive && _currentPressure < activationPressure)
                {
                    Deactivate();
                }
            }

            if (_isActive != wasActive)
            {
                EventBus.Publish("OnPressureSensorChanged", sensorId + "_" + (_isActive ? "on" : "off"));
            }
        }

        private void Activate()
        {
            _isActive = true;
            onActivated?.Invoke();
        }

        private void Deactivate()
        {
            _isActive = false;
            onDeactivated?.Invoke();
        }

        private void UpdateIndicator()
        {
            if (indicatorRenderer == null) return;

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            indicatorRenderer.GetPropertyBlock(mpb);

            Color targetColor = _isActive ? activeColor : inactiveColor;
            float intensity = _isActive ? glowIntensity : 0f;

            mpb.SetColor("_EmissionColor", targetColor * intensity);
            mpb.SetColor("_Color", targetColor);

            indicatorRenderer.SetPropertyBlock(mpb);
        }

        public void ResetSensor()
        {
            _isActive = false;
            _currentPressure = 0f;
            UpdateIndicator();
        }

        private void OnDrawGizmosSelected()
        {
            float pressurePercent = activationPressure > 0 ? Mathf.Clamp01(_currentPressure / activationPressure) : 0f;
            Color gizmoColor = Color.Lerp(Color.gray, Color.blue, pressurePercent);
            gizmoColor.a = 0.3f;

            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, detectionRadius);

            Gizmos.color = _isActive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Vector3 labelPos = transform.position + Vector3.up * (detectionRadius + 0.5f);
            Gizmos.color = Color.white;
        }
    }
}
