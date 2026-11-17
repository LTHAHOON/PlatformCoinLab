using System;

namespace CoinGame.Player
{
    public static class PlayerProfileStorage
    {
        private const string BaseNameKey = "CoinGame.DisplayName.v1";

        private static string GetNameKey()
        {
#if UNITY_EDITOR
            if (!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor)
            {
                string[] arguments = Environment.GetCommandLineArgs();
                for (int i = 0; i < arguments.Length - 1; i++)
                {
                    if (arguments[i] == "-name")
                    {
                        return BaseNameKey + "." + arguments[i + 1];
                    }
                }

                throw new InvalidOperationException("Virtual Player is missing its -name profile argument.");
            }
#endif
            return BaseNameKey;
        }

        public static string NameKey => GetNameKey();
    }
}
