using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine.AddressableAssets;

namespace CoinGame.Editor
{
    public sealed class WebAddressablesBuild : BuildPlayerProcessor
    {
        public override void PrepareForBuild(BuildPlayerContext context)
        {
            if (context.BuildPlayerOptions.target != BuildTarget.WebGL)
            {
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new BuildFailedException("Addressables settings are required for the Web bootstrap.");
            }

            if (settings.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
            {
                return;
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new BuildFailedException(result.Error);
            }

            if (!File.Exists(Path.Combine(Addressables.BuildPath, "settings.json")))
            {
                throw new BuildFailedException("The Web Addressables runtime settings were not generated.");
            }
        }

        // Addressables' processor runs at 1 and includes this data in StreamingAssets/aa.
        public override int callbackOrder => 0;
    }
}
