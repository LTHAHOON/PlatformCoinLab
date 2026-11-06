using UnityEngine;

namespace CoinGame.Gameplay.M1
{
    [CreateAssetMenu(menuName = "CoinGame/M1 Stage Config")]
    public class M1StageConfig : ScriptableObject
    {
        // TODO(GDD): [제안] 기본 이동 속도는 이 ScriptableObject에서 조정한다.
        [SerializeField]
        private float _moveSpeed = 4f;
        [SerializeField]
        private Vector3 _playerStart = new(0f, 1f, 0f);
        [SerializeField]
        private Vector3 _playerScale = Vector3.one;
        [SerializeField]
        private Vector3 _coinScale = Vector3.one;
        [SerializeField]
        private Vector3 _cameraPosition = new(0f, 18f, -14f);
        [SerializeField]
        private Vector3 _cameraEuler = new(55f, 0f, 0f);
        // TODO(GDD): [제안] 프로토타입 코인 배치는 스테이지 데이터로 교체한다.
        [SerializeField]
        private Vector3[] _coinPositions =
        {
            new(-6f, 0.6f, -4f), new(-2f, 0.6f, -4f),
            new(2f, 0.6f, -4f), new(6f, 0.6f, -4f),
            new(-6f, 0.6f, 4f), new(-2f, 0.6f, 4f),
            new(2f, 0.6f, 4f), new(6f, 0.6f, 4f)
        };

        public float MoveSpeed => _moveSpeed;
        public Vector3 PlayerStart => _playerStart;
        public Vector3 PlayerScale => _playerScale;
        public Vector3 CoinScale => _coinScale;
        public Vector3 CameraPosition => _cameraPosition;
        public Vector3 CameraEuler => _cameraEuler;
        public Vector3[] CoinPositions => _coinPositions;
    }
}
