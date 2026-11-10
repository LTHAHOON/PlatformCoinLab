using Unity.Netcode;
using System;
using UnityEngine;
using CoinGame.UI;
using CoinGame.Utilities;
using CoinGame.Player;
using CoinGame.Gameplay.Combat;
using CoinGame.Gameplay.Player;
using CoinGame.Stage;
using CoinGame.Gameplay.Items;
using CoinGame.Managers;

namespace CoinGame.Network
{
    public class NetworkPlayerStats : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _curScore = new(0);
        private readonly NetworkVariable<PlayerRole> _role = new(PlayerRole.Unselected);
        private readonly NetworkVariable<double> _coinLockUntil = new(0d);
        private readonly NetworkVariable<double> _pushedUntil = new(0d);
        private readonly NetworkVariable<int> _pushAttempts = new(0);
        private readonly NetworkVariable<int> _successfulPushes = new(0);
        private readonly NetworkVariable<int> _successfulLocks = new(0);
        private readonly NetworkVariable<int> _successfulSteals = new(0);
        private readonly NetworkVariable<int> _disruptionScore = new(0);
        private readonly NetworkVariable<int> _matchPlayerCount = new(0);
        private readonly NetworkVariable<int> _health = new(100);
        private readonly NetworkVariable<int> _maxHealth = new(100);
        private readonly NetworkVariable<PlayerLifeState> _lifeState = new(PlayerLifeState.Alive);
        private readonly NetworkVariable<double> _reviveReadyAt = new(0d);
        private readonly NetworkVariable<double> _invulnerableUntil = new(0d);
        private readonly NetworkVariable<double> _stunnedUntil = new(0d);
        private readonly NetworkVariable<double> _stealthUntil = new(0d);
        private readonly NetworkVariable<double> _itemBlockedUntil = new(0d);
        private readonly NetworkVariable<double> _boostUntil = new(0d);
        private double _respawnAt;
        [SerializeField]
        private PlayerStatsConfig _config;
        private PlayerStatsView _playerStatsView;
        private bool _scoreEventsBound;
        private ulong _coinLockSourceClientId = ulong.MaxValue;
        private ulong _pushSourceClientId = ulong.MaxValue;

        public PlayerRole Role => _role.Value;
        public bool IsCoinLocked => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _coinLockUntil.Value;
        public bool IsBeingPushed => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _pushedUntil.Value;
        public int Score => _curScore.Value;
        public int DisruptionScore => _disruptionScore.Value;
        public int Health => _health.Value;
        public int MaxHealth => _maxHealth.Value;
        public PlayerLifeState LifeState => _lifeState.Value;
        public bool IsAlive => _lifeState.Value == PlayerLifeState.Alive;
        public double ReviveReadyAt => _reviveReadyAt.Value;
        public bool IsInvulnerable => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _invulnerableUntil.Value;
        public bool IsStunned => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _stunnedUntil.Value;
        public bool IsStealthed => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _stealthUntil.Value;
        public bool IsItemBlocked => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _itemBlockedUntil.Value;
        public bool IsBoosted => NetworkManager.Singleton != null && NetworkManager.Singleton.ServerTime.Time < _boostUntil.Value;
        public double InvulnerableRemaining => Remaining(_invulnerableUntil.Value);
        public double StealthRemaining => Remaining(_stealthUntil.Value);
        public double ItemBlockedRemaining => Remaining(_itemBlockedUntil.Value);
        public double BoostRemaining => Remaining(_boostUntil.Value);
        public event Action HudStateChanged;
        private int _lastHudSignature = int.MinValue;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                int maxHealth = _config != null ? _config.MaxHealth : 100;
                _maxHealth.Value = maxHealth;
                _health.Value = maxHealth;
                _lifeState.Value = PlayerLifeState.Alive;
                _reviveReadyAt.Value = 0d;
                _respawnAt = 0d;
                RefreshMatchPlayerCount();
            }

            if (!IsOwner)
            {
                return;
            }

            TryBindStatsView();
            if (!_scoreEventsBound)
            {
                _curScore.OnValueChanged += OnScoreChanged;
                _scoreEventsBound = true;
            }
            PlayerPrefsStorage.GetOrInitPlayerSessionFloat(PlayerSessionKey.SCORE, 0f);
        }

        private void Update()
        {
            UpdateLifeServer();
            int signature = HashCode.Combine(_health.Value, _lifeState.Value, IsInvulnerable, IsStealthed, IsItemBlocked, IsBoosted);
            if (signature != _lastHudSignature)
            {
                _lastHudSignature = signature;
                HudStateChanged?.Invoke();
            }
            if (IsOwner && !_playerStatsView)
            {
                TryBindStatsView();
            }
        }

        private double Remaining(double until)
        {
            return NetworkManager.Singleton == null ? 0d : Math.Max(0d, until - NetworkManager.Singleton.ServerTime.Time);
        }

        private void TryBindStatsView()
        {
            if (_playerStatsView)
            {
                return;
            }

            _playerStatsView = FindAnyObjectByType<PlayerStatsView>();
            if (!_playerStatsView)
            {
                return;
            }

            _playerStatsView.SetScoreText(0, _curScore.Value);
            _curScore.OnValueChanged += _playerStatsView.SetScoreText;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            if (_playerStatsView)
            {
                _playerStatsView.SetScoreText(0, 0);
                _curScore.OnValueChanged -= _playerStatsView.SetScoreText;
            }

            _curScore.OnValueChanged -= OnScoreChanged;
            _playerStatsView = null;
            _scoreEventsBound = false;
        }

        private const int MaxAddScoreAmount = 10000;
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
        public void AddScoreRpc(int addAmount)
        {
            if (addAmount <= 0 || addAmount > MaxAddScoreAmount)
            {
                Debug.LogWarning($"?섎せ???먯닔 利앷??? {addAmount}");
                return;
            }

            _curScore.Value += addAmount;
        }

        private const int MaxSetScoreAmount = 10000000;
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetScoreRpc(int curScore)
        {
            if (curScore < 0 || curScore > MaxSetScoreAmount)
            {
                Debug.LogWarning($"?섎せ???먯닔 ?ㅼ젙媛? {curScore}");
                return;
            }
            _curScore.Value = curScore;
        }

        public void OnScoreChanged(int previousValue, int newValue)
        {
        }

        public void ApplyDamageServer(DamageInfo info)
        {
            if (!IsServer || NetworkManager.Singleton == null || !IsAlive || IsInvulnerable || info.Amount <= 0)
            {
                return;
            }
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(info.Attacker, out NetworkClient attacker) &&
                attacker.PlayerObject != null && attacker.PlayerObject.TryGetComponent(out NetworkPlayerStats source) &&
                source.Role == Role)
            {
                return;
            }
            _stunnedUntil.Value = Mathf.Max((float)_stunnedUntil.Value, (float)(NetworkManager.Singleton.ServerTime.Time + 0.1d));
            _invulnerableUntil.Value = Mathf.Max((float)_invulnerableUntil.Value, (float)(NetworkManager.Singleton.ServerTime.Time + 0.3d));
            _health.Value = Mathf.Max(0, _health.Value - info.Amount);
            if (_health.Value > 0) return;
            _lifeState.Value = PlayerLifeState.Dead;
            _reviveReadyAt.Value = NetworkManager.Singleton.ServerTime.Time + (_config != null ? _config.ReviveReadyDelay : 3f);
            if (Role == PlayerRole.Collector)
            {
                _lifeState.Value = PlayerLifeState.Spectating;
                NetworkRoundState.Instance?.NotifyPlayerDeathServer();
                return;
            }
            _respawnAt = NetworkManager.Singleton.ServerTime.Time + (_config != null ? _config.DisruptorRespawnDelay : 5f);
        }

        public bool TryReviveServer()
        {
            if (!IsServer || Role != PlayerRole.Collector || _lifeState.Value != PlayerLifeState.Spectating ||
                NetworkManager.Singleton.ServerTime.Time > _reviveReadyAt.Value)
            {
                return false;
            }
            NetworkInventory inventory = GetComponent<NetworkInventory>();
            if (inventory == null || !inventory.ConsumeReviveServer()) return false;
            _health.Value = Mathf.Max(1, _maxHealth.Value / 2);
            _lifeState.Value = PlayerLifeState.Alive;
            _invulnerableUntil.Value = NetworkManager.Singleton.ServerTime.Time + 2d;
            return true;
        }

        public bool TryAcquireItemServer(ItemData item)
        {
            if (!IsServer || item == null || IsItemBlocked || !IsAlive) return false;
            NetworkInventory inventory = GetComponent<NetworkInventory>();
            if (inventory == null) return false;
            switch (item.Kind)
            {
                case ItemKind.Ammo: inventory.AddAmmoServer(item.Amount); return true;
                case ItemKind.Gun: inventory.SetWeaponServer(Gameplay.Combat.WeaponKind.Gun); return true;
                case ItemKind.Sword: inventory.SetWeaponServer(Gameplay.Combat.WeaponKind.Sword); return true;
                case ItemKind.Boost: return inventory.GiveBoostServer();
                case ItemKind.Revive: return inventory.GiveReviveServer();
                case ItemKind.Health: _health.Value = Mathf.Min(_maxHealth.Value, _health.Value + Mathf.Max(1, _maxHealth.Value * 30 / 100)); return true;
                case ItemKind.Invulnerable: _invulnerableUntil.Value = NetworkManager.Singleton.ServerTime.Time + Mathf.Max(0.1f, item.Duration); return true;
                case ItemKind.Stealth: _stealthUntil.Value = NetworkManager.Singleton.ServerTime.Time + Mathf.Max(0.1f, item.Duration); return true;
                case ItemKind.Random: return AcquireRandomServer(inventory);
                default: return false;
            }
        }

        private bool AcquireRandomServer(NetworkInventory inventory)
        {
            ItemKind kind = (ItemKind)UnityEngine.Random.Range((int)ItemKind.Ammo, (int)ItemKind.Revive + 1);
            switch (kind)
            {
                case ItemKind.Ammo: inventory.AddAmmoServer(24); return true;
                case ItemKind.Gun: inventory.SetWeaponServer(Gameplay.Combat.WeaponKind.Gun); return true;
                case ItemKind.Sword: inventory.SetWeaponServer(Gameplay.Combat.WeaponKind.Sword); return true;
                case ItemKind.Boost: return inventory.GiveBoostServer();
                case ItemKind.Revive: return inventory.GiveReviveServer();
                case ItemKind.Health: _health.Value = Mathf.Min(_maxHealth.Value, _health.Value + _maxHealth.Value * 30 / 100); return true;
                case ItemKind.Invulnerable: _invulnerableUntil.Value = NetworkManager.Singleton.ServerTime.Time + 4d; return true;
                case ItemKind.Stealth: _stealthUntil.Value = NetworkManager.Singleton.ServerTime.Time + 5d; return true;
                default: return false;
            }
        }

        public void RestoreHealthServer(int percent)
        {
            if (IsServer) _health.Value = Mathf.Min(_maxHealth.Value, _health.Value + _maxHealth.Value * Mathf.Clamp(percent, 1, 100) / 100);
        }

        public void GiveRandomDebugServer()
        {
            if (IsServer) AcquireRandomServer(GetComponent<NetworkInventory>());
        }

        public void SetItemBlockedServer(double until) { if (IsServer) _itemBlockedUntil.Value = Mathf.Max((float)_itemBlockedUntil.Value, (float)until); }
        public void SetInvulnerableServer(double until) { if (IsServer) _invulnerableUntil.Value = Mathf.Max((float)_invulnerableUntil.Value, (float)until); }
        public void ClearStealthServer() { if (IsServer) _stealthUntil.Value = 0d; }
        public void SetStealthServer(double until) { if (IsServer) _stealthUntil.Value = Mathf.Max((float)_stealthUntil.Value, (float)until); }
        public void SetBoostServer(double until) { if (IsServer) _boostUntil.Value = Mathf.Max((float)_boostUntil.Value, (float)until); }

        private void UpdateLifeServer()
        {
            if (!IsServer || _lifeState.Value != PlayerLifeState.Dead || NetworkManager.Singleton.ServerTime.Time < _respawnAt) return;
            _health.Value = _maxHealth.Value;
            _lifeState.Value = PlayerLifeState.Alive;
            NetworkRoundState.Instance?.RelocatePlayerServer(this);
        }

        public void ResetRoundLifeServer()
        {
            if (!IsServer)
            {
                return;
            }

            _health.Value = _maxHealth.Value;
            _lifeState.Value = PlayerLifeState.Alive;
            _reviveReadyAt.Value = 0d;
            _respawnAt = 0d;
        }

        public void SelectRole(PlayerRole role)
        {
            SelectRoleRpc(role);
        }

        public void AssignRoleServer(PlayerRole role)
        {
            if (IsServer && role is PlayerRole.Collector or PlayerRole.Disruptor)
            {
                _role.Value = role;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SelectRoleRpc(PlayerRole role)
        {
            if (role is not (PlayerRole.Collector or PlayerRole.Disruptor))
            {
                return;
            }

            int roleCount = 0;
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject == null || client.PlayerObject == gameObject)
                {
                    continue;
                }

                NetworkPlayerStats otherStats = client.PlayerObject.GetComponent<NetworkPlayerStats>();
                if (otherStats != null && otherStats.Role == role)
                {
                    ++roleCount;
                }
            }

            int maxRoleCount = role == PlayerRole.Collector
                ? RoomLobbyController.Instance?.MaxCollectors ?? 1
                : RoomLobbyController.Instance?.MaxDisruptors ?? 1;
            if (roleCount >= maxRoleCount)
            {
                return;
            }

            _role.Value = role;
        }

        public bool TrySpendScore(int amount)
        {
            if (!IsServer || amount <= 0 || _curScore.Value < amount)
            {
                return false;
            }

            _curScore.Value -= amount;
            return true;
        }

        public void AddScoreServer(int amount)
        {
            if (IsServer && amount > 0)
            {
                _curScore.Value += amount;
            }
        }

        public void SetCoinLockServer(double until, NetworkPlayerStats source)
        {
            if (IsServer)
            {
                _coinLockUntil.Value = until;
                _coinLockSourceClientId = source != null ? source.OwnerClientId : ulong.MaxValue;
            }
        }

        public void SetBeingPushedServer(double until, NetworkPlayerStats source)
        {
            if (IsServer)
            {
                _pushedUntil.Value = Mathf.Max((float)_pushedUntil.Value, (float)until);
                _pushSourceClientId = source != null ? source.OwnerClientId : ulong.MaxValue;
            }
        }

        public void RegisterBlockedCoinAttemptServer()
        {
            if (!IsServer || NetworkManager.Singleton == null)
            {
                return;
            }

            ulong sourceClientId = IsCoinLocked ? _coinLockSourceClientId :
                IsBeingPushed ? _pushSourceClientId : ulong.MaxValue;
            if (sourceClientId == ulong.MaxValue ||
                !NetworkManager.Singleton.ConnectedClients.TryGetValue(sourceClientId, out NetworkClient sourceClient) ||
                sourceClient.PlayerObject == null)
            {
                return;
            }

            NetworkPlayerStats sourceStats = sourceClient.PlayerObject.GetComponent<NetworkPlayerStats>();
            if (sourceStats != null)
            {
                sourceStats._disruptionScore.Value++;
            }
        }

        public void ResetRoundDisruptorStatsServer()
        {
            if (!IsServer) return;
            _pushAttempts.Value = 0;
            _successfulPushes.Value = 0;
            _successfulLocks.Value = 0;
            _successfulSteals.Value = 0;
            _disruptionScore.Value = 0;
            _pushedUntil.Value = 0d;
            _coinLockSourceClientId = ulong.MaxValue;
            _pushSourceClientId = ulong.MaxValue;
        }

        private void RefreshMatchPlayerCount()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (!IsServer || networkManager == null)
            {
                return;
            }

            int playerCount = networkManager.ConnectedClientsList.Count;
            foreach (NetworkClient client in networkManager.ConnectedClientsList)
            {
                NetworkPlayerStats playerStats = client.PlayerObject != null
                    ? client.PlayerObject.GetComponent<NetworkPlayerStats>()
                    : null;
                if (playerStats != null)
                {
                    playerStats._matchPlayerCount.Value = playerCount;
                }
            }
        }
    }
}




