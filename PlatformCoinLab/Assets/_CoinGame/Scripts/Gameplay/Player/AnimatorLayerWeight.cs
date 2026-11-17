using UnityEngine;

namespace CoinGame.Gameplay.Player
{
    public sealed class AnimatorLayerWeight : StateMachineBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float _weight;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetLayerWeight(layerIndex, _weight);
        }
    }
}
