using UnityEngine;
using SoftFluidPuzzle.PhysicsSimulation;

namespace SoftFluidPuzzle.FluidRendering
{
    public class FluidSoftBodyInteraction : MonoBehaviour
    {
        [Header("References")]
        public FluidSystem fluidSystem;
        public SoftBody softBody;

        [Header("Interaction Settings")]
        public float interactionRadius = 0.5f;
        public float fluidPushForce = 10f;
        public float softBodyPushForce = 5f;
        public float buoyancyForce = 50f;
        public float dragForce = 2f;

        [Header("Collision Response")]
        public bool enableFluidToSoftBody = true;
        public bool enableSoftBodyToFluid = true;

        private void FixedUpdate()
        {
            if (fluidSystem == null || softBody == null) return;

            if (enableFluidToSoftBody)
            {
                ApplyFluidForceOnSoftBody();
            }

            if (enableSoftBodyToFluid)
            {
                ApplySoftBodyForceOnFluid();
            }
        }

        private void ApplyFluidForceOnSoftBody()
        {
            if (softBody.Particles == null) return;

            foreach (SoftBodyParticle softParticle in softBody.Particles)
            {
                if (softParticle == null || softParticle.IsStatic) continue;

                Vector3 totalForce = Vector3.zero;
                float totalWeight = 0f;

                foreach (FluidParticle fluidParticle in fluidSystem.Particles)
                {
                    if (!fluidParticle.IsActive) continue;

                    Vector3 direction = softParticle.Position - fluidParticle.Position;
                    float distance = direction.magnitude;

                    if (distance < interactionRadius + fluidParticle.Radius && distance > 0.001f)
                    {
                        float weight = 1f - (distance / (interactionRadius + fluidParticle.Radius));
                        weight = weight * weight;

                        totalForce += direction.normalized * fluidPushForce * weight * fluidParticle.Mass;

                        totalWeight += weight;
                    }
                }

                if (totalWeight > 0f)
                {
                    softParticle.AddForce(totalForce);

                    Vector3 buoyancy = Vector3.up * buoyancyForce * totalWeight;
                    softParticle.AddForce(buoyancy);

                    Vector3 drag = -softParticle.Velocity * dragForce * totalWeight;
                    softParticle.AddForce(drag);
                }
            }
        }

        private void ApplySoftBodyForceOnFluid()
        {
            if (softBody.Particles == null) return;

            foreach (FluidParticle fluidParticle in fluidSystem.Particles)
            {
                if (!fluidParticle.IsActive) continue;

                Vector3 totalForce = Vector3.zero;
                float totalWeight = 0f;

                foreach (SoftBodyParticle softParticle in softBody.Particles)
                {
                    if (softParticle == null) continue;

                    Vector3 direction = fluidParticle.Position - softParticle.Position;
                    float distance = direction.magnitude;

                    if (distance < interactionRadius + softParticle.radius && distance > 0.001f)
                    {
                        float weight = 1f - (distance / (interactionRadius + softParticle.radius));
                        weight = weight * weight;

                        totalForce += direction.normalized * softBodyPushForce * weight * softParticle.mass;
                        totalForce += softParticle.Velocity * weight * 0.1f;

                        totalWeight += weight;
                    }
                }

                if (totalWeight > 0f)
                {
                    fluidParticle.ApplyForce(totalForce);
                }
            }
        }
    }
}
