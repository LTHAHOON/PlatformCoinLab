using UnityEngine;

namespace CoinGame.Player
{
    [CreateAssetMenu(menuName = "CoinGame/Player Name Settings")]
    public sealed class PlayerNameSettings : ScriptableObject
    {
        [SerializeField]
        private int _maxLength = 16;
        [SerializeField]
        [Range(1000, 9999)]
        private int _firstNumber = 1000;
        [SerializeField]
        [Range(1000, 9999)]
        private int _lastNumber = 9999;

        public int GetNumber(ulong ownerId, int offset)
        {
            int first = Mathf.Clamp(_firstNumber, 1000, 9999);
            return first + (int)((ownerId % (ulong)NumberCount + (ulong)offset) % (ulong)NumberCount);
        }

        public string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string name = value.Trim();
            foreach (char character in name)
            {
                if (char.IsControl(character))
                {
                    return string.Empty;
                }
            }

            int limit = Mathf.Clamp(_maxLength, 1, 20);
            if (name.Length > limit)
            {
                name = name.Substring(0, limit);
                if (char.IsHighSurrogate(name[name.Length - 1]))
                {
                    name = name.Substring(0, name.Length - 1);
                }
            }

            return name;
        }

        public int MaxLength => Mathf.Clamp(_maxLength, 1, 20);
        public int NumberCount => Mathf.Clamp(_lastNumber, Mathf.Clamp(_firstNumber, 1000, 9999), 9999) - Mathf.Clamp(_firstNumber, 1000, 9999) + 1;
    }
}
