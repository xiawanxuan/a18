using UnityEngine;
using SoftFluidPuzzle.FluidRendering;

namespace SoftFluidPuzzle.PlayerControl
{
    public class PlayerFluidInteraction : MonoBehaviour
    {
        [Header("References")]
        public FluidSystem targetFluidSystem;

        [Header("Interaction Settings")]
        public float interactionRadius = 1f;
        public float pushForce = 20f;
        public float playerDragInFluid = 3f;
        public float buoyancyStrength = 15f;

        [Header("Foot Splash")]
        public bool enableFootSplash = true;
        public float footSplashForce = 10f;
        public int footSplashParticles = 5;
        public Transform leftFoot;
        public Transform rightFoot;

        private Rigidbody _playerRb;
        private PlayerController _playerController;
        private bool _wasInFluid;
        private float _fluidImmersion;

        public float FluidImmersion => _fluidImmersion;

        private void Awake()
        {
            _playerRb = GetComponent<Rigidbody>();
            _playerController = GetComponent<PlayerController>();
        }

        private void FixedUpdate()
        {
            if (targetFluidSystem == null) return;

            CalculateFluidImmersion();
            ApplyFluidForces();
            PushFluidAway();
        }

        private void CalculateFluidImmersion()
        {
            if (targetFluidSystem == null || targetFluidSystem.Particles.Count == 0)
            {
                _fluidImmersion = 0f;
                return;
            }

            int nearbyParticles = 0;
            int totalChecked = 0;

            foreach (FluidParticle particle in targetFluidSystem.Particles)
            {
                if (!particle.IsActive) continue;

                float distance = Vector3.Distance(transform.position, particle.Position);
                if (distance < interactionRadius)
                {
                    nearbyParticles++;
                }

                totalChecked++;
                if (totalChecked > 100) break;
            }

            _fluidImmersion = Mathf.Clamp01((float)nearbyParticles / 20f);
        }

        private void ApplyFluidForces()
        {
            if (_fluidImmersion <= 0f) return;

            Vector3 buoyancy = Vector3.up * buoyancyStrength * _fluidImmersion;
            _playerRb.AddForce(buoyancy, ForceMode.Acceleration);

            Vector3 drag = -_playerRb.velocity * playerDragInFluid * _fluidImmersion;
            _playerRb.AddForce(drag, ForceMode.Acceleration);
        }

        private void PushFluidAway()
        {
            Vector3 playerVelocity = _playerRb.velocity;

            if (playerVelocity.magnitude < 0.1f) return;

            foreach (FluidParticle particle in targetFluidSystem.Particles)
            {
                if (!particle.IsActive) continue;

                Vector3 direction = particle.Position - transform.position;
                float distance = direction.magnitude;

                if (distance < interactionRadius && distance > 0.01f)
                {
                    float falloff = 1f - (distance / interactionRadius);
                    falloff = falloff * falloff;

                    Vector3 pushDirection = direction.normalized;
                    Vector3 velocityTransfer = playerVelocity * 0.5f;

                    particle.ApplyImpulse(pushDirection * pushForce * falloff * 0.1f);
                    particle.Velocity += velocityTransfer * falloff * 0.3f;
                }
            }
        }

        public void CreateSplash(Vector3 position, float force, int particleCount)
        {
            if (targetFluidSystem == null) return;

            targetFluidSystem.AddForceAtPosition(position, force, interactionRadius);
        }

        public void TriggerFootSplash(bool isLeftFoot)
        {
            if (!enableFootSplash || targetFluidSystem == null) return;

            Transform foot = isLeftFoot ? leftFoot : rightFoot;
            if (foot == null) return;

            if (_fluidImmersion > 0.3f)
            {
                CreateSplash(foot.position, footSplashForce, footSplashParticles);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
