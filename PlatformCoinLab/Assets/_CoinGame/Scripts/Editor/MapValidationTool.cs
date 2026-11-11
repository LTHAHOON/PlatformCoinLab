using UnityEditor;
using UnityEngine;
using System.Linq;

namespace CoinGame.Editor
{
    public static class MapValidationTool
    {
        [MenuItem("CoinGame/Validate Active Map")]
        public static void Validate()
        {
            BoxCollider bounds = Object.FindObjectsByType<BoxCollider>()
                .FirstOrDefault(x => x.name == "Bounds");
            Collider[] coins = Object.FindObjectsByType<BoxCollider>()
                .Where(x => x.name.StartsWith("CoinZone")).ToArray();
            Collider[] crates = Object.FindObjectsByType<BoxCollider>()
                .Where(x => x.name.StartsWith("CrateZone")).ToArray();
            if (bounds == null)
            {
                Debug.LogError("Map validation: Bounds not found.");
                return;
            }
            Vector3 size = bounds.bounds.size;
            Debug.Log($"Map validation: bounds={size}, coinZones={coins.Length}, crateZones={crates.Length}");
            if (size.x < 8f || size.z < 8f) Debug.LogWarning("Map bounds are too narrow for the default player.");
            foreach (Collider zone in coins.Concat(crates))
            {
                if (zone.bounds.size.x < 1.3f || zone.bounds.size.z < 1.3f)
                {
                    Debug.LogWarning($"Narrow spawn zone: {zone.name}");
                }
            }
        }
    }
}
