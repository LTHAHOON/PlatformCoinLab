using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoinGame.Player
{
    public class PlayerScoreStore : MonoBehaviour
    {
        public static PlayerScoreStore Instance { get; set; }
        private readonly Dictionary<Guid, int> _dicScoreData = new();

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
        }

        public void SetScoreData(Guid playerId, int curScore)
        {
            if (!_dicScoreData.ContainsKey(playerId))
            {
                _dicScoreData.Add(playerId, curScore);
                return;
            }
            _dicScoreData[playerId] = curScore;
        }

        public bool TryGetScoreData(Guid playerId, out int curScore)
        {
            if (_dicScoreData.TryGetValue(playerId, out int score))
            {
                curScore = score;
                return true;
            }
            curScore = 0;
            return false;
        }

    }

}