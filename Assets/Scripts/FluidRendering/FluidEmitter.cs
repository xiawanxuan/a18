using UnityEngine;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.FluidRendering
{
    public class FluidEmitter : MonoBehaviour
    {
        [Header("References")]
        public FluidSystem targetFluidSystem;

        [Header("Emission Settings")]
        public int particlesPerSecond = 50;
        public float emissionForce = 5f;
        public float maxEmissionAngle = 15f;

        [Header("Shape")]
        public enum EmitterShape { Point, Cone, Ring }
        public EmitterShape shape = EmitterShape.Cone;
        public float emitterRadius = 0.5f;

        [Header("Particle Settings")]
        public Color particleColor = Color.blue;
        public float particleSize = 1f;

        private float _accumulatedParticles;
        private bool _isEmitting = true;

        public bool IsEmitting => _isEmitting;

        public void StartEmitting()
        {
            _isEmitting = true;
        }

        public void StopEmitting()
        {
            _isEmitting = false;
        }

        public void ToggleEmission()
        {
            _isEmitting = !_isEmitting;
        }

        public void EmitBurst(int count)
        {
            if (targetFluidSystem == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 emitPosition = GetEmitPosition();
                Vector3 emitVelocity = GetEmitVelocity();

                targetFluidSystem.EmitParticles(emitPosition, 1, emitVelocity);
            }
        }

        private void Update()
        {
            if (!_isEmitting || targetFluidSystem == null) return;

            _accumulatedParticles += particlesPerSecond * Time.deltaTime;

            while (_accumulatedParticles >= 1f)
            {
                EmitSingleParticle();
                _accumulatedParticles -= 1f;
            }
        }

        private void EmitSingleParticle()
        {
            if (targetFluidSystem == null) return;

            Vector3 emitPosition = GetEmitPosition();
            Vector3 emitVelocity = GetEmitVelocity();

            targetFluidSystem.EmitParticles(emitPosition, 1, emitVelocity);
        }

        private Vector3 GetEmitPosition()
        {
            switch (shape)
            {
                case EmitterShape.Point:
                    return transform.position;

                case EmitterShape.Cone:
                    return transform.position;

                case EmitterShape.Ring:
                    float angle = Random.value * Mathf.PI * 2f;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * emitterRadius,
                        0f,
                        Mathf.Sin(angle) * emitterRadius
                    );
                    return transform.position + transform.TransformDirection(offset);

                default:
                    return transform.position;
            }
        }

        private Vector3 GetEmitVelocity()
        {
            Vector3 baseDirection = transform.forward;

            if (shape == EmitterShape.Cone || shape == EmitterShape.Ring)
            {
                float angleRad = maxEmissionAngle * Mathf.Deg2Rad * Random.value;
                float azimuth = Random.value * Mathf.PI * 2f;

                Vector3 randomOffset = new Vector3(
                    Mathf.Sin(angleRad) * Mathf.Cos(azimuth),
                    Mathf.Sin(angleRad) * Mathf.Sin(azimuth),
                    Mathf.Cos(angleRad)
                );

                baseDirection = transform.TransformDirection(randomOffset.normalized);
            }

            return baseDirection * emissionForce;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.1f);

            if (shape == EmitterShape.Cone)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Vector3 endPoint = transform.position + transform.forward * emissionForce * 0.2f;

                float endRadius = Mathf.Tan(maxEmissionAngle * Mathf.Deg2Rad) * emissionForce * 0.2f;
                Gizmos.DrawWireSphere(endPoint, endRadius);

                Vector3[] lines = new Vector3[8];
                for (int i = 0; i < 8; i++)
                {
                    float angle = (float)i / 8f * Mathf.PI * 2f;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * endRadius,
                        Mathf.Sin(angle) * endRadius,
                        0f
                    );
                    Gizmos.DrawLine(transform.position, endPoint + transform.TransformDirection(offset));
                }
            }
            else if (shape == EmitterShape.Ring)
            {
                Gizmos.color = Color.yellow;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(Vector3.zero, emitterRadius);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
