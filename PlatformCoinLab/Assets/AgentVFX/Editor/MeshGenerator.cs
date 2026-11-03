using UnityEditor;
using UnityEngine;

namespace AgentVFX.Editor
{
    public static class MeshGenerator
    {
        public static string Generate(VfxSpec spec, string outputDirectory, string hash)
        {
            string path = $"{outputDirectory}/{spec.id}_{hash}_{spec.mesh.type}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                return path;
            }

            Mesh mesh = spec.mesh.type.ToLowerInvariant() switch
            {
                "ring" => BuildRing(spec.mesh),
                "cone" => BuildCone(spec.mesh),
                _ => BuildQuad(spec.mesh)
            };
            mesh.name = $"{spec.id}_{hash}_{spec.mesh.type}";
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            return path;
        }

        private static Mesh BuildQuad(MeshSpec spec)
        {
            Mesh mesh = new Mesh
            {
                vertices = new[] { new Vector3(-spec.radius, 0f, -spec.radius), new Vector3(spec.radius, 0f, -spec.radius), new Vector3(spec.radius, 0f, spec.radius), new Vector3(-spec.radius, 0f, spec.radius) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildRing(MeshSpec spec)
        {
            int count = spec.segments;
            Vector3[] vertices = new Vector3[count * 2];
            Vector2[] uv = new Vector2[count * 2];
            int[] triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i] = direction * spec.radius;
                vertices[count + i] = direction * spec.innerRadius;
                uv[i] = new Vector2(i / (float)count, 1f);
                uv[count + i] = new Vector2(i / (float)count, 0f);
                int offset = i * 6;
                int next = (i + 1) % count;
                triangles[offset] = i;
                triangles[offset + 1] = next;
                triangles[offset + 2] = count + next;
                triangles[offset + 3] = i;
                triangles[offset + 4] = count + next;
                triangles[offset + 5] = count + i;
            }

            Mesh mesh = new Mesh { vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildCone(MeshSpec spec)
        {
            int count = spec.segments;
            Vector3[] vertices = new Vector3[count + 1];
            Vector2[] uv = new Vector2[count + 1];
            int[] triangles = new int[count * 3];
            vertices[0] = Vector3.up * spec.height;
            uv[0] = new Vector2(0.5f, 1f);
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * spec.radius, 0f, Mathf.Sin(angle) * spec.radius);
                uv[i + 1] = new Vector2(i / (float)count, 0f);
                int offset = i * 3;
                triangles[offset] = 0;
                triangles[offset + 1] = i + 1;
                triangles[offset + 2] = (i + 1) % count + 1;
            }

            Mesh mesh = new Mesh { vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
