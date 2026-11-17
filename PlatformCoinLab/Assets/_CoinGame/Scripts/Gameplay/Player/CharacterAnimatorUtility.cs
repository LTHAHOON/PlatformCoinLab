using UnityEngine;

namespace CoinGame.Gameplay.Player
{
    public sealed class CharacterAnimatorUtility : MonoBehaviour
    {
        [SerializeField]
        private AnimatorParameterSet _parameters;
        [SerializeField]
        private Animator _animator;

        public void SetMove(float value)
        {
            SetFloat(_parameters != null ? _parameters.Move : "MoveSpeed", value);
        }

        public void SetFloat(string parameter, float value)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Float))
            {
                _animator.SetFloat(parameter, value);
            }
        }

        public void SetBool(string parameter, bool value)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Bool))
            {
                _animator.SetBool(parameter, value);
            }
        }

        public void SetInteger(string parameter, int value)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Int))
            {
                _animator.SetInteger(parameter, value);
            }
        }

        public void Trigger(string parameter)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Trigger))
            {
                _animator.SetTrigger(parameter);
            }
        }

        public void ResetTrigger(string parameter)
        {
            if (HasParameter(parameter, AnimatorControllerParameterType.Trigger))
            {
                _animator.ResetTrigger(parameter);
            }
        }

        private bool HasParameter(string parameter, AnimatorControllerParameterType type)
        {
            if (_animator == null || string.IsNullOrEmpty(parameter))
            {
                return false;
            }

            foreach (AnimatorControllerParameter item in _animator.parameters)
            {
                if (item.name == parameter && item.type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
