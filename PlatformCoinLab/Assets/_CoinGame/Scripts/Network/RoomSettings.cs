namespace CoinGame.Network
{
    public readonly struct RoomSettings
    {
        public readonly string RoomId;
        public readonly string RoomName;
        public readonly int MaxPlayers;
        public readonly int MaxCollectors;
        public readonly int MaxDisruptors;
        public readonly int MaxRounds;
        public readonly int CoinMultiplier;

        public RoomSettings(string roomId, string roomName, int maxPlayers, int maxCollectors,
            int maxDisruptors, int maxRounds, int coinMultiplier)
        {
            RoomId = roomId;
            RoomName = roomName;
            MaxPlayers = maxPlayers;
            MaxCollectors = maxCollectors;
            MaxDisruptors = maxDisruptors;
            MaxRounds = maxRounds;
            CoinMultiplier = coinMultiplier;
        }
    }
}
