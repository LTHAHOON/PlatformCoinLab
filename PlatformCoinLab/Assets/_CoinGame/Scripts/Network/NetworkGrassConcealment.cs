using Unity.Netcode;

namespace CoinGame.Network
{
    using CoinGame.Stage;

    public sealed class NetworkGrassConcealment : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> _concealed = new NetworkVariable<bool>(false);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _concealed.Value = GrassPatch.ContainsAny(transform.position);
            }
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            _concealed.Value = GrassPatch.ContainsAny(transform.position);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _concealed.Value = false;
            }
        }

        public bool IsConcealed => _concealed.Value;
    }
}
