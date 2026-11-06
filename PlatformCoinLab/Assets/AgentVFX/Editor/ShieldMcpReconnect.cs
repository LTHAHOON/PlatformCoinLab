using UnityEditor;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;

internal static class ShieldMcpReconnect
{
    [InitializeOnLoadMethod]
    private static void Connect()
    {
        EditorApplication.delayCall += async () =>
        {
            await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Http);
        };
    }
}
