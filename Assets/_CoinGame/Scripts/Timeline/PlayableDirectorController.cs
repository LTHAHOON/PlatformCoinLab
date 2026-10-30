using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace CoinGame.Timeline
{
    using CoinGame.Managers;

    [RequireComponent(typeof(PlayableDirector))]
    public class PlayableDirectorController : MonoBehaviour
    {
        [SerializeField]
        private PlayableAsset _Start_PlayableAsset;
        
        private PlayableDirector _playableDirector;

        private void Awake()
        {
            _playableDirector = GetComponent<PlayableDirector>();
        }

        private void OnEnable()
        {
            if(_Start_PlayableAsset)
            {
                OnPlayTimeline(_Start_PlayableAsset);
            }
        }

        public void OnPlayTimeline(PlayableAsset playableAsset)
        {
            _playableDirector.Stop();
            _playableDirector.playableAsset = playableAsset;
            _playableDirector.Play();
        }

        private void OnLoadScene(GameScene gameScene)
        {
            GameManager.Instance.LoadSceneAsync(gameScene).Forget();
        }

        public void OnLoadScene(int gameScene)
        {
            if (gameScene < (int)GameScene.OutGame || gameScene > (int)GameScene.InGame)
            {
                Debug.LogError($"Invalid game scene value: {gameScene}.", this);
                return;
            }

            OnLoadScene((GameScene)gameScene);
        }
    }
}
