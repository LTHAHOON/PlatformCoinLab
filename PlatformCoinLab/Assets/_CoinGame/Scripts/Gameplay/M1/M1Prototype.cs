using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using CoinGame.Network;

namespace CoinGame.Gameplay.M1
{
    public class M1Prototype : MonoBehaviour
    {
        [SerializeField]
        private M1StageConfig _config;

        private readonly List<M1Coin> _coins = new();
        private CharacterController _player;
        private InputActionMap _gameplay;
        private InputAction _move;
        private bool _cleared;

        private void Start()
        {
            if ((NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) ||
                (NetworkRoundState.Instance != null && NetworkRoundState.Instance.IsSpawned))
            {
                gameObject.SetActive(false);
                return;
            }

            if (_config == null)
            {
                Debug.LogError("M1StageConfig is not assigned.", this);
                return;
            }

            CreatePlayer();
            CreateCoins();
            SetupCamera();
            _gameplay = new InputActionMap("Gameplay");
            _move = _gameplay.AddAction("Move", InputActionType.Value, "Vector2");
            _move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _gameplay.Enable();
        }

        private void Update()
        {
            if (_player == null || _cleared)
            {
                return;
            }

            Vector2 input = _move.ReadValue<Vector2>();
            Vector3 direction = new(input.x, 0f, input.y);
            _player.Move(direction * (_config.MoveSpeed * Time.deltaTime));
        }

        private void CreatePlayer()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "M1_Player";
            player.tag = "M1Player";
            player.transform.SetPositionAndRotation(_config.PlayerStart, Quaternion.identity);
            player.transform.localScale = _config.PlayerScale;
            Destroy(player.GetComponent<CapsuleCollider>());
            _player = player.AddComponent<CharacterController>();
        }

        private void CreateCoins()
        {
            foreach (Vector3 position in _config.CoinPositions)
            {
                GameObject coinObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coinObject.name = "M1_Coin";
                coinObject.transform.SetPositionAndRotation(position, Quaternion.identity);
                coinObject.transform.localScale = _config.CoinScale;
                CapsuleCollider collider = coinObject.GetComponent<CapsuleCollider>();
                collider.isTrigger = true;
                Rigidbody body = coinObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                M1Coin coin = coinObject.AddComponent<M1Coin>();
                coin.Initialize(this);
                _coins.Add(coin);
            }
        }

        private void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.SetPositionAndRotation(_config.CameraPosition, Quaternion.Euler(_config.CameraEuler));
            }
        }

        public void CollectCoin(M1Coin coin)
        {
            if (!_coins.Remove(coin))
            {
                return;
            }

            Destroy(coin.gameObject);
            if (_coins.Count == 0)
            {
                _cleared = true;
            }
        }

        private void OnGUI()
        {
            int total = _config != null ? _config.CoinPositions.Length : 0;
            GUI.Label(new Rect(24f, 24f, 320f, 32f), $"Coins: {total - _coins.Count}/{total}");
            if (_cleared)
            {
                GUI.Label(new Rect(24f, 64f, 420f, 40f), "STAGE CLEAR");
            }
        }

        private void OnDestroy()
        {
            _gameplay?.Disable();
            _gameplay?.Dispose();
        }
    }
}
