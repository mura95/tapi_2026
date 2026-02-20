using Fusion;
using UnityEngine;
using TapHouse.MetaverseWalk.Movement;
using TapHouse.MetaverseWalk.UI;

namespace TapHouse.MetaverseWalk.Network
{
    /// <summary>
    /// ローカルプレイヤーのSpawn後の初期設定を担当
    /// 犬・プレイヤー・カメラ・UIの相互参照を設定する
    /// </summary>
    public class NetworkSpawnManager : MonoBehaviour
    {
        [Header("UI参照")]
        [SerializeField] private MovementButtonUI movementButtonUI;

        [Header("カメラ参照")]
        [SerializeField] private MetaverseWalk.Camera.MetaverseCamera metaverseCamera;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private NetworkObject localDog;
        private NetworkObject localPlayer;

        /// <summary>
        /// ローカルプレイヤーのエンティティを初期設定
        /// PhotonNetworkManagerのOnPlayerJoinedから呼ばれる
        /// </summary>
        public void SetupLocalEntities(NetworkObject dogObj, NetworkObject playerObj)
        {
            localDog = dogObj;
            localPlayer = playerObj;

            if (localDog == null || localPlayer == null)
            {
                Debug.LogError("[NetworkSpawnManager] Dog or Player object is null");
                return;
            }

            // 1. 犬のDogFollowerにプレイヤーTransformを追従対象として設定
            var dogNetCtrl = localDog.GetComponent<NetworkDogController>();
            if (dogNetCtrl != null && dogNetCtrl.LocalDogFollower != null)
            {
                dogNetCtrl.LocalDogFollower.SetTarget(localPlayer.transform);
                Log("Dog follower linked to player");
            }
            else
            {
                Debug.LogWarning("[NetworkSpawnManager] DogFollower not found on dog object, will retry...");
            }

            // 2. カメラのターゲットを設定（犬＋プレイヤー、変更なし）
            if (metaverseCamera != null)
            {
                metaverseCamera.SetTargets(localDog.transform, localPlayer.transform);
                metaverseCamera.SnapToTarget();
                Log("Camera targets set");
            }
            else
            {
                // シーン内から検索
                var camera = FindFirstObjectByType<MetaverseWalk.Camera.MetaverseCamera>();
                if (camera != null)
                {
                    camera.SetTargets(localDog.transform, localPlayer.transform);
                    camera.SnapToTarget();
                    Log("Camera targets set (found in scene)");
                }
            }

            // 3. 移動ボタンUIにプレイヤーコントローラーを設定
            var playerNetCtrl = localPlayer.GetComponent<NetworkPlayerController>();
            MetaversePlayerController playerController = playerNetCtrl != null
                ? playerNetCtrl.LocalPlayerController
                : null;

            if (playerController != null)
            {
                if (movementButtonUI != null)
                {
                    movementButtonUI.SetPlayerController(playerController);
                    Debug.Log("[NetworkSpawnManager] Movement UI linked to player controller (serialized ref)");
                }
                else
                {
                    // シーン内から検索
                    var buttonUI = FindFirstObjectByType<MovementButtonUI>();
                    if (buttonUI != null)
                    {
                        buttonUI.SetPlayerController(playerController);
                        Debug.Log("[NetworkSpawnManager] Movement UI linked to player controller (found in scene)");
                    }
                    else
                    {
                        Debug.LogError("[NetworkSpawnManager] MovementButtonUI not found in scene! Player will not respond to UI.");
                    }
                }
            }
            else
            {
                Debug.LogError("[NetworkSpawnManager] MetaversePlayerController not found on player object!");
            }

            Debug.Log("[NetworkSpawnManager] Local entities setup complete");
        }

        /// <summary>
        /// ローカルエンティティのクリーンアップ
        /// </summary>
        public void CleanupLocalEntities()
        {
            localDog = null;
            localPlayer = null;
            Log("Local entities cleaned up");
        }

        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[NetworkSpawnManager] {message}");
            }
        }
    }
}
