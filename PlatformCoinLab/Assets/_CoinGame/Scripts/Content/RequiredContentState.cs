using UnityEngine;

namespace CoinGame.Content
{
    internal static class RequiredContentState
    {
        private static string _readyLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
            _readyLabel = null;
        }

        internal static void MarkReady(string label)
        {
            _readyLabel = label;
        }

        internal static bool IsReadyFor(string label)
        {
            return _readyLabel == label;
        }
    }
}
