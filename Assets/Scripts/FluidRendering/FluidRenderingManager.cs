using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;

namespace SoftFluidPuzzle.FluidRendering
{
    public class FluidRenderingManager : Singleton<FluidRenderingManager>
    {
        [Header("Global Fluid Settings")]
        public float globalFluidDensity = 1000f;
        public float globalViscosity = 50f;

        private List<FluidSystem> _fluidSystems = new List<FluidSystem>();
        private List<FluidRenderer> _fluidRenderers = new List<FluidRenderer>();

        public List<FluidSystem> FluidSystems => _fluidSystems;
        public List<FluidRenderer> FluidRenderers => _fluidRenderers;

        public void RegisterFluidSystem(FluidSystem system)
        {
            if (!_fluidSystems.Contains(system))
            {
                _fluidSystems.Add(system);
            }
        }

        public void UnregisterFluidSystem(FluidSystem system)
        {
            _fluidSystems.Remove(system);
        }

        public void RegisterFluidRenderer(FluidRenderer renderer)
        {
            if (!_fluidRenderers.Contains(renderer))
            {
                _fluidRenderers.Add(renderer);
            }
        }

        public void UnregisterFluidRenderer(FluidRenderer renderer)
        {
            _fluidRenderers.Remove(renderer);
        }

        public void SetGlobalFluidColor(Color color)
        {
            foreach (FluidRenderer renderer in _fluidRenderers)
            {
                if (renderer != null)
                {
                    renderer.SetParticleColor(color);
                }
            }
        }

        public void AddGlobalForce(Vector3 force)
        {
            foreach (FluidSystem system in _fluidSystems)
            {
                if (system != null)
                {
                    foreach (FluidParticle particle in system.Particles)
                    {
                        if (particle.IsActive)
                        {
                            particle.ApplyForce(force);
                        }
                    }
                }
            }
        }

        public float GetTotalFluidVolume()
        {
            float totalVolume = 0f;
            foreach (FluidSystem system in _fluidSystems)
            {
                if (system != null)
                {
                    totalVolume += system.GetFluidVolume();
                }
            }
            return totalVolume;
        }

        public void ClearAllFluids()
        {
            foreach (FluidSystem system in _fluidSystems)
            {
                if (system != null)
                {
                    system.ClearParticles();
                }
            }
        }
    }
}
