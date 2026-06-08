using UnityEngine;
using System.Collections.Generic;

namespace SoftFluidPuzzle.FluidRendering
{
    public class FluidRenderer : MonoBehaviour
    {
        [Header("References")]
        public FluidSystem fluidSystem;
        public Material particleMaterial;

        [Header("Rendering")]
        public float particleSize = 0.3f;
        public Color particleColor = new Color(0.2f, 0.5f, 1f, 0.8f);
        public bool useInstancing = true;

        [Header("Fade")]
        public float minOpacity = 0.3f;
        public float maxOpacity = 0.9f;

        private Mesh _particleMesh;
        private Matrix4x4[] _particleMatrices;
        private MaterialPropertyBlock _propertyBlock;
        private const int MaxInstanceCount = 1023;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            CreateParticleMesh();
        }

        private void CreateParticleMesh()
        {
            _particleMesh = new Mesh();
            _particleMesh.name = "FluidParticleMesh";

            int segments = 8;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.5f;
                vertices.Add(pos);
                uvs.Add(new Vector2(pos.x + 0.5f, pos.y + 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(0);
                triangles.Add(i + 1);
                triangles.Add(next + 1);
            }

            _particleMesh.vertices = vertices.ToArray();
            _particleMesh.triangles = triangles.ToArray();
            _particleMesh.uv = uvs.ToArray();
            _particleMesh.RecalculateNormals();
            _particleMesh.RecalculateBounds();
        }

        private void Update()
        {
            if (fluidSystem == null || fluidSystem.Particles == null) return;
            if (particleMaterial == null || _particleMesh == null) return;

            RenderParticles();
        }

        private void RenderParticles()
        {
            List<FluidParticle> particles = fluidSystem.Particles;
            int particleCount = particles.Count;

            if (particleCount == 0) return;

            if (_particleMatrices == null || _particleMatrices.Length != particleCount)
            {
                _particleMatrices = new Matrix4x4[particleCount];
            }

            for (int i = 0; i < particleCount; i++)
            {
                FluidParticle particle = particles[i];
                if (!particle.IsActive) continue;

                Vector3 scale = Vector3.one * particleSize * particle.Radius * 2f;
                _particleMatrices[i] = Matrix4x4.TRS(particle.Position, Quaternion.identity, scale);
            }

            _propertyBlock.SetColor("_Color", particleColor);

            if (useInstancing && particleMaterial.enableInstancing)
            {
                int batches = Mathf.CeilToInt((float)particleCount / MaxInstanceCount);

                for (int batch = 0; batch < batches; batch++)
                {
                    int startIndex = batch * MaxInstanceCount;
                    int count = Mathf.Min(MaxInstanceCount, particleCount - startIndex);

                    Matrix4x4[] batchMatrices = new Matrix4x4[count];
                    System.Array.Copy(_particleMatrices, startIndex, batchMatrices, 0, count);

                    Graphics.DrawMeshInstanced(_particleMesh, 0, particleMaterial, batchMatrices, count, _propertyBlock);
                }
            }
            else
            {
                for (int i = 0; i < particleCount; i++)
                {
                    FluidParticle particle = particles[i];
                    if (!particle.IsActive) continue;

                    Graphics.DrawMesh(_particleMesh, _particleMatrices[i], particleMaterial, gameObject.layer, null, 0, _propertyBlock);
                }
            }
        }

        public void SetParticleColor(Color color)
        {
            particleColor = color;
        }

        public void SetParticleSize(float size)
        {
            particleSize = size;
        }
    }
}
