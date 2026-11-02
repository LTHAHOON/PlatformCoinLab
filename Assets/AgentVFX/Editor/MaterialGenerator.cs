using UnityEditor;
using UnityEngine;

namespace AgentVFX.Editor
{
    public static class MaterialGenerator
    {
        public static string Generate(VfxSpec spec, string texturePath, string outputDirectory, string hash)
        {
            string path = $"{outputDirectory}/{spec.id}_{hash}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                return path;
            }

            Shader shader = Shader.Find(spec.material.shader);
            if (shader == null || !shader.isSupported)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null || !shader.isSupported)
            {
                throw new System.InvalidOperationException("지원되는 URP 셰이더를 찾을 수 없습니다.");
            }

            Material material = new Material(shader) { name = $"{spec.id}_{hash}" };
            SetColor(material, "_BaseColor", spec.material.color);
            SetColor(material, "_Color", spec.material.color);
            SetColor(material, "_EmissionColor", spec.material.emission);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            return path;
        }

        private static void SetColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }
    }
}
