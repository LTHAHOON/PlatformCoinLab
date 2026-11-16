using System;
using UnityEngine;

namespace CoinGame.Utilities
{
    [Flags]
    public enum GridCellFlags : byte
    {
        None = 0,
        Ground = 1,
        StaticBlocked = 2,
        PlayerZone = 4,
        CollectorZone = 8,
        DisruptorZone = 16,
        CoinZone = 32,
        CrateZone = 64,
    }

    [Serializable]
    public struct GridSpawnCell
    {
        [SerializeField]
        private Vector2Int _coordinates;
        [SerializeField]
        private Vector3 _worldPosition;
        [SerializeField]
        private GridCellFlags _flags;

        public GridSpawnCell(Vector2Int coordinates, Vector3 worldPosition, GridCellFlags flags)
        {
            _coordinates = coordinates;
            _worldPosition = worldPosition;
            _flags = flags;
        }

        public Vector2Int Coordinates => _coordinates;
        public Vector3 WorldPosition => _worldPosition;
        public GridCellFlags Flags => _flags;
    }

    [CreateAssetMenu(menuName = "CoinGame/Grid Spawn Map")]
    public sealed class GridSpawnMap : ScriptableObject
    {
        [SerializeField]
        private string _sourceScene;
        [SerializeField]
        private string _groundPath;
        [SerializeField]
        private Vector3 _worldOrigin;
        [SerializeField]
        private Vector2 _cellSize;
        [SerializeField]
        private int _width;
        [SerializeField]
        private int _height;
        [SerializeField]
        private GridSpawnCell[] _cells;

        public void SetData(string sourceScene, string groundPath, Vector3 worldOrigin, Vector2 cellSize,
            int width, int height, GridSpawnCell[] cells)
        {
            _sourceScene = sourceScene;
            _groundPath = groundPath;
            _worldOrigin = worldOrigin;
            _cellSize = cellSize;
            _width = width;
            _height = height;
            _cells = cells;
        }

        public int Width => _width;
        public int Height => _height;
        public Vector2 CellSize => _cellSize;
        public Vector3 WorldOrigin => _worldOrigin;
        public GridSpawnCell[] Cells => _cells;
    }
}
