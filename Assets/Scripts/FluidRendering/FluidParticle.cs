using UnityEngine;

namespace SoftFluidPuzzle.FluidRendering
{
    public class FluidParticle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Density;
        public float Pressure;
        public float Mass;
        public float Radius;
        public Color Color;
        public bool IsActive;

        public FluidParticle(Vector3 position, float mass, float radius)
        {
            Position = position;
            Velocity = Vector3.zero;
            Density = 0f;
            Pressure = 0f;
            Mass = mass;
            Radius = radius;
            Color = Color.blue;
            IsActive = true;
        }

        public void Update(float deltaTime, Vector3 gravity, float drag)
        {
            if (!IsActive) return;

            Velocity += gravity * deltaTime;
            Velocity *= (1f - drag);
            Position += Velocity * deltaTime;
        }

        public void ApplyForce(Vector3 force)
        {
            if (!IsActive) return;
            Velocity += force / Mass;
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            if (!IsActive) return;
            Velocity += impulse / Mass;
        }
    }
}
