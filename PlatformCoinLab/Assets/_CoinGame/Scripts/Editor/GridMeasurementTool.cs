using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinGame.Editor
{
    public static class GridMeasurementTool
    {
        private const string ReportPath = "Assets/_CoinGame/Generated/GridMeasurementReport.json";
        private const float IntervalTolerance = 0.02f;

        [MenuItem("CoinGame/Measure Grid Layout")]
        public static void Measure()
        {
            Scene scene = SceneManager.GetActiveScene();
            Physics.SyncTransforms();
            Transform ground = FindGround(scene);
            if (ground == null)
            {
                Debug.LogError("Grid measurement: Ground was not found in the active scene.");
                return;
            }

            List<Collider> staticColliders = FindStaticColliders(scene, ground);
            List<Transform> zones = FindSpawnZones(scene);
            List<Vector3> centers = staticColliders.Select(x => ground.InverseTransformPoint(x.bounds.center)).ToList();
            GridMeasurementReport report = BuildReport(scene, ground, staticColliders, zones, centers);
            string directory = Path.GetDirectoryName(ReportPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            AssetDatabase.Refresh();
            LogReport(report);
        }

        private static GridMeasurementReport BuildReport(
            Scene scene,
            Transform ground,
            List<Collider> colliders,
            List<Transform> zones,
            List<Vector3> centers)
        {
            Bounds bounds = GetBounds(ground, scene);
            List<float> xIntervals = GetIntervals(centers.Select(x => x.x));
            List<float> zIntervals = GetIntervals(centers.Select(x => x.z));
            GridObjectReport[] objects = colliders.Select(x => CreateObjectReport(ground, x)).ToArray();
            float xStep = GetFootprintStep(objects.Select(x => x.LocalFootprint.x));
            float zStep = GetFootprintStep(objects.Select(x => x.LocalFootprint.y));
            List<Vector3> validCenters = objects.Select(x => x.LocalCenter).ToList();
            int xCells = GetCellCount(validCenters.Select(x => x.x), xStep);
            int zCells = GetCellCount(validCenters.Select(x => x.z), zStep);
            return new GridMeasurementReport
            {
                Scene = scene.path,
                GroundPath = GetHierarchyPath(ground),
                GroundLocalBounds = new SerializableBounds(bounds),
                StaticColliderCount = colliders.Count,
                SpawnZoneCount = zones.Count,
                XCandidateStep = xStep,
                ZCandidateStep = zStep,
                XInferredCellCount = xCells,
                ZInferredCellCount = zCells,
                XResidues = GetResidues(validCenters.Select(x => x.x), xStep),
                ZResidues = GetResidues(validCenters.Select(x => x.z), zStep),
                XIntervals = xIntervals.Take(8).ToArray(),
                ZIntervals = zIntervals.Take(8).ToArray(),
                Objects = objects,
            };
        }

        private static GridObjectReport CreateObjectReport(Transform ground, Collider collider)
        {
            Bounds bounds = collider.bounds;
            Vector3 min = ground.InverseTransformPoint(bounds.min);
            Vector3 max = ground.InverseTransformPoint(bounds.max);
            return new GridObjectReport
            {
                Name = collider.name,
                Layer = LayerMask.LayerToName(collider.gameObject.layer),
                LocalCenter = ground.InverseTransformPoint(bounds.center),
                LocalFootprint = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.z - min.z)),
            };
        }

        private static List<Collider> FindStaticColliders(Scene scene, Transform ground)
        {
            int environment = LayerMask.NameToLayer("Environment");
            int grass = LayerMask.NameToLayer("Grass");
            int obstacle = LayerMask.NameToLayer("Obstacle");
            return Resources.FindObjectsOfTypeAll<Collider>()
                .Where(x => x.gameObject.scene == scene && x.transform != ground)
                .Where(x => x.gameObject.layer == environment || x.gameObject.layer == grass || x.gameObject.layer == obstacle)
                .ToList();
        }

        private static List<Transform> FindSpawnZones(Scene scene)
        {
            return Resources.FindObjectsOfTypeAll<Transform>()
                .Where(x => x.gameObject.scene == scene &&
                    (x.name.Contains("Points", StringComparison.Ordinal) || x.name.Contains("Zone", StringComparison.Ordinal)))
                .ToList();
        }

        private static Transform FindGround(Scene scene)
        {
            return Resources.FindObjectsOfTypeAll<Transform>()
                .FirstOrDefault(x => x.gameObject.scene == scene && x.name == "Ground");
        }

        private static Bounds GetBounds(Transform ground, Scene scene)
        {
            Renderer[] renderers = ground.GetComponentsInChildren<Renderer>(true)
                .Where(x => x.gameObject.scene == scene).ToArray();
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            Vector3 min = ground.InverseTransformPoint(bounds.min);
            Vector3 max = ground.InverseTransformPoint(bounds.max);
            Vector3 normalizedMin = Vector3.Min(min, max);
            Vector3 normalizedMax = Vector3.Max(min, max);
            return new Bounds((normalizedMin + normalizedMax) * 0.5f, normalizedMax - normalizedMin);
        }

        private static List<float> GetIntervals(IEnumerable<float> values)
        {
            List<float> sorted = values.OrderBy(x => x).ToList();
            List<float> intervals = new();
            for (int i = 0; i < sorted.Count; i++)
            {
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    float interval = sorted[j] - sorted[i];
                    if (interval > IntervalTolerance)
                    {
                        intervals.Add(Mathf.Round(interval * 100f) / 100f);
                    }
                }
            }

            return intervals.GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key)
                .Select(x => x.Key)
                .ToList();
        }

        private static float GetFootprintStep(IEnumerable<float> footprints)
        {
            List<float> values = footprints.OrderBy(x => x).ToList();
            if (values.Count == 0)
            {
                return 0f;
            }

            int middle = values.Count / 2;
            return values.Count % 2 == 0
                ? (values[middle - 1] + values[middle]) * 0.5f
                : values[middle];
        }

        private static int GetCellCount(IEnumerable<float> values, float step)
        {
            if (step <= IntervalTolerance)
            {
                return 0;
            }

            List<float> sorted = values.OrderBy(x => x).ToList();
            return sorted.Count == 0 ? 0 : Mathf.RoundToInt((sorted[sorted.Count - 1] - sorted[0]) / step) + 1;
        }

        private static ResidueCluster[] GetResidues(IEnumerable<float> values, float step)
        {
            if (step <= IntervalTolerance)
            {
                return Array.Empty<ResidueCluster>();
            }

            return values.Select(value => Mathf.Repeat(value, step) / step)
                .GroupBy(value => Mathf.Round(value * 1000f) / 1000f)
                .OrderByDescending(group => group.Count())
                .Take(8)
                .Select(group => new ResidueCluster { Value = group.Key, Count = group.Count() })
                .ToArray();
        }

        private static string GetHierarchyPath(Transform target)
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

        private static void LogReport(GridMeasurementReport report)
        {
            Debug.Log($"Grid measurement: ground={report.GroundPath}, colliders={report.StaticColliderCount}, zones={report.SpawnZoneCount}, " +
                $"candidateStep=({report.XCandidateStep:0.###}, {report.ZCandidateStep:0.###}), report={ReportPath}");
        }

        [Serializable]
        private sealed class GridMeasurementReport
        {
            public string Scene;
            public string GroundPath;
            public SerializableBounds GroundLocalBounds;
            public int StaticColliderCount;
            public int SpawnZoneCount;
            public float XCandidateStep;
            public float ZCandidateStep;
            public int XInferredCellCount;
            public int ZInferredCellCount;
            public ResidueCluster[] XResidues;
            public ResidueCluster[] ZResidues;
            public float[] XIntervals;
            public float[] ZIntervals;
            public GridObjectReport[] Objects;
        }

        [Serializable]
        private sealed class GridObjectReport
        {
            public string Name;
            public string Layer;
            public Vector3 LocalCenter;
            public Vector2 LocalFootprint;
        }

        [Serializable]
        private sealed class ResidueCluster
        {
            public float Value;
            public int Count;
        }

        [Serializable]
        private struct SerializableBounds
        {
            public Vector3 Center;
            public Vector3 Size;

            public SerializableBounds(Bounds bounds)
            {
                Center = bounds.center;
                Size = bounds.size;
            }
        }
    }
}
