using UnityEngine;

namespace CoinGame.Network
{
    public static class RoomSettingsValidator
    {
        public static bool TryRead(RoomLobbyView view, out RoomSettings settings)
        {
            settings = default;
            if (!view)
            {
                return false;
            }

            if (!int.TryParse(view.MaxPlayers, out int players) || players < 1 || players > 8)
            {
                return Fail(view, "Max Players must be between 1 and 8.");
            }

            if (!int.TryParse(view.MaxCollectors, out int collectors) || collectors < 0 || collectors > players)
            {
                return Fail(view, "Check Max Collectors.");
            }

            if (!int.TryParse(view.MaxDisruptors, out int disruptors) || disruptors < 0 || disruptors > players)
            {
                return Fail(view, "Check Max Disruptors.");
            }

            if (collectors + disruptors != players)
            {
                return Fail(view, "Role totals must equal Max Players.");
            }

            if (players > 1 && (collectors == 0 || disruptors == 0))
            {
                return Fail(view, "Two or more players require both roles.");
            }

            if (!int.TryParse(view.MaxRounds, out int rounds) || rounds < 1 || rounds > 20)
            {
                return Fail(view, "Max Rounds must be between 1 and 20.");
            }

            if (!int.TryParse(view.CoinMultiplier, out int multiplier) || multiplier < 1 || multiplier > 5)
            {
                return Fail(view, "Coin multiplier must be between 1 and 5.");
            }

            if (string.IsNullOrWhiteSpace(view.RoomId) || string.IsNullOrWhiteSpace(view.RoomName))
            {
                return Fail(view, "Enter a room name and Room ID.");
            }

            settings = new RoomSettings(view.RoomId, view.RoomName, players, collectors, disruptors, rounds, multiplier);
            return true;
        }

        private static bool Fail(RoomLobbyView view, string message)
        {
            view.SetStatus(message);
            return false;
        }
    }
}
