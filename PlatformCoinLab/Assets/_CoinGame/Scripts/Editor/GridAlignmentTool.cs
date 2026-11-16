using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinGame.Editor
{
    public static class GridAlignmentTool
    {
        private const string ReportPath = "Assets/_CoinGame/Generated/GridAlignmentReport.json";
        private const string BorderPrefix = "Border_6_7";
        private const string DecorativePrefix = "decorative_block_yellow";
        private const float PositionTolerance = 0.001f;

        [MenuItem("CoinGame/Align Voxel Props To Border Grid")]
        public static void Align()
        {
            Scene scene = SceneManager.GetActiveScene();
            List<Transform> transforms = Resources.FindObjectsOfTypeAll<Transform>()
                .Where(x => x.gameObject.scene == scene).ToList();
            List<Transform> borders = transforms.Where(x => x.name.StartsWith(BorderPrefix, StringComparison.Ordinal)).ToList();
            if (borders.Count < 2)
            {
                Debug.LogError("Grid alignment: Border_6_7 blocks were not found.");
                return;
            }

            float xStep = GetStep(borders.Select(x => x.position.x));
            float zStep = GetStep(borders.Select(x => x.position.z));
            float xOrigin = borders.Min(x => x.position.x);
            float zOrigin = borders.Min(x => x.position.z);
            int grassLayer = LayerMask.NameToLayer("Grass");
            List<Transform> targets = transforms.Where(x =>
                x.name.StartsWith(DecorativePrefix, StringComparison.Ordinal) ||
                x.gameObject.layer == grassLayer).ToList();
            List<GridAlignmentEntry> entries = new();
            foreach (Transform target in targets)
            {
                Vector3 before = target.position;
                Vector3 after = new(
                    Snap(before.x, xOrigin, xStep),
                    before.y,
                    Snap(before.z, zOrigin, zStep));
                float delta = Vector3.Distance(before, after);
                entries.Add(new GridAlignmentEntry
                {
                    Name = target.name,
                    Layer = LayerMask.LayerToName(target.gameObject.layer),
                    Before = before,
                    After = after,
                    Delta = delta,
                });
                if (delta <= PositionTolerance)
                {
                    continue;
                }

                Undo.RecordObject(target, "Align voxel props to Border grid");
                target.position = after;
                EditorUtility.SetDirty(target);
            }

            GridAlignmentReport report = new()
            {
                Scene = scene.path,
                BorderCount = borders.Count,
                XStep = xStep,
                ZStep = zStep,
                XOrigin = xOrigin,
                ZOrigin = zOrigin,
                Entries = entries.ToArray(),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
            LogReport(report);
        }

        private static float GetStep(IEnumerable<float> values)
        {
            List<float> unique = values.OrderBy(x => x).Aggregate(new List<float>(), (list, value) =>
            {
                if (list.Count == 0 || Mathf.Abs(value - list[list.Count - 1]) > PositionTolerance)
                {
                    list.Add(value);
                }

                return list;
            });
            List<float> differences = new();
            for (int index = 1; index < unique.Count; index++)
            {
                float difference = unique[index] - unique[index - 1];
                if (difference > PositionTolerance)
                {
                    differences.Add(difference);
                }
            }

            return differences.Count == 0 ? 0f : differences.Min();
        }

        private static float Snap(float value, float origin, float step)
        {
            if (step <= PositionTolerance)
            {
                return value;
            }

            return origin + Mathf.Round((value - origin) / step) * step;
        }

        private static void LogReport(GridAlignmentReport report)
        {
            int moved = report.Entries.Count(x => x.Delta > PositionTolerance);
            float maxDelta = report.Entries.Length == 0 ? 0f : report.Entries.Max(x => x.Delta);
            Debug.Log($"Grid alignment: borders={report.BorderCount}, targets={report.Entries.Length}, moved={moved}, " +
                $"step=({report.XStep:0.###}, {report.ZStep:0.###}), maxDelta={maxDelta:0.###}, report={ReportPath}");
        }

        [Serializable]
        private sealed class GridAlignmentReport
        {
            public string Scene;
            public int BorderCount;
            public float XStep;
            public float ZStep;
            public float XOrigin;
            public float ZOrigin;
            public GridAlignmentEntry[] Entries;
        }

        [Serializable]
        private sealed class GridAlignmentEntry
        {
            public string Name;
            public string Layer;
            public Vector3 Before;
            public Vector3 After;
            public float Delta;
        }
    }
}
