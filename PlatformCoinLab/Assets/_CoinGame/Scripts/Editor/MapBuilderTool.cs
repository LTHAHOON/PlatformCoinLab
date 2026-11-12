using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinGame.Editor
{
    public sealed class MapBuilderSettings
    {
        public int tilesX = 2;
        public int tilesZ = 2;
        public float tileSize = 45f;
        public float rootScale = 0.5f;
        public int seed = 1001;
        public int propCount = 34;
    }

    public static class MapBuilderTool
    {
        private const string SettingsPath = "Assets/_CoinGame/Editor/MapBuilderSettings.asset";
        private const string LandPath = "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Lands/land camping forest.prefab";
        private static readonly string[] PropPaths =
        {
            "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Plants/bush plant.prefab",
            "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Stones/stone medium.prefab",
            "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/Sports environment/mesh fence.prefab",
            "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/Benchs/bench.prefab",
            "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/Camping environment/camping tent blue.prefab"
        };

        [MenuItem("CoinGame/Map Builder/Create Lands Maps")]
        public static void CreateMaps()
        {
            MapBuilderSettings settings = LoadSettings();
            for (int index = 0; index < 3; index++)
            {
                CreateMap($"Map_Lands_{(char)('A' + index)}", settings, settings.seed + index * 7919);
            }
            AssetDatabase.SaveAssets();
            ReplaceActiveSceneMap();
            Debug.Log("Map Builder: created Map_Lands_A/B/C with Lands 2x2 and seeded decor.");
        }

        private static MapBuilderSettings LoadSettings()
        {
            MapBuilderSettings settings = new();
            settings.tilesX = EditorPrefs.GetInt("CoinGame.Map.TilesX", settings.tilesX);
            settings.tilesZ = EditorPrefs.GetInt("CoinGame.Map.TilesZ", settings.tilesZ);
            settings.tileSize = EditorPrefs.GetFloat("CoinGame.Map.TileSize", settings.tileSize);
            settings.rootScale = EditorPrefs.GetFloat("CoinGame.Map.RootScale", settings.rootScale);
            settings.seed = EditorPrefs.GetInt("CoinGame.Map.Seed", settings.seed);
            settings.propCount = EditorPrefs.GetInt("CoinGame.Map.PropCount", settings.propCount);
            return settings;
        }

        private static void CreateMap(string name, MapBuilderSettings settings, int seed)
        {
            string path = $"Assets/_CoinGame/Prefabs/Maps/{name}.prefab";
            GameObject old = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            GameObject root = new(name);
            root.transform.localScale = Vector3.one * settings.rootScale;
            Transform ground = CreateChild(root.transform, "Ground");
            Transform decor = CreateChild(root.transform, "Decor");
            CreateAnchors(root.transform);
            CreateZones(root.transform);
            CreateBounds(root.transform, settings.tileSize * settings.tilesX * 0.5f);
            BuildGround(ground, settings);
            BuildDecor(decor, settings, seed);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void BuildGround(Transform parent, MapBuilderSettings settings)
        {
            GameObject land = AssetDatabase.LoadAssetAtPath<GameObject>(LandPath);
            if (land == null) return;
            for (int x = 0; x < settings.tilesX; x++)
            for (int z = 0; z < settings.tilesZ; z++)
            {
                GameObject tile = PrefabUtility.InstantiatePrefab(land) as GameObject;
                tile.name = $"Land_{x}_{z}";
                tile.transform.SetParent(parent, false);
                tile.transform.localPosition = new Vector3(
                    (x - (settings.tilesX - 1) * 0.5f) * settings.tileSize,
                    0f,
                    (z - (settings.tilesZ - 1) * 0.5f) * settings.tileSize);
                tile.transform.localRotation = Quaternion.Euler(0f, (x + z) % 2 * 180f, 0f);
                PrefabUtility.UnpackPrefabInstance(tile, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                SetLayerRecursively(tile, LayerMask.NameToLayer("Ground"));
                SetStaticRecursively(tile);
            }
        }

        private static void BuildDecor(Transform parent, MapBuilderSettings settings, int seed)
        {
            System.Random random = new(seed);
            for (int index = 0; index < settings.propCount; index++)
            {
                float x = Mathf.Lerp(-19f, 19f, (float)random.NextDouble());
                float z = Mathf.Lerp(-19f, 19f, (float)random.NextDouble());
                if (Mathf.Abs(x) < 7f || Mathf.Abs(z) > 16f) continue;
                string path = PropPaths[random.Next(PropPaths.Length)];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                GameObject prop = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                prop.name = $"Decor_{index:00}";
                prop.transform.SetParent(parent, false);
                prop.transform.localPosition = new Vector3(x, 0f, z);
                prop.transform.localRotation = Quaternion.Euler(0f, random.Next(0, 4) * 90f, 0f);
                PrefabUtility.UnpackPrefabInstance(prop, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                SetLayerRecursively(prop, LayerMask.NameToLayer("Decor"));
                SetStaticRecursively(prop);
            }
        }

        private static void CreateAnchors(Transform parent)
        {
            CreateChild(parent, "StartAnchor").localPosition = new Vector3(0f, 0f, -21f);
            CreateChild(parent, "ExitAnchor").localPosition = new Vector3(0f, 0f, 21f);
            Transform spawns = CreateChild(parent, "PlayerSpawnPoints");
            CreateChild(spawns, "CollectorSpawn_1").localPosition = new Vector3(-3f, 0f, -17f);
            CreateChild(spawns, "DisruptorSpawn_1").localPosition = new Vector3(3f, 0f, -17f);
        }

        private static void CreateZones(Transform parent)
        {
            Transform coins = CreateChild(parent, "CoinZones");
            CreateZone(coins, "CoinZone_Center", Vector3.zero, new Vector3(10f, 1f, 10f));
            CreateZone(coins, "CoinZone_East", new Vector3(13f, 0f, 7f), new Vector3(8f, 1f, 8f));
            CreateZone(coins, "CoinZone_West", new Vector3(-13f, 0f, 7f), new Vector3(8f, 1f, 8f));
            Transform crates = CreateChild(parent, "CrateZones");
            CreateZone(crates, "CrateZone_Center", new Vector3(0f, 0f, 7f), new Vector3(5f, 1f, 5f));
        }

        private static void CreateZone(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject zone = new(name);
            zone.transform.SetParent(parent, false);
            zone.transform.localPosition = position;
            BoxCollider collider = zone.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
        }

        private static void CreateBounds(Transform parent, float halfSize)
        {
            Transform bounds = CreateChild(parent, "Bounds");
            BoxCollider movement = bounds.gameObject.AddComponent<BoxCollider>();
            movement.isTrigger = true;
            movement.size = new Vector3(halfSize * 2f - 4f, 1f, halfSize * 2f - 4f);
            CreateWall(bounds, "Wall_North", new Vector3(0f, 1f, halfSize), new Vector3(halfSize * 2f, 2f, 1f));
            CreateWall(bounds, "Wall_South", new Vector3(0f, 1f, -halfSize), new Vector3(halfSize * 2f, 2f, 1f));
            CreateWall(bounds, "Wall_East", new Vector3(halfSize, 1f, 0f), new Vector3(1f, 2f, halfSize * 2f));
            CreateWall(bounds, "Wall_West", new Vector3(-halfSize, 1f, 0f), new Vector3(1f, 2f, halfSize * 2f));
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;
            wall.layer = LayerMask.NameToLayer("Obstacle");
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void SetStaticRecursively(GameObject target)
        {
            target.isStatic = true;
            foreach (Transform child in target.transform) SetStaticRecursively(child.gameObject);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (layer >= 0) target.layer = layer;
            foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private static void ReplaceActiveSceneMap()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject old = GameObject.Find("Map_Palmov_A");
            if (old != null) old.SetActive(false);
            foreach (GameObject existing in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (existing != null && existing.name == "Map_Lands_A")
                {
                    Object.DestroyImmediate(existing);
                }
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_CoinGame/Prefabs/Maps/Map_Lands_A.prefab");
            if (prefab == null || !scene.isLoaded) return;
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            instance.name = "Map_Lands_A";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
