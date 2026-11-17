using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace CoinGame.Editor
{
    [InitializeOnLoad]
    public static class AndroidContentBuild
    {
        private const string PendingKey = "CoinGame.BuildAndroidContent";

        static AndroidContentBuild()
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                EditorApplication.update += Resume;
            }
        }

        [MenuItem("CoinGame/Content/Build Android to ContentServer")]
        public static void Build()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Stop Play Mode before building Android content.");
                return;
            }
            SessionState.SetBool(PendingKey, true);
            EditorApplication.update -= Resume;
            EditorApplication.update += Resume;
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            string profile = settings.profileSettings.GetProfileId("CoinGame_Android");
            if (!string.IsNullOrEmpty(profile))
            {
                settings.activeProfileId = profile;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    SessionState.SetBool(PendingKey, false);
                    EditorApplication.update -= Resume;
                    Debug.LogError("Android target could not be selected. Check Android Build Support.");
                }
                return;
            }
            Resume();
        }

        private static void Resume()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                return;
            }
            SessionState.SetBool(PendingKey, false);
            EditorApplication.update -= Resume;
            BootstrapProjectSetup.Build();
        }
    }
}
