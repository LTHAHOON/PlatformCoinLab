using System.Collections.Generic;
using UnityEngine;

namespace CoinGame.Utilities
{
    using CoinGame.Player;

    public static class GridSpawnReservation
    {
        private static GridSpawnMap _map;
        private static readonly Dictionary<int, GridSpawnKind> _owners = new();
        private static readonly Dictionary<ulong, int> _players = new();

        public static void Configure(GridSpawnMap map)
        {
            if (_map == map)
            {
                return;
            }

            _map = map;
            _owners.Clear();
            _players.Clear();
        }

        public static void ReleasePlayer(ulong clientId)
        {
            if (!_players.Remove(clientId, out int index))
            {
                return;
            }

            _owners.Remove(index);
        }

        public static void ReleaseAllPlayers()
        {
            foreach (int index in _players.Values)
            {
                _owners.Remove(index);
            }

            _players.Clear();
        }

        public static bool TryReservePlayer(ulong clientId, PlayerRole role, out Vector3 position)
        {
            position = default;
            if (_map == null)
            {
                return false;
            }

            ReleasePlayer(clientId);
            GridCellFlags required = role == PlayerRole.Collector
                ? GridCellFlags.CollectorZone
                : role == PlayerRole.Disruptor
                    ? GridCellFlags.DisruptorZone
                    : GridCellFlags.PlayerZone;
            GridSpawnCell[] cells = _map.Cells;
            for (int index = 0; index < cells.Length; index++)
            {
                if (!CanReserve(cells[index], required))
                {
                    continue;
                }

                _owners[index] = GridSpawnKind.Player;
                _players[clientId] = index;
                position = cells[index].WorldPosition;
                return true;
            }

            return false;
        }

        private static bool CanReserve(GridSpawnCell cell, GridCellFlags required)
        {
            int index = cell.Coordinates.x + cell.Coordinates.y * _map.Width;
            return (cell.Flags & required) == required &&
                (cell.Flags & GridCellFlags.Ground) == GridCellFlags.Ground &&
                (cell.Flags & GridCellFlags.StaticBlocked) == GridCellFlags.None &&
                !_owners.ContainsKey(index);
        }
    }

    public enum GridSpawnKind : byte
    {
        Player,
        Coin,
        Crate,
    }
}
