using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.PhysicsSimulation
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SoftBody : MonoBehaviour
    {
        [Header("Soft Body Settings")]
        public int resolution = 10;
        public float radius = 1f;
        public float massPerParticle = 1f;
        public float particleRadius = 0.3f;
        public float stiffness = 100f;
        public float damping = 2f;
        public float pressure = 50f;

        [Header("Gravity")]
        public float gravityScale = 1f;

        [Header("Collision")]
        public LayerMask collisionLayers;

        [Header("Rendering")]
        public bool updateMesh = true;

        private SoftBodyParticle[] _particles;
        private SoftBodySpring[] _springs;
        private Mesh _mesh;
        private Vector3[] _originalVertices;
        private int[] _triangles;
        private bool _isInitialized = false;

        public SoftBodyParticle[] Particles => _particles;
        public SoftBodySpring[] Springs => _springs;

        private void Start()
        {
            InitializeSoftBody();
        }

        public void InitializeSoftBody()
        {
            if (_isInitialized) return;

            GenerateParticles();
            GenerateSprings();
            InitializeMesh();

            _isInitialized = true;
        }

        private void GenerateParticles()
        {
            int totalParticles = resolution * resolution * 6;
            _particles = new SoftBodyParticle[totalParticles];

            int index = 0;
            float halfRes = (resolution - 1) * 0.5f;

            Vector3[] faceDirections = {
                Vector3.forward, Vector3.back,
                Vector3.right, Vector3.left,
                Vector3.up, Vector3.down
            };

            foreach (Vector3 faceDir in faceDirections)
            {
                Vector3 uAxis, vAxis;
                GetFaceAxes(faceDir, out uAxis, out vAxis);

                for (int u = 0; u < resolution; u++)
                {
                    for (int v = 0; v < resolution; v++)
                    {
                        float uNorm = (u - halfRes) / halfRes;
                        float vNorm = (v - halfRes) / halfRes;

                        Vector3 localPos = faceDir * radius +
                                          uAxis * uNorm * radius +
                                          vAxis * vNorm * radius;

                        localPos = localPos.normalized * radius;

                        GameObject particleObj = new GameObject("Particle_" + index);
                        particleObj.transform.SetParent(transform, false);
                        particleObj.transform.localPosition = localPos;

                        SoftBodyParticle particle = particleObj.AddComponent<SoftBodyParticle>();
                        particle.mass = massPerParticle;
                        particle.radius = particleRadius;
                        particle.gravityScale = gravityScale;
                        particle.collisionLayers = collisionLayers;

                        _particles[index] = particle;
                        index++;
                    }
                }
            }
        }

        private void GetFaceAxes(Vector3 faceDir, out Vector3 uAxis, out Vector3 vAxis)
        {
            if (faceDir == Vector3.forward || faceDir == Vector3.back)
            {
                uAxis = Vector3.right;
                vAxis = Vector3.up;
            }
            else if (faceDir == Vector3.right || faceDir == Vector3.left)
            {
                uAxis = Vector3.forward;
                vAxis = Vector3.up;
            }
            else
            {
                uAxis = Vector3.right;
                vAxis = Vector3.forward;
            }
        }

        private void GenerateSprings()
        {
            List<SoftBodySpring> springList = new List<SoftBodySpring>();
            int particlesPerFace = resolution * resolution;

            for (int face = 0; face < 6; face++)
            {
                int faceStart = face * particlesPerFace;

                for (int u = 0; u < resolution; u++)
                {
                    for (int v = 0; v < resolution; v++)
                    {
                        int idx = faceStart + u * resolution + v;

                        if (u < resolution - 1)
                        {
                            int nextIdx = faceStart + (u + 1) * resolution + v;
                            CreateSpring(_particles[idx], _particles[nextIdx], springList);
                        }

                        if (v < resolution - 1)
                        {
                            int nextIdx = faceStart + u * resolution + (v + 1);
                            CreateSpring(_particles[idx], _particles[nextIdx], springList);
                        }

                        if (u < resolution - 1 && v < resolution - 1)
                        {
                            int diagIdx = faceStart + (u + 1) * resolution + (v + 1);
                            CreateSpring(_particles[idx], _particles[diagIdx], springList);
                        }

                        if (u < resolution - 1 && v > 0)
                        {
                            int diagIdx = faceStart + (u + 1) * resolution + (v - 1);
                            CreateSpring(_particles[idx], _particles[diagIdx], springList);
                        }
                    }
                }
            }

            ConnectAdjacentFaces(springList);

            _springs = springList.ToArray();
        }

        private void CreateSpring(SoftBodyParticle a, SoftBodyParticle b, List<SoftBodySpring> springList)
        {
            if (a == null || b == null) return;

            float restLength = Vector3.Distance(a.Position, b.Position);

            GameObject springObj = new GameObject("Spring");
            springObj.transform.SetParent(transform, false);

            SoftBodySpring spring = springObj.AddComponent<SoftBodySpring>();
            spring.Initialize(a, b, restLength);
            spring.stiffness = stiffness;
            spring.damping = damping;

            springList.Add(spring);
        }

        private void ConnectAdjacentFaces(List<SoftBodySpring> springList)
        {
            int p = particlesPerFace;
            int r = resolution;

            for (int i = 0; i < resolution; i++)
            {
                int frontRight = 0 * p + i * r + (r - 1);
                int rightFront = 2 * p + i * r + 0;
                if (_particles[frontRight] != null && _particles[rightFront] != null)
                    CreateSpring(_particles[frontRight], _particles[rightFront], springList);

                int rightBack = 2 * p + i * r + (r - 1);
                int backRight = 1 * p + i * r + 0;
                if (_particles[rightBack] != null && _particles[backRight] != null)
                    CreateSpring(_particles[rightBack], _particles[backRight], springList);

                int topFront = 4 * p + (r - 1) * r + i;
                int frontTop = 0 * p + (r - 1) * r + i;
                if (_particles[topFront] != null && _particles[frontTop] != null)
                    CreateSpring(_particles[topFront], _particles[frontTop], springList);

                int topRight = 4 * p + i * r + (r - 1);
                int rightTop = 2 * p + (r - 1) * r + i;
                if (_particles[topRight] != null && _particles[rightTop] != null)
                    CreateSpring(_particles[topRight], _particles[rightTop], springList);
            }
        }

        private int particlesPerFace => resolution * resolution;

        private void InitializeMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh();
            _mesh.name = "SoftBodyMesh";

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            int p = particlesPerFace;
            int r = resolution;

            for (int face = 0; face < 6; face++)
            {
                int faceStart = face * p;

                for (int u = 0; u < r - 1; u++)
                {
                    for (int v = 0; v < r - 1; v++)
                    {
                        int idx0 = faceStart + u * r + v;
                        int idx1 = faceStart + (u + 1) * r + v;
                        int idx2 = faceStart + u * r + (v + 1);
                        int idx3 = faceStart + (u + 1) * r + (v + 1);

                        int vertStart = vertices.Count;

                        vertices.Add(_particles[idx0].Position - transform.position);
                        vertices.Add(_particles[idx1].Position - transform.position);
                        vertices.Add(_particles[idx2].Position - transform.position);
                        vertices.Add(_particles[idx3].Position - transform.position);

                        bool reverseFace = face % 2 == 1;

                        if (reverseFace)
                        {
                            triangles.Add(vertStart + 0);
                            triangles.Add(vertStart + 2);
                            triangles.Add(vertStart + 1);
                            triangles.Add(vertStart + 1);
                            triangles.Add(vertStart + 2);
                            triangles.Add(vertStart + 3);
                        }
                        else
                        {
                            triangles.Add(vertStart + 0);
                            triangles.Add(vertStart + 1);
                            triangles.Add(vertStart + 2);
                            triangles.Add(vertStart + 1);
                            triangles.Add(vertStart + 3);
                            triangles.Add(vertStart + 2);
                        }
                    }
                }
            }

            _mesh.vertices = vertices.ToArray();
            _mesh.triangles = triangles.ToArray();
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _originalVertices = (Vector3[])_mesh.vertices.Clone();
            _triangles = (int[])_mesh.triangles.Clone();

            meshFilter.mesh = _mesh;
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
            foreach (SoftBodySpring spring in _springs)
            {
                if (spring != null)
                {
                    spring.UpdateSpring(deltaTime);
                }
            }

            ApplyPressure();

            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle != null)
                {
                    particle.UpdateParticle(deltaTime);
                }
            }
        }

        private void ApplyPressure()
        {
            if (pressure <= 0f) return;

            Vector3 center = CalculateCenter();

            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle == null || particle.IsStatic) continue;

                Vector3 direction = (particle.Position - center).normalized;
                particle.AddForce(direction * pressure);
            }
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
            if (_mesh == null || _particles == null) return;

            Vector3[] vertices = _mesh.vertices;
            int p = particlesPerFace;
            int r = resolution;
            int vertIndex = 0;

            for (int face = 0; face < 6; face++)
            {
                int faceStart = face * p;

                for (int u = 0; u < r - 1; u++)
                {
                    for (int v = 0; v < r - 1; v++)
                    {
                        int idx0 = faceStart + u * r + v;
                        int idx1 = faceStart + (u + 1) * r + v;
                        int idx2 = faceStart + u * r + (v + 1);
                        int idx3 = faceStart + (u + 1) * r + (v + 1);

                        vertices[vertIndex + 0] = transform.InverseTransformPoint(_particles[idx0].Position);
                        vertices[vertIndex + 1] = transform.InverseTransformPoint(_particles[idx1].Position);
                        vertices[vertIndex + 2] = transform.InverseTransformPoint(_particles[idx2].Position);
                        vertices[vertIndex + 3] = transform.InverseTransformPoint(_particles[idx3].Position);

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
            if (index >= 0 && index < _particles.Length && _particles[index] != null)
            {
                _particles[index].SetStatic(isStatic);
            }
        }

        public void ResetSoftBody()
        {
            for (int i = 0; i < _particles.Length; i++)
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

            Gizmos.color = Color.cyan;
            foreach (SoftBodyParticle particle in _particles)
            {
                if (particle != null)
                {
                    Gizmos.DrawWireSphere(particle.Position, particle.radius);
                }
            }

            if (_springs != null)
            {
                Gizmos.color = Color.green;
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
