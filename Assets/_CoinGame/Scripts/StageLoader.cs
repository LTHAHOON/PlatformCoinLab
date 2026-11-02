using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CoinGame
{
    public class StageLoader : MonoBehaviour
    {
        [SerializeField]
        private string address = "coin_Spawner";

        private AsyncOperationHandle<GameObject> handle;
        private bool ownerDestroyed;

        private void Start()
        {
            LoadStage();
        }

        private void LoadStage()
        {
            handle = Addressables.InstantiateAsync(address, Vector3.zero, Quaternion.identity);
            handle.Completed += OnStageLoaded;
        }

        private void OnStageLoaded(AsyncOperationHandle<GameObject> operation)
        {
            if (ownerDestroyed)
            {
                ReleaseOwnedOperation();
                return;
            }

            if (operation.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("Failed to load stage: " + operation.OperationException);
                return;
            }

            GameObject stagePrefab = operation.Result;
            if (stagePrefab != null)
            {
                Debug.Log("Stage prefab loaded successfully.");
            }

        }

        private void OnDestroy()
        {
            ownerDestroyed = true;

            // 진행 중인 요청은 여기서 취소되지 않습니다. 완료된 요청만 지금 정리하고,
            // 아직 진행 중이면 위 OnCompleted의 ownerDestroyed 분기에서 나중에 정리합니다.
            if (handle.IsValid() && handle.IsDone)
            {
                ReleaseOwnedOperation();
            }
        }

        private void ReleaseOwnedOperation()
        {

            if (!handle.IsValid())
            {
                return;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 성공한 InstantiateAsync 요청은 생성된 인스턴스와 참조를 함께 해제합니다.
                Addressables.ReleaseInstance(handle);
            }
            else
            {
                // 실패한 요청은 인스턴스가 없지만, 요청 핸들이 가진 참조는 반납해야 합니다.
                Addressables.Release(handle);
            }

            handle = default;   
        }
    }
}
