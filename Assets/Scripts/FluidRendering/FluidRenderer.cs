using UnityEngine;
using System.Collections.Generic;

namespace SoftFluidPuzzle.FluidRendering
{
    [ExecuteInEditMode]
    public class FluidRenderer : MonoBehaviour
    {
        [Header("References")]
        public FluidSystem fluidSystem;
        public Material particleMaterial;

        [Header("Rendering")]
        public float particleSize = 0.4f;
        public Color particleColor = new Color(0.2f, 0.5f, 1f, 0.6f);
        public bool useInstancing = true;
        public bool billboard = true;

        [Header("Soft Particle")]
        public float softParticleFade = 0.3f;
        public float glowIntensity = 0.5f;

        [Header("Fade by Density")]
        public bool fadeByDensity = true;
        public float minOpacity = 0.2f;
        public float maxOpacity = 0.7f;

        private Mesh _particleMesh;
        private Matrix4x4[] _particleMatrices;
        private Vector4[] _particleColors;
        private MaterialPropertyBlock _propertyBlock;
        private const int MaxInstanceCount = 1023;
        private Camera _mainCamera;
        private Texture2D _softParticleTexture;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _mainCamera = Camera.main;
            CreateSoftParticleTexture();
            CreateParticleMesh();
        }

        private void CreateSoftParticleTexture()
        {
            int size = 128;
            _softParticleTexture = new Texture2D(size, size, TextureFormat.Alpha8, false);
            _softParticleTexture.wrapMode = TextureWrapMode.Clamp;
            _softParticleTexture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;

                    if (dist < radius)
                    {
                        float t = 1f - (dist / radius);
                        alpha = Mathf.SmoothStep(0f, 1f, t);
                        alpha = Mathf.Pow(alpha, 1.5f);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }

            _softParticleTexture.SetPixels32(pixels);
            _softParticleTexture.Apply();
        }

        private void CreateParticleMesh()
        {
            _particleMesh = new Mesh();
            _particleMesh.name = "FluidParticleMesh";

            Vector3[] vertices = new Vector3[4];
            Vector2[] uvs = new Vector2[4];
            int[] triangles = new int[6];

            vertices[0] = new Vector3(-0.5f, -0.5f, 0f);
            vertices[1] = new Vector3(0.5f, -0.5f, 0f);
            vertices[2] = new Vector3(-0.5f, 0.5f, 0f);
            vertices[3] = new Vector3(0.5f, 0.5f, 0f);

            uvs[0] = new Vector2(0f, 0f);
            uvs[1] = new Vector2(1f, 0f);
            uvs[2] = new Vector2(0f, 1f);
            uvs[3] = new Vector2(1f, 1f);

            triangles[0] = 0;
            triangles[1] = 2;
            triangles[2] = 1;
            triangles[3] = 1;
            triangles[4] = 2;
            triangles[5] = 3;

            _particleMesh.vertices = vertices;
            _particleMesh.uv = uvs;
            _particleMesh.triangles = triangles;
            _particleMesh.RecalculateNormals();
            _particleMesh.RecalculateBounds();
            _particleMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
        }

        private void Update()
        {
            if (fluidSystem == null || fluidSystem.Particles == null) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            EnsureMaterialSetup();
            RenderParticles();
        }

        private void EnsureMaterialSetup()
        {
            if (particleMaterial == null) return;

            if (_softParticleTexture != null && particleMaterial.GetTexture("_MainTex") == null)
            {
                particleMaterial.SetTexture("_MainTex", _softParticleTexture);
            }
        }

        private void RenderParticles()
        {
            List<FluidParticle> particles = fluidSystem.Particles;
            int particleCount = particles.Count;

            if (particleCount == 0) return;
            if (particleMaterial == null || _particleMesh == null) return;

            if (_particleMatrices == null || _particleMatrices.Length != particleCount)
            {
                _particleMatrices = new Matrix4x4[particleCount];
                _particleColors = new Vector4[particleCount];
            }

            Quaternion billboardRotation = Quaternion.identity;
            if (billboard && _mainCamera != null)
            {
                billboardRotation = Quaternion.LookRotation(_mainCamera.transform.forward, _mainCamera.transform.up);
            }

            float avgDensity = 0f;
            if (fadeByDensity && particles.Count > 0)
            {
                for (int i = 0; i < particles.Count; i++)
                {
                    avgDensity += particles[i].Density;
                }
                avgDensity /= particles.Count;
            }

            for (int i = 0; i < particleCount; i++)
            {
                FluidParticle particle = particles[i];
                if (!particle.IsActive)
                {
                    _particleMatrices[i] = Matrix4x4.identity;
                    continue;
                }

                float size = particleSize * particle.Radius * 2f;
                Vector3 scale = new Vector3(size, size, size);
                _particleMatrices[i] = Matrix4x4.TRS(particle.Position, billboardRotation, scale);

                Color finalColor = particle.Color;
                if (fadeByDensity && avgDensity > 0f)
                {
                    float densityFactor = Mathf.Clamp01(particle.Density / (avgDensity * 1.5f));
                    float alpha = Mathf.Lerp(minOpacity, maxOpacity, densityFactor);
                    finalColor.a = alpha * particleColor.a;
                }
                else
                {
                    finalColor.a = particleColor.a;
                }

                _particleColors[i] = new Vector4(finalColor.r, finalColor.g, finalColor.b, finalColor.a);
            }

            if (useInstancing && particleMaterial.enableInstancing)
            {
                RenderInstanced(particleCount);
            }
            else
            {
                RenderSingle(particleCount);
            }
        }

        private void RenderInstanced(int particleCount)
        {
            int batches = Mathf.CeilToInt((float)particleCount / MaxInstanceCount);

            for (int batch = 0; batch < batches; batch++)
            {
                int startIndex = batch * MaxInstanceCount;
                int count = Mathf.Min(MaxInstanceCount, particleCount - startIndex);

                Matrix4x4[] batchMatrices = new Matrix4x4[count];
                Vector4[] batchColors = new Vector4[count];

                System.Array.Copy(_particleMatrices, startIndex, batchMatrices, 0, count);
                System.Array.Copy(_particleColors, startIndex, batchColors, 0, count);

                _propertyBlock.Clear();
                _propertyBlock.SetVectorArray("_InstanceColor", batchColors);
                _propertyBlock.SetFloat("_SoftFade", softParticleFade);
                _propertyBlock.SetFloat("_GlowIntensity", glowIntensity);

                Graphics.DrawMeshInstanced(
                    _particleMesh,
                    0,
                    particleMaterial,
                    batchMatrices,
                    count,
                    _propertyBlock,
                    UnityEngine.Rendering.ShadowCastingMode.Off,
                    false,
                    gameObject.layer
                );
            }
        }

        private void RenderSingle(int particleCount)
        {
            for (int i = 0; i < particleCount; i++)
            {
                FluidParticle particle = fluidSystem.Particles[i];
                if (!particle.IsActive) continue;

                _propertyBlock.Clear();
                _propertyBlock.SetColor("_Color", particle.Color);
                _propertyBlock.SetFloat("_SoftFade", softParticleFade);
                _propertyBlock.SetFloat("_GlowIntensity", glowIntensity);

                Graphics.DrawMesh(
                    _particleMesh,
                    _particleMatrices[i],
                    particleMaterial,
                    gameObject.layer,
                    null,
                    0,
                    _propertyBlock
                );
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

        private void OnDestroy()
        {
            if (_softParticleTexture != null)
            {
                DestroyImmediate(_softParticleTexture);
            }
            if (_particleMesh != null)
            {
                DestroyImmediate(_particleMesh);
            }
        }
    }
}
