using UnityEngine;

namespace CoinGame.Gameplay.M1
{
    public class M1Coin : MonoBehaviour
    {
        private M1Prototype _stage;

        public void Initialize(M1Prototype stage)
        {
            _stage = stage;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("M1Player"))
            {
                _stage.CollectCoin(this);
            }
        }
    }
}