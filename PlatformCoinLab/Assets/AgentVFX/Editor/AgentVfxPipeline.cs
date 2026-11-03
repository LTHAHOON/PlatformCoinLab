using System.IO;
using UnityEditor;
using UnityEngine;

namespace AgentVFX.Editor
{
    public static class AgentVfxPipeline
    {
        private const string Root = "Assets/AgentVFX/Generated";

        [MenuItem("Tools/Agent VFX/Generate Electric Explosion")]
        public static void GenerateElectricExplosion()
        {
            string path = "Assets/AgentVFX/Specs/ElectricExplosion.json";
            if (!File.Exists(path))
            {
                Debug.LogError($"[AgentVFX] Spec not found: {path}");
                return;
            }

            VfxGenerationResult result = GenerateFromJson(File.ReadAllText(path));
            Debug.Log($"[AgentVFX] Generated {result.prefabPath}\n{result.validationReport}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(result.prefabPath);
        }

        public static VfxGenerationResult GenerateFromJson(string json)
        {
            if (!VfxSpecJson.TryParse(json, out VfxSpec spec, out string error))
            {
                Debug.LogError($"[AgentVFX] Invalid spec: {error}");
                throw new System.ArgumentException(error, nameof(json));
            }

            string hash = VfxSpecJson.ComputeHash(json);
            string outputDirectory = $"{Root}/{spec.id}-{hash}";
            EnsureDirectory(outputDirectory);
            string texturePath = TextureGenerator.Generate(spec, outputDirectory, hash);
            string meshPath = MeshGenerator.Generate(spec, outputDirectory, hash);
            string materialPath = MaterialGenerator.Generate(spec, texturePath, outputDirectory, hash);
            string prefabPath = VFXPrefabBuilder.Generate(spec, meshPath, materialPath, outputDirectory, hash);
            AssetDatabase.Refresh();

            VfxGenerationResult result = new VfxGenerationResult
            {
                rootPath = outputDirectory,
                texturePath = texturePath,
                meshPath = meshPath,
                materialPath = materialPath,
                prefabPath = prefabPath,
                reusedExisting = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null
            };
            result.validationReport = VFXValidator.Validate(spec, result);
            return result;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
