using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinGame.Editor
{
    using CoinGame.Network;
    using CoinGame.Utilities;

    public static class GridSpawnMapBaker
    {
        private const string AssetPath = "Assets/_CoinGame/Generated/GridSpawnMap.asset";
        private const float Tolerance = 0.01f;

        [MenuItem("CoinGame/Bake Grid Spawn Map")]
        public static void Bake()
        {
            Scene scene = SceneManager.GetActiveScene();
            Transform ground = FindGround(scene);
            List<Collider> staticColliders = FindStaticColliders(scene);
            List<Collider> playerZones = FindPlayerZoneColliders(scene);
            List<Transform> borders = FindBorders(scene);
            if (ground == null || staticColliders.Count == 0 || borders.Count < 2)
            {
                Debug.LogError("Grid bake: Ground, static colliders, or Border_6_7 blocks are missing.");
                return;
            }

            float xStep = GetStep(borders.Select(x => x.position.x));
            float zStep = GetStep(borders.Select(x => x.position.z));
            Vector3 origin = new(borders.Min(x => x.position.x), 0f, borders.Min(x => x.position.z));
            int width = GetCount(borders.Select(x => x.position.x), xStep);
            int height = GetCount(borders.Select(x => x.position.z), zStep);
            GridSpawnCell[] cells = BuildCells(origin, xStep, zStep, width, height, staticColliders, playerZones);
            GridSpawnMap map = LoadOrCreateAsset();
            map.SetData(scene.path, GetPath(ground), origin, new Vector2(xStep, zStep), width, height, cells);
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            Debug.Log($"Grid bake: {width}x{height}, step=({xStep:0.###}, {zStep:0.###}), " +
                $"blocked={cells.Count(x => HasFlag(x.Flags, GridCellFlags.StaticBlocked))}, asset={AssetPath}");
        }

        [MenuItem("CoinGame/Assign Grid Spawn Map References")]
        public static void AssignReferences()
        {
            GridSpawnMap map = AssetDatabase.LoadAssetAtPath<GridSpawnMap>(AssetPath);
            if (map == null)
            {
                Debug.LogError("Grid references: GridSpawnMap asset was not found. Bake it first.");
                return;
            }

            int assigned = 0;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (NetworkSpawner spawner in Resources.FindObjectsOfTypeAll<NetworkSpawner>()
                    .Where(x => x.gameObject.scene == scene))
                {
                    SerializedObject serialized = new(spawner);
                    SerializedProperty property = serialized.FindProperty("_gridSpawnMap");
                    if (property == null)
                    {
                        continue;
                    }

                    property.objectReferenceValue = map;
                    if (spawner.IsDefaultPlayerSpawner)
                    {
                        ClearZoneList(serialized, "_playerZones");
                        ClearZoneList(serialized, "_collectorPlayerZones");
                        ClearZoneList(serialized, "_disruptorPlayerZones");
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(spawner);
                    assigned++;
                }

                if (scene.isDirty)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                }
            }

            Debug.Log($"Grid references: assigned {assigned} spawner references.");
        }

        private static void ClearZoneList(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.arraySize = 0;
            }
        }

        private static GridSpawnCell[] BuildCells(Vector3 origin, float xStep, float zStep, int width, int height,
            List<Collider> staticColliders, List<Collider> playerZones)
        {
            Physics.SyncTransforms();
            List<GridSpawnCell> cells = new();
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 position = new(origin.x + x * xStep, origin.y + 50f, origin.z + z * zStep);
                    float groundY = GetGroundHeight(position);
                    Vector3 worldPosition = new(position.x, groundY, position.z);
                    GridCellFlags flags = groundY > float.MinValue ? GridCellFlags.Ground : GridCellFlags.None;
                    if (OverlapsStatic(worldPosition, xStep, zStep, staticColliders))
                    {
                        flags |= GridCellFlags.StaticBlocked;
                    }

                    foreach (Collider zone in playerZones)
                    {
                        if (!ContainsXZ(zone.bounds, worldPosition))
                        {
                            continue;
                        }

                        flags |= GetZoneFlags(zone.name);
                    }

                    cells.Add(new GridSpawnCell(new Vector2Int(x, z), worldPosition, flags));
                }
            }

            return cells.ToArray();
        }

        private static float GetGroundHeight(Vector3 position)
        {
            int mask = LayerMask.GetMask("Ground");
            if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 100f, mask, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return float.MinValue;
        }

        private static bool OverlapsStatic(Vector3 position, float xStep, float zStep, List<Collider> colliders)
        {
            float minX = position.x - xStep * 0.5f;
            float maxX = position.x + xStep * 0.5f;
            float minZ = position.z - zStep * 0.5f;
            float maxZ = position.z + zStep * 0.5f;
            foreach (Collider collider in colliders)
            {
                Bounds bounds = collider.bounds;
                if (bounds.max.x > minX + Tolerance && bounds.min.x < maxX - Tolerance &&
                    bounds.max.z > minZ + Tolerance && bounds.min.z < maxZ - Tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private static GridCellFlags GetZoneFlags(string name)
        {
            if (name.StartsWith("Collector", StringComparison.Ordinal))
            {
                return GridCellFlags.PlayerZone | GridCellFlags.CollectorZone;
            }
            if (name.StartsWith("Disruptor", StringComparison.Ordinal))
            {
                return GridCellFlags.PlayerZone | GridCellFlags.DisruptorZone;
            }
            if (name.StartsWith("Player", StringComparison.Ordinal))
            {
                return GridCellFlags.PlayerZone;
            }
            return GridCellFlags.None;
        }

        private static bool ContainsXZ(Bounds bounds, Vector3 position)
        {
            return position.x >= bounds.min.x && position.x <= bounds.max.x &&
                position.z >= bounds.min.z && position.z <= bounds.max.z;
        }

        private static GridSpawnMap LoadOrCreateAsset()
        {
            GridSpawnMap map = AssetDatabase.LoadAssetAtPath<GridSpawnMap>(AssetPath);
            if (map != null)
            {
                return map;
            }

            string directory = Path.GetDirectoryName(AssetPath);
            Directory.CreateDirectory(directory);
            map = ScriptableObject.CreateInstance<GridSpawnMap>();
            AssetDatabase.CreateAsset(map, AssetPath);
            return map;
        }

        private static List<Collider> FindStaticColliders(Scene scene)
        {
            int environment = LayerMask.NameToLayer("Environment");
            int grass = LayerMask.NameToLayer("Grass");
            int obstacle = LayerMask.NameToLayer("Obstacle");
            return Resources.FindObjectsOfTypeAll<Collider>()
                .Where(x => x.gameObject.scene == scene)
                .Where(x => x.gameObject.layer == environment || x.gameObject.layer == grass || x.gameObject.layer == obstacle)
                .ToList();
        }

        private static List<Collider> FindPlayerZoneColliders(Scene scene)
        {
            return Resources.FindObjectsOfTypeAll<BoxCollider>()
                .Where(x => x.gameObject.scene == scene &&
                    (x.name.StartsWith("Collector", StringComparison.Ordinal) ||
                     x.name.StartsWith("Disruptor", StringComparison.Ordinal) ||
                     x.name.StartsWith("Player", StringComparison.Ordinal) ||
                     x.name.EndsWith("_Points", StringComparison.Ordinal)))
                .Cast<Collider>()
                .ToList();
        }

        private static List<Transform> FindBorders(Scene scene)
        {
            return Resources.FindObjectsOfTypeAll<Transform>()
                .Where(x => x.gameObject.scene == scene && x.name.StartsWith("Border_6_7", StringComparison.Ordinal))
                .ToList();
        }

        private static Transform FindGround(Scene scene)
        {
            return Resources.FindObjectsOfTypeAll<Transform>()
                .FirstOrDefault(x => x.gameObject.scene == scene && x.name == "Ground");
        }

        private static float GetStep(IEnumerable<float> values)
        {
            List<float> sorted = values.OrderBy(x => x).ToList();
            List<float> unique = new();
            foreach (float value in sorted)
            {
                if (unique.Count == 0 || Mathf.Abs(value - unique[unique.Count - 1]) > Tolerance)
                {
                    unique.Add(value);
                }
            }

            List<float> differences = new();
            for (int index = 1; index < unique.Count; index++)
            {
                float difference = unique[index] - unique[index - 1];
                if (difference > Tolerance)
                {
                    differences.Add(difference);
                }
            }

            return differences.Count == 0 ? 0f : differences.Min();
        }

        private static int GetCount(IEnumerable<float> values, float step)
        {
            if (step <= Tolerance)
            {
                return 0;
            }

            List<float> sorted = values.OrderBy(x => x).ToList();
            return Mathf.RoundToInt((sorted[sorted.Count - 1] - sorted[0]) / step) + 1;
        }

        private static bool HasFlag(GridCellFlags value, GridCellFlags flag)
        {
            return (value & flag) == flag;
        }

        private static string GetPath(Transform target)
        {
            List<string> names = new();
            Transform current = target;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
    }
}
