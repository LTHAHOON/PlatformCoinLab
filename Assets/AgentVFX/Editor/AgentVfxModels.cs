using System;
using UnityEngine;

namespace AgentVFX
{
    [Serializable]
    public sealed class VfxSpec
    {
        public string id;
        public int seed = 1;
        public TextureSpec texture = new TextureSpec();
        public MeshSpec mesh = new MeshSpec();
        public MaterialSpec material = new MaterialSpec();
        public ParticleSpec particle = new ParticleSpec();
        public PrefabSpec prefab = new PrefabSpec();
    }

    [Serializable]
    public sealed class TextureSpec
    {
        public int width = 128;
        public int height = 128;
        public float noiseScale = 4f;
        public Color gradientStart = Color.white;
        public Color gradientEnd = Color.black;
        public float radialPower = 2f;
    }

    [Serializable]
    public sealed class MeshSpec
    {
        public string type = "Quad";
        public float radius = 0.5f;
        public float innerRadius = 0.25f;
        public float height = 1f;
        public int segments = 24;
    }

    [Serializable]
    public sealed class MaterialSpec
    {
        public string shader = "Universal Render Pipeline/Unlit";
        public Color color = Color.white;
        public Color emission = Color.black;
    }

    [Serializable]
    public sealed class ParticleSpec
    {
        public float duration = 1f;
        public float startLifetime = 0.5f;
        public float startSpeed = 1f;
        public float startSize = 1f;
        public int maxParticles = 100;
        public int burstCount = 10;
        public float shapeRadius = 0.1f;
        public string simulationSpace = "Local";
    }

    [Serializable]
    public sealed class PrefabSpec
    {
        public bool loop;
        public bool playOnAwake = true;
        public Vector3 localScale = Vector3.one;
    }

    public sealed class VfxGenerationResult
    {
        public string rootPath;
        public string texturePath;
        public string meshPath;
        public string materialPath;
        public string prefabPath;
        public string validationReport;
        public bool reusedExisting;
    }
}
