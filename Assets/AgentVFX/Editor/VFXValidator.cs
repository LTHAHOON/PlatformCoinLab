using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AgentVFX.Editor
{
    public static class VFXValidator
    {
        public static string Validate(VfxSpec spec, VfxGenerationResult result)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            CheckAsset<Texture2D>(result.texturePath, "텍스처", errors);
            CheckAsset<Mesh>(result.meshPath, "메시", errors);
            Material material = CheckAsset<Material>(result.materialPath, "머티리얼", errors);
            GameObject prefab = CheckAsset<GameObject>(result.prefabPath, "Prefab", errors);
            if (material != null && (material.shader == null || !material.shader.isSupported))
            {
                errors.Add("머티리얼 셰이더가 없거나 현재 플랫폼에서 지원되지 않습니다.");
            }

            if (prefab != null)
            {
                if (prefab.GetComponent<ParticleSystem>() == null)
                {
                    errors.Add("Prefab에 ParticleSystem이 없습니다.");
                }

                ParticleSystemRenderer renderer = prefab.GetComponent<ParticleSystemRenderer>();
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    errors.Add("Prefab ParticleSystemRenderer의 머티리얼 참조가 없습니다.");
                }
            }

            if (spec.particle.maxParticles > 256)
            {
                warnings.Add("maxParticles가 256보다 커 오버드로우와 CPU 비용을 점검해야 합니다.");
            }

            string report = $"AgentVFX Validation: {spec.id}\nErrors: {errors.Count}\nWarnings: {warnings.Count}";
            foreach (string error in errors)
            {
                report += $"\nERROR: {error}";
                Debug.LogError($"[AgentVFX] {spec.id}: {error}");
            }

            foreach (string warning in warnings)
            {
                report += $"\nWARNING: {warning}";
                Debug.LogWarning($"[AgentVFX] {spec.id}: {warning}");
            }

            if (errors.Count == 0)
            {
                Debug.Log($"[AgentVFX] {spec.id} validation passed with {warnings.Count} warning(s).");
            }

            return report;
        }

        private static T CheckAsset<T>(string path, string label, List<string> errors) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                errors.Add($"{label} asset missing: {path}");
            }

            return asset;
        }
    }
}
