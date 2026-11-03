using UnityEngine;

namespace AgentVFX.Editor
{
    public static class ParticleSystemBuilder
    {
        public static ParticleSystem Build(GameObject target, VfxSpec spec, Material material)
        {
            ParticleSystem particleSystem = target.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                target.AddComponent<ParticleSystem>();
                particleSystem = target.GetComponent<ParticleSystem>();
            }

            if (particleSystem == null)
            {
                throw new System.InvalidOperationException("ParticleSystem 컴포넌트를 생성할 수 없습니다.");
            }
            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = spec.particle.duration;
            main.loop = spec.prefab.loop;
            main.playOnAwake = spec.prefab.playOnAwake;
            main.startLifetime = spec.particle.startLifetime;
            main.startSpeed = spec.particle.startSpeed;
            main.startSize = spec.particle.startSize;
            main.maxParticles = spec.particle.maxParticles;
            main.simulationSpace = spec.particle.simulationSpace.Equals("World", System.StringComparison.OrdinalIgnoreCase) ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)spec.particle.burstCount) });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = spec.particle.shapeRadius;

            ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particleSystem;
        }
    }
}
