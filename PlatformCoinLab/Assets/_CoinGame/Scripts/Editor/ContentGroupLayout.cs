using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace CoinGame.Editor
{
    using CoinGame.Content;

    public static class ContentGroupLayout
    {
        [Serializable]
        private sealed class Layout
        {
            public Definition[] Groups;
        }

        [Serializable]
        private sealed class Definition
        {
            public string Name;
            public string PreviousName;
            public bool Remote;
            public bool PackSeparately;
            public string[] Assets;
        }

        [MenuItem("CoinGame/Content/Configure Content Groups")]
        public static void Configure()
        {
            BootstrapContentSettings config = AssetDatabase.LoadAssetAtPath<BootstrapContentSettings>("Assets/_CoinGame/ScriptableObject/BootstrapContentSettings.asset");
            Apply(AddressableAssetSettingsDefaultObject.Settings, config);
            AssetDatabase.SaveAssets();
        }

        public static void Apply(AddressableAssetSettings settings, BootstrapContentSettings config)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode before configuring content groups.");
            }
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText("AddressablesLayout.json"));
            HashSet<string> paths = new HashSet<string>();
            foreach (Definition definition in layout.Groups)
            {
                foreach (string path in definition.Assets)
                {
                    if (!paths.Add(path) || string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                    {
                        throw new InvalidOperationException($"Missing or repeated content asset: {path}");
                    }
                }
            }
            settings.AddLabel(config.RequiredLabel);
            foreach (Definition definition in layout.Groups)
            {
                AddressableAssetGroup group = settings.FindGroup(definition.Name);
                if (group == null && !string.IsNullOrEmpty(definition.PreviousName))
                {
                    group = settings.FindGroup(definition.PreviousName);
                    if (group != null)
                    {
                        group.Name = definition.Name;
                    }
                }
                if (group == null)
                {
                    group = settings.CreateGroup(definition.Name, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                }
                ConfigureSchema(settings, group.GetSchema<BundledAssetGroupSchema>(), definition, config);
                foreach (string path in definition.Assets)
                {
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                    entry.address = path;
                    entry.SetLabel(config.RequiredLabel, definition.Remote);
                }
                EditorUtility.SetDirty(group);
            }
            EditorUtility.SetDirty(settings);
        }

        private static void ConfigureSchema(AddressableAssetSettings settings, BundledAssetGroupSchema schema, Definition definition, BootstrapContentSettings config)
        {
            schema.UseDefaultSchemaSettings = false;
            schema.StripDownloadOptions = false;
            schema.UseAssetBundleCache = true;
            if (definition.Remote)
            {
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                schema.Timeout = config.RequestTimeoutSeconds;
                schema.RetryCount = config.DownloadRetryCount;
            }
            else
            {
                schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            }
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            if (definition.PackSeparately)
            {
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            }
            EditorUtility.SetDirty(schema);
        }
    }
}
