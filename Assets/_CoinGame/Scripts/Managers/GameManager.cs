using System.Collections.Generic;
using CoinGame.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinGame.Managers
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _plableDirector;
        [SerializeField]
        private SceneReference _inGameSceneRef;
        [SerializeField]
        private SceneReference _outGameSceneRef;
        
        private bool _isLoaded = false;
        public static GameManager Instance {get; private set;}

        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
        }

        public void IN_OnClickMenuButton_Timeline(PlayableAsset playableAsset)
        {
            _plableDirector.playableAsset = playableAsset;
            _plableDirector.Play();
        }

        public void OUT_OnClickMenuButton_Timeline(PlayableAsset playableAsset)
        {
            _plableDirector.Stop();
            _plableDirector.playableAsset = playableAsset;
            _plableDirector.Play();
        }
        

        public void OnPlayCoinGame_Rcv()
        {
            LoadSceneAsync(_inGameSceneRef).Forget();
        }

        public async UniTask LoadSceneAsync(SceneReference sceneRefToLoad)
        {
            Scene sceneToLoad = sceneRefToLoad.GetScene();
            await SceneManager.LoadSceneAsync(sceneToLoad.buildIndex, LoadSceneMode.Additive);
            _isLoaded = true;
        }

        public async UniTask UnloadSceneAsync(SceneReference sceneRefToUnload)
        {
            Scene sceneToUnload = sceneRefToUnload.GetScene();
            await SceneManager.UnloadSceneAsync(sceneToUnload.buildIndex);
        }
        
    }
}
