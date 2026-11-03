using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AgentVFX.Editor.Tests
{
    public sealed class AgentVfxPipelineTests
    {
        [Test]
        public void ElectricExplosionSpec_IsValid()
        {
            string json = File.ReadAllText("Assets/AgentVFX/Specs/ElectricExplosion.json");
            Assert.IsTrue(VfxSpecJson.TryParse(json, out VfxSpec spec, out string error), error);
            Assert.AreEqual("ElectricExplosion", spec.id);
            Assert.Greater(spec.particle.maxParticles, 0);
        }

        [Test]
        public void ElectricExplosionPrefab_HasRequiredComponents()
        {
            string[] guids = AssetDatabase.FindAssets("ElectricExplosion t:Prefab", new[] { "Assets/AgentVFX/Generated" });
            Assert.IsNotEmpty(guids, "생성된 ElectricExplosion Prefab을 찾을 수 없습니다.");
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab.GetComponent<ParticleSystem>());
            Assert.IsNotNull(prefab.GetComponent<ParticleSystemRenderer>());
            Assert.IsNotNull(prefab.GetComponent<ParticleSystemRenderer>().sharedMaterial);
        }
    }
}
