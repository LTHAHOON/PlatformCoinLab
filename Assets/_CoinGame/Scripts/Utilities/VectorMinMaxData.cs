using UnityEngine;

namespace CoinGame.Utilities
{
    [CreateAssetMenu(fileName = "new VectorMinMaxData", menuName = "CoinGame/Utilites/VectorMinMaxData")]
    public class VectorMinMaxData : ScriptableObject
    {
        [SerializeField]
        private Vector3 _minVector;
        [SerializeField]
        private Vector3 _maxVector;

        public Vector3 MinVector => _minVector;
        public Vector3 MaxVetor => _maxVector;        
    }
}
