using System;
using UnityEngine;

namespace CoinGame.Input
{
    [Serializable]
    public struct PlayerInputState
    {
        public Vector2 Move;
        public bool Attack;
        public bool Interact;
        public bool WeaponPrevious;
        public bool WeaponNext;
        public bool Reload;
        public bool Boost;
        public bool Revive;
        public bool Push;
        public bool Skill;
    }
}
