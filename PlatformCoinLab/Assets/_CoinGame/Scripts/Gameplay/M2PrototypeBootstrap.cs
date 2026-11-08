using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoinGame.Managers;

namespace CoinGame.Gameplay
{
    public class M2PrototypeBootstrap : MonoBehaviour
    {
        private const string PersistentScenePath =
            "Assets/_CoinGame/Scenes/Persistent_CoinGameScene.unity";

        private async void Start()
        {
            if (SceneManager.GetSceneByPath(PersistentScenePath).isLoaded)
            {
                return;
            }

            await SceneManager.LoadSceneAsync(PersistentScenePath, LoadSceneMode.Additive);
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            gameManager?.ConfigureInGameSceneServices();
        }
    }
}
