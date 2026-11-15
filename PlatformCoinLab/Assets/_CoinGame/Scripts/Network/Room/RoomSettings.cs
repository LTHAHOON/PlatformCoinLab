namespace CoinGame.Network
{
    public struct RoomSettings
    {
        public string RoomId {get; private set;}
        public string RoomName {get; private set;}
        public int MaxPlayers {get; private set;}
        public int MaxCollectors {get; private set;}
        public int MaxDisruptors {get; private set;}
        public int MaxRounds {get; private set;}
        public int CoinMultiplier {get; private set;}

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
