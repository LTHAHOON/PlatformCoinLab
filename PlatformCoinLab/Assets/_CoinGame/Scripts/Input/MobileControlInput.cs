using UnityEngine;

namespace CoinGame.Input
{
    public enum MobileAction
    {
        Attack, Reload, Fist, Gun, Sword, Boost, Revive, Push, Skill
    }

    public sealed class MobileControlInput : MonoBehaviour
    {
        private PlayerInputState _pending;

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            Clear();
            if (Active == this)
            {
                Active = null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Clear();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                Clear();
            }
        }

        public void Move(Vector2 value)
        {
            _pending.Move = value;
        }

        public void Press(MobileAction action)
        {
            switch (action)
            {
                case MobileAction.Attack:
                    _pending.Attack = true;
                    break;
                case MobileAction.Reload:
                    _pending.Reload = true;
                    break;
                case MobileAction.Fist:
                    _pending.SelectFist = true;
                    break;
                case MobileAction.Gun:
                    _pending.SelectGun = true;
                    break;
                case MobileAction.Sword:
                    _pending.SelectSword = true;
                    break;
                case MobileAction.Boost:
                    _pending.Boost = true;
                    break;
                case MobileAction.Revive:
                    _pending.Revive = true;
                    break;
                case MobileAction.Push:
                    _pending.Push = true;
                    break;
                case MobileAction.Skill:
                    _pending.Skill = true;
                    break;
            }
        }

        public PlayerInputState Consume()
        {
            PlayerInputState state = _pending;
            _pending = new PlayerInputState { Move = state.Move };
            return state;
        }

        public void Clear()
        {
            _pending = default;
        }

        public static MobileControlInput Active { get; private set; }
    }
}
