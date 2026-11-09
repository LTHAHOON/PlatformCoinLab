using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using CoinGame.Managers;
using CoinGame.Network;

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

            DevQuickStart quickStart = GetComponent<DevQuickStart>();

            await SceneManager.LoadSceneAsync(PersistentScenePath, LoadSceneMode.Additive);
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            gameManager?.ConfigureInGameSceneServices();
            if (quickStart != null)
            {
                quickStart.Begin();
            }
        }
    }
}
