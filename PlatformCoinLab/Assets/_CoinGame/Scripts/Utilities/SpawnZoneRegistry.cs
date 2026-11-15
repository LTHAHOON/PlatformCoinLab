using System.Collections.Generic;
using UnityEngine;

namespace CoinGame.Utilities
{
    public sealed class SpawnZoneRegistry : MonoBehaviour
    {
        public static SpawnZoneRegistry Instance { get; private set; }

        [SerializeField]
        private List<BoxCollider> _playerZones = new();
        [SerializeField]
        private List<BoxCollider> _collectorPlayerZones = new();
        [SerializeField]
        private List<BoxCollider> _disruptorPlayerZones = new();
        [SerializeField]
        private List<BoxCollider> _coinZones = new();
        [SerializeField]
        private List<BoxCollider> _crateZones = new();

        private void Awake()
        {
            InitializeLists();
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void InitializeLists()
        {
            if (_playerZones == null)
            {
                _playerZones = new();
            }
            if (_collectorPlayerZones == null)
            {
                _collectorPlayerZones = new();
            }
            if (_disruptorPlayerZones == null)
            {
                _disruptorPlayerZones = new();
            }
            if (_coinZones == null)
            {
                _coinZones = new();
            }
            if (_crateZones == null)
            {
                _crateZones = new();
            }

            _playerZones.RemoveAll(zone => zone == null);
            _collectorPlayerZones.RemoveAll(zone => zone == null);
            _disruptorPlayerZones.RemoveAll(zone => zone == null);
            _coinZones.RemoveAll(zone => zone == null);
            _crateZones.RemoveAll(zone => zone == null);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public List<BoxCollider> PlayerZones => _playerZones;
        public List<BoxCollider> CollectorPlayerZones => _collectorPlayerZones;
        public List<BoxCollider> DisruptorPlayerZones => _disruptorPlayerZones;
        public List<BoxCollider> CoinZones => _coinZones;
        public List<BoxCollider> CrateZones => _crateZones;
    }
}
