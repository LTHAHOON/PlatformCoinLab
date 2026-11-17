using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoinGame.Editor
{
    using CoinGame.Content;

    public static class BootstrapProjectSetup
    {
        public const string ScenePath = "Assets/_CoinGame/Scenes/Bootstrap_CoinGameScene.unity";
        public const string InGamePath = "Assets/_CoinGame/Scenes/InGame_CoinGameScene.unity";
        private const string SettingsPath = "Assets/_CoinGame/ScriptableObject/BootstrapContentSettings.asset";

        [MenuItem("CoinGame/Content/Configure Groups and Bootstrap")]
        public static void Configure()
        {
            if (Application.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            BootstrapContentSettings config = AssetDatabase.LoadAssetAtPath<BootstrapContentSettings>(SettingsPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BootstrapContentSettings>();
                AssetDatabase.CreateAsset(config, SettingsPath);
            }
            string profile = settings.profileSettings.GetProfileId("CoinGame_Profile");
            settings.activeProfileId = profile;
            settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteBuildPath, "../ContentServer/Content/[BuildTarget]");
            settings.profileSettings.SetValue(profile, AddressableAssetSettings.kRemoteLoadPath, "http://localhost:5250/[BuildTarget]");
            string androidProfile = settings.profileSettings.GetProfileId("CoinGame_Android");
            if (string.IsNullOrEmpty(androidProfile))
            {
                androidProfile = settings.profileSettings.AddProfile("CoinGame_Android", profile);
                settings.profileSettings.SetValue(androidProfile, AddressableAssetSettings.kRemoteLoadPath, "http://10.0.2.2:5250/[BuildTarget]");
            }
            settings.BuildRemoteCatalog = true;
            settings.EnableJsonCatalog = true;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            ContentGroupLayout.Apply(settings, config);
            EditorUtility.SetDirty(settings);
            if (!File.Exists(ScenePath))
            {
                BootstrapSceneBuilder.Create(config);
            }
            System.Collections.Generic.List<EditorBuildSettingsScene> scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path != ScenePath && scene.path != InGamePath)
                {
                    scenes.Add(scene);
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Bootstrap configured. InGame is remote; bootstrap, lobby, network prefabs and settings remain local.");
        }

        [MenuItem("CoinGame/Content/Build Active Target to ContentServer")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode before building content.");
            }
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new InvalidOperationException(result.Error);
            }
            Debug.Log($"Content built to {Path.GetFullPath("../ContentServer/Content/" + EditorUserBuildSettings.activeBuildTarget)}. Build the matching Player with the same profile.");
        }
    }
}
