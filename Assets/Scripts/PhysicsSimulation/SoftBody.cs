using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.PhysicsSimulation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SoftBody : MonoBehaviour
    {
        [Header("Soft Body Settings")]
        public int resolution = 8;
        public float size = 2f;
        public float massPerParticle = 0.5f;
        public float particleRadius = 0.25f;

        [Header("Spring Settings")]
        public float structuralStiffness = 80f;
        public float shearStiffness = 40f;
        public float bendStiffness = 20f;
        public float springDamping = 0.8f;
        public float volumeConservation = 50f;

        [Header("Gravity")]
        public float gravityScale = 1f;

        [Header("Collision")]
        public LayerMask collisionLayers;

        [Header("Rendering")]
        public bool updateMesh = true;

        private SoftBodyParticle[,,] _particleGrid;
        private List<SoftBodyParticle> _particles;
        private List<SoftBodySpring> _springs;
        private Mesh _mesh;
        private bool _isInitialized = false;
        private float _initialVolume;

        public List<SoftBodyParticle> Particles => _particles;
        public List<SoftBodySpring> Springs => _springs;

        private void Start()
        {
            InitializeSoftBody();
        }

        public void InitializeSoftBody()
        {
            if (_isInitialized) return;

            GenerateParticleGrid();
            GenerateAllSprings();
            CalculateInitialVolume();
            InitializeMesh();

            _isInitialized = true;
        }

        private void GenerateParticleGrid()
        {
            _particleGrid = new SoftBodyParticle[resolution, resolution, resolution];
            _particles = new List<SoftBodyParticle>();

            float halfSize = size * 0.5f;
            float step = size / (resolution - 1);

            int index = 0;
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        Vector3 localPos = new Vector3(
                            -halfSize + x * step,
                            -halfSize + y * step,
                            -halfSize + z * step
                        );

                        GameObject particleObj = new GameObject("Particle_" + index);
                        particleObj.transform.SetParent(transform, false);
                        particleObj.transform.localPosition = localPos;

                        SoftBodyParticle particle = particleObj.AddComponent<SoftBodyParticle>();
                        particle.mass = massPerParticle;
                        particle.radius = particleRadius;
                        particle.gravityScale = gravityScale;
                        particle.collisionLayers = collisionLayers;
                        particle.drag = 0.01f;

                        _particleGrid[x, y, z] = particle;
                        _particles.Add(particle);
                        index++;
                    }
                }
            }
        }

        private void GenerateAllSprings()
        {
            _springs = new List<SoftBodySpring>();

            GenerateStructuralSprings();
            GenerateShearSprings();
            GenerateBendSprings();
        }

        private void GenerateStructuralSprings()
        {
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        SoftBodyParticle current = _particleGrid[x, y, z];

                        if (x < resolution - 1)
                            CreateSpring(current, _particleGrid[x + 1, y, z], structuralStiffness, springDamping);

                        if (y < resolution - 1)
                            CreateSpring(current, _particleGrid[x, y + 1, z], structuralStiffness, springDamping);

                        if (z < resolution - 1)
                            CreateSpring(current, _particleGrid[x, y, z + 1], structuralStiffness, springDamping);
                    }
                }
            }
        }

        private void GenerateShearSprings()
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                for (int y = 0; y < resolution - 1; y++)
                {
                    for (int z = 0; z < resolution - 1; z++)
                    {
                        SoftBodyParticle p000 = _particleGrid[x, y, z];
                        SoftBodyParticle p110 = _particleGrid[x + 1, y + 1, z];
                        SoftBodyParticle p101 = _particleGrid[x + 1, y, z + 1];
                        SoftBodyParticle p011 = _particleGrid[x, y + 1, z + 1];

                        SoftBodyParticle p100 = _particleGrid[x + 1, y, z];
                        SoftBodyParticle p010 = _particleGrid[x, y + 1, z];
                        SoftBodyParticle p001 = _particleGrid[x, y, z + 1];
                        SoftBodyParticle p111 = _particleGrid[x + 1, y + 1, z + 1];

                        CreateSpring(p000, p110, shearStiffness, springDamping);
                        CreateSpring(p100, p010, shearStiffness, springDamping);
                        CreateSpring(p000, p101, shearStiffness, springDamping);
                        CreateSpring(p100, p001, shearStiffness, springDamping);
                        CreateSpring(p000, p011, shearStiffness, springDamping);
                        CreateSpring(p010, p001, shearStiffness, springDamping);
                        CreateSpring(p110, p101, shearStiffness, springDamping);
                        CreateSpring(p110, p011, shearStiffness, springDamping);
                        CreateSpring(p101, p011, shearStiffness, springDamping);
                        CreateSpring(p111, p100, shearStiffness, springDamping);
                        CreateSpring(p111, p010, shearStiffness, springDamping);
                        CreateSpring(p111, p001, shearStiffness, springDamping);
                    }
                }
            }
        }

        private void GenerateBendSprings()
        {
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    for (int z = 0; z < resolution; z++)
                    {
                        SoftBodyParticle current = _particleGrid[x, y, z];

                        if (x < resolution - 2)
                            CreateSpring(current, _particleGrid[x + 2, y, z], bendStiffness, springDamping);

                        if (y < resolution - 2)
                            CreateSpring(current, _particleGrid[x, y + 2, z], bendStiffness, springDamping);

                        if (z < resolution - 2)
                            CreateSpring(current, _particleGrid[x, y, z + 2], bendStiffness, springDamping);
                    }
                }
            }
        }

        private void CreateSpring(SoftBodyParticle a, SoftBodyParticle b, float stiffness, float damping)
        {
            if (a == null || b == null) return;

            float restLength = Vector3.Distance(a.Position, b.Position);

            GameObject springObj = new GameObject("Spring");
            springObj.transform.SetParent(transform, false);

            SoftBodySpring spring = springObj.AddComponent<SoftBodySpring>();
            spring.Initialize(a, b, restLength);
            spring.stiffness = stiffness;
            spring.damping = damping;

            _springs.Add(spring);
        }

        private void CalculateInitialVolume()
        {
            _initialVolume = 0f;

            for (int x = 0; x < resolution - 1; x++)
            {
                for (int y = 0; y < resolution - 1; y++)
                {
                    for (int z = 0; z < resolution - 1; z++)
                    {
                        Vector3 p0 = _particleGrid[x, y, z].Position;
                        Vector3 p1 = _particleGrid[x + 1, y + 1, z + 1].Position;

                        float cellVolume = Mathf.Abs((p1.x - p0.x) * (p1.y - p0.y) * (p1.z - p0.z));
                        _initialVolume += cellVolume;
                    }
                }
            }
        }

        private void InitializeMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh();
            _mesh.name = "SoftBodyMesh";

            GenerateSurfaceMesh();

            meshFilter.mesh = _mesh;
        }

        private void GenerateSurfaceMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            for (int face = 0; face < 6; face++)
            {
                Vector3 normal = Vector3.zero;
                int lastIndex = vertices.Count;

                switch (face)
                {
                    case 0: normal = Vector3.forward; break;
                    case 1: normal = Vector3.back; break;
                    case 2: normal = Vector3.right; break;
                    case 3: normal = Vector3.left; break;
                    case 4: normal = Vector3.up; break;
                    case 5: normal = Vector3.down; break;
                }

                for (int u = 0; u < resolution - 1; u++)
                {
                    for (int v = 0; v < resolution - 1; v++)
                    {
                        SoftBodyParticle p00 = GetSurfaceParticle(face, u, v);
                        SoftBodyParticle p10 = GetSurfaceParticle(face, u + 1, v);
                        SoftBodyParticle p01 = GetSurfaceParticle(face, u, v + 1);
                        SoftBodyParticle p11 = GetSurfaceParticle(face, u + 1, v + 1);

                        if (p00 == null || p10 == null || p01 == null || p11 == null) continue;

                        int idx0 = vertices.Count;
                        vertices.Add(transform.InverseTransformPoint(p00.Position));
                        vertices.Add(transform.InverseTransformPoint(p10.Position));
                        vertices.Add(transform.InverseTransformPoint(p01.Position));
                        vertices.Add(transform.InverseTransformPoint(p11.Position));

                        normals.Add(-normal);
                        normals.Add(-normal);
                        normals.Add(-normal);
                        normals.Add(-normal);

                        bool flip = face == 1 || face == 3 || face == 5;

                        if (flip)
                        {
                            triangles.Add(idx0 + 0);
                            triangles.Add(idx0 + 2);
                            triangles.Add(idx0 + 1);
                            triangles.Add(idx0 + 1);
                            triangles.Add(idx0 + 2);
                            triangles.Add(idx0 + 3);
                        }
                        else
                        {
                            triangles.Add(idx0 + 0);
                            triangles.Add(idx0 + 1);
                            triangles.Add(idx0 + 2);
                            triangles.Add(idx0 + 1);
                            triangles.Add(idx0 + 3);
                            triangles.Add(idx0 + 2);
                        }
                    }
                }
            }

            _mesh.vertices = vertices.ToArray();
            _mesh.triangles = triangles.ToArray();
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        private SoftBodyParticle GetSurfaceParticle(int face, int u, int v)
        {
            int last = resolution - 1;

            switch (face)
            {
                case 0: return _particleGrid[u, v, last];
                case 1: return _particleGrid[last - u, v, 0];
                case 2: return _particleGrid[last, u, v];
                case 3: return _particleGrid[0, last - u, v];
                case 4: return _particleGrid[u, last, v];
                case 5: return _particleGrid[u, 0, last - v];
                default: return null;
            }
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            SimulatePhysics(Time.fixedDeltaTime);

            if (updateMesh)
            {
                UpdateMesh();
            }

            EventBus.Publish(GameEvents.OnSoftBodyDeformed);
        }

        private void SimulatePhysics(float deltaTime)
        {
            int iterations = 3;
            float subStep = deltaTime / iterations;

            for (int iter = 0; iter < iterations; iter++)
            {
                foreach (SoftBodySpring spring in _springs)
                {
                    if (spring != null)
                    {
                        spring.UpdateSpring(subStep);
                    }
                }

                ApplyVolumeConservation();

                foreach (SoftBodyParticle particle in _particles)
                {
                    if (particle != null)
                    {
                        particle.UpdateParticle(subStep);
                    }
                }
            }
        }

        private void ApplyVolumeConservation()
        {
            if (volumeConservation <= 0f) return;

            float currentVolume = CalculateCurrentVolume();
            if (currentVolume <= 0f || _initialVolume <= 0f) return;

            float volumeRatio = currentVolume / _initialVolume;
            Vector3 center = CalculateCenter();

            float pressureForce = (1f - volumeRatio) * volumeConservation;

            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle == null || particle.IsStatic) continue;

                Vector3 direction = (particle.Position - center).normalized;
                if (direction.sqrMagnitude < 0.001f) continue;

                particle.AddForce(direction * pressureForce * particle.mass);
            }
        }

        private float CalculateCurrentVolume()
        {
            if (_particles == null || _particles.Count == 0) return 0f;

            Vector3 min = _particles[0].Position;
            Vector3 max = _particles[0].Position;

            foreach (SoftBodyParticle p in _particles)
            {
                if (p == null) continue;
                min = Vector3.Min(min, p.Position);
                max = Vector3.Max(max, p.Position);
            }

            return Mathf.Max(0f, (max.x - min.x) * (max.y - min.y) * (max.z - min.z));
        }

        public Vector3 CalculateCenter()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle != null)
                {
                    sum += particle.Position;
                    count++;
                }
            }

            return count > 0 ? sum / count : transform.position;
        }

        private void UpdateMesh()
        {
            if (_mesh == null || _particleGrid == null) return;

            Vector3[] vertices = _mesh.vertices;
            int vertIndex = 0;

            for (int face = 0; face < 6; face++)
            {
                for (int u = 0; u < resolution - 1; u++)
                {
                    for (int v = 0; v < resolution - 1; v++)
                    {
                        SoftBodyParticle p00 = GetSurfaceParticle(face, u, v);
                        SoftBodyParticle p10 = GetSurfaceParticle(face, u + 1, v);
                        SoftBodyParticle p01 = GetSurfaceParticle(face, u, v + 1);
                        SoftBodyParticle p11 = GetSurfaceParticle(face, u + 1, v + 1);

                        if (p00 == null || p10 == null || p01 == null || p11 == null) continue;

                        vertices[vertIndex + 0] = transform.InverseTransformPoint(p00.Position);
                        vertices[vertIndex + 1] = transform.InverseTransformPoint(p10.Position);
                        vertices[vertIndex + 2] = transform.InverseTransformPoint(p01.Position);
                        vertices[vertIndex + 3] = transform.InverseTransformPoint(p11.Position);

                        vertIndex += 4;
                    }
                }
            }

            _mesh.vertices = vertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        public void AddForce(Vector3 force)
        {
            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle != null && !particle.IsStatic)
                {
                    particle.AddForce(force);
                }
            }
        }

        public void AddExplosionForce(Vector3 position, float force, float radius)
        {
            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle == null || particle.IsStatic) continue;

                Vector3 direction = particle.Position - position;
                float distance = direction.magnitude;

                if (distance < radius)
                {
                    float falloff = 1f - (distance / radius);
                    particle.AddImpulse(direction.normalized * force * falloff);
                }
            }
        }

        public void SetParticleStatic(int index, bool isStatic)
        {
            if (index >= 0 && index < _particles.Count && _particles[index] != null)
            {
                _particles[index].SetStatic(isStatic);
            }
        }

        public void ResetSoftBody()
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                if (_particles[i] != null)
                {
                    _particles[i].ResetVelocity();
                }
            }

            foreach (SoftBodySpring spring in _springs)
            {
                if (spring != null)
                {
                    spring.Repair();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_particles == null) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle != null)
                {
                    Gizmos.DrawWireSphere(particle.Position, particle.radius * 0.5f);
                }
            }

            if (_springs != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
                foreach (SoftBodySpring spring in _springs)
                {
                    if (spring != null && !spring.IsBroken && spring.particleA != null && spring.particleB != null)
                    {
                        Gizmos.DrawLine(spring.particleA.Position, spring.particleB.Position);
                    }
                }
            }
        }
    }
}
