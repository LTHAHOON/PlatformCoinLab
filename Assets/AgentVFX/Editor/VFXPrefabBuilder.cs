using UnityEditor;
using UnityEngine;

namespace AgentVFX.Editor
{
    public static class VFXPrefabBuilder
    {
        public static string Generate(VfxSpec spec, string meshPath, string materialPath, string outputDirectory, string hash)
        {
            string prefabPath = $"{outputDirectory}/{spec.id}_{hash}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return prefabPath;
            }

            GameObject root = new GameObject(spec.id);
            try
            {
                root.transform.localScale = spec.prefab.localScale;
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                ParticleSystemBuilder.Build(root, spec, material);

                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh != null)
                {
                    GameObject meshObject = new GameObject("GeneratedMesh");
                    meshObject.transform.SetParent(root.transform, false);
                    meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    meshObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                return prefabPath;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
