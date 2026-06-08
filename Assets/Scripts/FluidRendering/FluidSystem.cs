using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.FluidRendering
{
    public class FluidSystem : MonoBehaviour
    {
        [Header("Fluid Settings")]
        public int maxParticles = 1000;
        public float particleRadius = 0.15f;
        public float particleMass = 1f;
        public float restDensity = 1000f;
        public float pressureCoefficient = 200f;
        public float viscosityCoefficient = 50f;
        public float surfaceTension = 0.5f;

        [Header("Physics")]
        public Vector3 gravity = new Vector3(0, -9.81f, 0);
        public float drag = 0.05f;

        [Header("Collision")]
        public LayerMask collisionLayers;
        public float collisionDamping = 0.6f;

        [Header("SPH Settings")]
        public float smoothingRadius = 0.5f;

        private List<FluidParticle> _particles = new List<FluidParticle>();
        private SpatialGrid _spatialGrid;

        public List<FluidParticle> Particles => _particles;
        public int ParticleCount => _particles.Count;

        private void Awake()
        {
            _spatialGrid = new SpatialGrid(smoothingRadius * 2f);
        }

        public void EmitParticles(Vector3 position, int count, Vector3 initialVelocity = default(Vector3))
        {
            for (int i = 0; i < count && _particles.Count < maxParticles; i++)
            {
                Vector3 offset = Random.insideUnitSphere * particleRadius * 2f;
                FluidParticle particle = new FluidParticle(position + offset, particleMass, particleRadius);
                particle.Velocity = initialVelocity + Random.insideUnitSphere * 0.5f;
                _particles.Add(particle);
            }
        }

        public void EmitParticlesInVolume(Bounds volume, int count)
        {
            for (int i = 0; i < count && _particles.Count < maxParticles; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(volume.min.x, volume.max.x),
                    Random.Range(volume.min.y, volume.max.y),
                    Random.Range(volume.min.z, volume.max.z)
                );
                FluidParticle particle = new FluidParticle(randomPos, particleMass, particleRadius);
                _particles.Add(particle);
            }
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            _spatialGrid.BuildGrid(_particles);

            ComputeDensityPressure();

            ComputeForces();

            UpdateParticles(deltaTime);

            HandleCollisions();

            EventBus.Publish(GameEvents.OnFluidVolumeChanged, new FluidVolumeChangedArgs
            {
                CurrentVolume = _particles.Count * particleMass,
                TargetVolume = maxParticles * particleMass
            });
        }

        private void ComputeDensityPressure()
        {
            float h2 = smoothingRadius * smoothingRadius;
            float h9 = Mathf.Pow(smoothingRadius, 9);
            float poly6Constant = 315f / (64f * Mathf.PI * Mathf.Pow(smoothingRadius, 9));

            for (int i = 0; i < _particles.Count; i++)
            {
                FluidParticle pi = _particles[i];
                pi.Density = 0f;

                List<FluidParticle> neighbors = _spatialGrid.GetNeighbors(pi.Position);
                foreach (FluidParticle pj in neighbors)
                {
                    Vector3 diff = pj.Position - pi.Position;
                    float r2 = diff.sqrMagnitude;

                    if (r2 < h2)
                    {
                        float hr2 = h2 - r2;
                        pi.Density += pj.Mass * poly6Constant * hr2 * hr2 * hr2;
                    }
                }

                pi.Density = Mathf.Max(pi.Density, restDensity * 0.1f);
                pi.Pressure = pressureCoefficient * (pi.Density - restDensity);
            }
        }

        private void ComputeForces()
        {
            float h2 = smoothingRadius * smoothingRadius;
            float spikyConstant = -45f / (Mathf.PI * Mathf.Pow(smoothingRadius, 6));
            float viscosityConstant = 45f / (Mathf.PI * Mathf.Pow(smoothingRadius, 6));

            for (int i = 0; i < _particles.Count; i++)
            {
                FluidParticle pi = _particles[i];
                Vector3 pressureForce = Vector3.zero;
                Vector3 viscosityForce = Vector3.zero;

                List<FluidParticle> neighbors = _spatialGrid.GetNeighbors(pi.Position);
                foreach (FluidParticle pj in neighbors)
                {
                    if (pi == pj) continue;

                    Vector3 diff = pj.Position - pi.Position;
                    float r = diff.magnitude;

                    if (r < smoothingRadius && r > 0.001f)
                    {
                        float hr = smoothingRadius - r;
                        pressureForce += -diff.normalized * pj.Mass * (pi.Pressure + pj.Pressure) / (2f * pj.Density) * spikyConstant * hr * hr;
                        viscosityForce += viscosityCoefficient * pj.Mass * (pj.Velocity - pi.Velocity) / pj.Density * viscosityConstant * hr;
                    }
                }

                pi.ApplyForce(pressureForce / pi.Density);
                pi.ApplyForce(viscosityForce / pi.Density);
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            foreach (FluidParticle particle in _particles)
            {
                particle.Update(deltaTime, gravity, drag);
            }
        }

        private void HandleCollisions()
        {
            Collider[] hitColliders = new Collider[10];

            for (int i = 0; i < _particles.Count; i++)
            {
                FluidParticle particle = _particles[i];
                if (!particle.IsActive) continue;

                int hitCount = Physics.OverlapSphereNonAlloc(particle.Position, particleRadius, hitColliders, collisionLayers);

                for (int j = 0; j < hitCount; j++)
                {
                    Collider collider = hitColliders[j];

                    Vector3 closestPoint = collider.ClosestPoint(particle.Position);
                    Vector3 direction = particle.Position - closestPoint;
                    float distance = direction.magnitude;

                    if (distance < particleRadius && distance > 0.001f)
                    {
                        Vector3 normal = direction / distance;
                        float penetration = particleRadius - distance;

                        particle.Position += normal * penetration;

                        float velocityAlongNormal = Vector3.Dot(particle.Velocity, normal);
                        if (velocityAlongNormal < 0)
                        {
                            particle.Velocity -= velocityAlongNormal * normal * (1f + collisionDamping);
                        }
                    }
                    else if (distance <= 0.001f)
                    {
                        Vector3 pushDir = (particle.Position - collider.transform.position).normalized;
                        if (pushDir == Vector3.zero) pushDir = Vector3.up;
                        particle.Position += pushDir * particleRadius;
                        particle.Velocity = Vector3.Reflect(particle.Velocity, pushDir) * collisionDamping;
                    }
                }
            }
        }

        public void RemoveParticle(int index)
        {
            if (index >= 0 && index < _particles.Count)
            {
                _particles.RemoveAt(index);
            }
        }

        public void ClearParticles()
        {
            _particles.Clear();
        }

        public void AddForceAtPosition(Vector3 position, float force, float radius)
        {
            foreach (FluidParticle particle in _particles)
            {
                if (!particle.IsActive) continue;

                Vector3 direction = particle.Position - position;
                float distance = direction.magnitude;

                if (distance < radius && distance > 0.001f)
                {
                    float falloff = 1f - (distance / radius);
                    particle.ApplyImpulse(direction.normalized * force * falloff);
                }
            }
        }

        public void AddVortexForce(Vector3 center, float strength, float radius)
        {
            foreach (FluidParticle particle in _particles)
            {
                if (!particle.IsActive) continue;

                Vector3 toParticle = particle.Position - center;
                float distance = toParticle.magnitude;

                if (distance < radius && distance > 0.001f)
                {
                    Vector3 tangent = Vector3.Cross(Vector3.up, toParticle).normalized;
                    float falloff = 1f - (distance / radius);
                    particle.ApplyForce(tangent * strength * falloff);
                }
            }
        }

        public float GetFluidVolume()
        {
            return _particles.Count * particleMass;
        }
    }

    public class SpatialGrid
    {
        private float _cellSize;
        private Dictionary<int, List<FluidParticle>> _grid = new Dictionary<int, List<FluidParticle>>();

        public SpatialGrid(float cellSize)
        {
            _cellSize = cellSize;
        }

        public void BuildGrid(List<FluidParticle> particles)
        {
            _grid.Clear();

            foreach (FluidParticle particle in particles)
            {
                int hash = GetHash(particle.Position);

                if (!_grid.ContainsKey(hash))
                {
                    _grid[hash] = new List<FluidParticle>();
                }

                _grid[hash].Add(particle);
            }
        }

        public List<FluidParticle> GetNeighbors(Vector3 position)
        {
            List<FluidParticle> neighbors = new List<FluidParticle>();

            int cx = Mathf.FloorToInt(position.x / _cellSize);
            int cy = Mathf.FloorToInt(position.y / _cellSize);
            int cz = Mathf.FloorToInt(position.z / _cellSize);

            for (int x = cx - 1; x <= cx + 1; x++)
            {
                for (int y = cy - 1; y <= cy + 1; y++)
                {
                    for (int z = cz - 1; z <= cz + 1; z++)
                    {
                        int hash = GetHash(x, y, z);
                        if (_grid.TryGetValue(hash, out List<FluidParticle> cellParticles))
                        {
                            neighbors.AddRange(cellParticles);
                        }
                    }
                }
            }

            return neighbors;
        }

        private int GetHash(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / _cellSize);
            int y = Mathf.FloorToInt(position.y / _cellSize);
            int z = Mathf.FloorToInt(position.z / _cellSize);
            return GetHash(x, y, z);
        }

        private int GetHash(int x, int y, int z)
        {
            unchecked
            {
                int hash = x;
                hash = (hash * 397) ^ y;
                hash = (hash * 397) ^ z;
                return hash;
            }
        }
    }
}
