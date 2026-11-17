using Unity.Collections;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Network
{
    using CoinGame.Player;

    public sealed class NetworkPlayerIdentity : NetworkBehaviour
    {
        [SerializeField]
        private PlayerNameSettings _settings;
        private readonly NetworkVariable<FixedString64Bytes> _displayName = new();
        private readonly NetworkVariable<int> _number = new();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                AssignNumberServer();
            }

            if (IsOwner)
            {
                LocalPlayerProfile.NameChanged += SendName;
                SendName();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                LocalPlayerProfile.NameChanged -= SendName;
            }
        }

        private void SendName()
        {
            SetNameRpc(LocalPlayerProfile.DisplayName);
        }

        private void AssignNumberServer()
        {
            HashSet<int> usedNumbers = new();
            foreach (NetworkObject player in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (player != NetworkObject && player.IsPlayerObject)
                {
                    NetworkPlayerIdentity identity = player.GetComponent<NetworkPlayerIdentity>();
                    usedNumbers.Add(identity._number.Value);
                }
            }

            for (int offset = 0; offset < _settings.NumberCount; offset++)
            {
                int number = _settings.GetNumber(OwnerClientId, offset);
                if (usedNumbers.Add(number))
                {
                    _number.Value = number;
                    return;
                }
            }

            throw new InvalidOperationException("Player name number range is exhausted.");
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetNameRpc(string value)
        {
            string name = _settings.Normalize(value);
            if (name.Length > 0)
            {
                _displayName.Value = new FixedString64Bytes(name);
            }
        }

        public string DisplayName
        {
            get
            {
                if (_displayName.Value.IsEmpty || _number.Value == 0)
                {
                    return string.Empty;
                }

                return $"{_displayName.Value}#{_number.Value:0000}";
            }
        }
    }
}
