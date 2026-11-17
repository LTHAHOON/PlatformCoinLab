using System.Text;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.UI
{
    using CoinGame.Network;
    using CoinGame.Player;

    public sealed class RoleRosterView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _collectors;
        [SerializeField]
        private TMP_Text _disruptors;
        private readonly StringBuilder _collectorNames = new();
        private readonly StringBuilder _disruptorNames = new();
        private readonly Dictionary<ulong, NetworkObject> _playersByOwner = new();
        private readonly List<NetworkObject> _players = new();

        private void Update()
        {
            _collectorNames.Clear();
            _disruptorNames.Clear();
            _playersByOwner.Clear();
            _players.Clear();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                foreach (NetworkObject instance in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
                {
                    if (!instance.IsPlayerObject)
                    {
                        continue;
                    }

                    List<NetworkObject> registeredPlayers = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObjects(instance.OwnerClientId);
                    if (registeredPlayers != null && registeredPlayers.Count > 0 && registeredPlayers[0] == instance)
                    {
                        _playersByOwner[instance.OwnerClientId] = instance;
                    }
                }

                _players.AddRange(_playersByOwner.Values);
                _players.Sort(CompareOwners);
                foreach (NetworkObject instance in _players)
                {
                    AppendPlayer(instance);
                }
            }

            _collectors.SetText(_collectorNames);
            _disruptors.SetText(_disruptorNames);
        }

        private void AppendPlayer(NetworkObject instance)
        {
            NetworkPlayerStats stats = instance.GetComponent<NetworkPlayerStats>();
            if (stats.Role == PlayerRole.Unselected)
            {
                return;
            }

            string name = instance.GetComponent<NetworkPlayerIdentity>().DisplayName;
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (stats.Role == PlayerRole.Collector)
            {
                AppendName(_collectorNames, name);
            }
            else if (stats.Role == PlayerRole.Disruptor)
            {
                AppendName(_disruptorNames, name);
            }
        }

        private static void AppendName(StringBuilder names, string name)
        {
            if (names.Length > 0)
            {
                names.AppendLine();
            }

            names.Append(name);
        }

        private static int CompareOwners(NetworkObject left, NetworkObject right)
        {
            return left.OwnerClientId.CompareTo(right.OwnerClientId);
        }
    }
}
