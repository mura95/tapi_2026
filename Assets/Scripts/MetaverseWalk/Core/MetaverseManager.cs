using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;
using TapHouse.MetaverseWalk.Dog;
using TapHouse.MetaverseWalk.Movement;
using TapHouse.MetaverseWalk.Network;
using TapHouse.MetaverseWalk.Camera;
using TapHouse.MetaverseWalk.UI;

namespace TapHouse.MetaverseWalk.Core
{
    /// <summary>
    /// メタバースシーンの管理を行うマネージャークラス
    /// シーンの初期化、状態管理、終了処理を担当
    /// </summary>
    public class MetaverseManager : MonoBehaviour
    {
        [Header("シーン設定")]
        [SerializeField] private string mainSceneName = "main";

        [Header("スポーン設定")]
        [SerializeField] private Transform dogSpawnPoint;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("シングルプレイヤー用Prefab")]
        [SerializeField] private GameObject localDogPrefab;
        [SerializeField] private GameObject localPlayerPrefab;

        [Header("マルチプレイヤー")]
        [SerializeField] private bool enableMultiplayer = true;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        public static MetaverseManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        private CancellationTokenSource initCts;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // エラーハンドラーを購読（AutoReturn=trueのエラーでメインシーンに戻る）
            MetaverseErrorHandler.OnError += HandleMetaverseError;

            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            Debug.Log("[MetaverseManager] Initializing...");
            initCts = new CancellationTokenSource();

            if (enableMultiplayer)
            {
                Debug.Log("[MetaverseManager] Connecting to Photon (multiplayer mode)...");

                var networkManager = PhotonNetworkManager.Instance;
                if (networkManager != null)
                {
                    bool connected = await networkManager.ConnectWithRetryAsync(ct: initCts.Token);
                    if (!connected)
                    {
                        Debug.LogWarning("[MetaverseManager] Failed to connect to Photon, falling back to single player");
                        SpawnLocalEntities();
                    }
                }
                else
                {
                    Debug.LogWarning("[MetaverseManager] PhotonNetworkManager not found in scene, falling back to single player");
                    SpawnLocalEntities();
                }
            }
            else
            {
                Debug.Log("[MetaverseManager] Single player mode: spawning local entities...");
                SpawnLocalEntities();
                await UniTask.Yield();
            }

            IsInitialized = true;
            Debug.Log("[MetaverseManager] Initialized successfully");
        }

        /// <summary>
        /// エラーハンドラーからの通知を受けて処理
        /// AutoReturn=trueのエラー → メインシーンへ戻る
        /// </summary>
        private void HandleMetaverseError(MetaverseError error)
        {
            Debug.LogWarning($"[MetaverseManager] Error received: {error.Code} - AutoReturn={error.AutoReturn}");

            if (error.AutoReturn)
            {
                // 少し待ってからメインシーンへ（ユーザーがメッセージを読む時間）
                ReturnToMainAfterDelay(2f).Forget();
            }
        }

        private async UniTaskVoid ReturnToMainAfterDelay(float delaySec)
        {
            await UniTask.Delay((int)(delaySec * 1000));
            ExitToMainScene();
        }

        /// <summary>
        /// シングルプレイヤー用: ローカルエンティティをスポーン
        /// NetworkSpawnManager.SetupLocalEntitiesのパターンを踏襲
        /// </summary>
        private void SpawnLocalEntities()
        {
            if (localDogPrefab == null || localPlayerPrefab == null)
            {
                Debug.LogError("[MetaverseManager] localDogPrefab or localPlayerPrefab is not assigned");
                return;
            }

            // スポーン位置の取得（人が基準、犬が前方）
            Vector3 playerPos = GetPlayerSpawnPosition();
            Quaternion playerRot = GetPlayerSpawnRotation();

            // NavMeshにスナップ（地下スポーン防止）
            if (NavMesh.SamplePosition(playerPos, out NavMeshHit playerNavHit, 5f, NavMesh.AllAreas))
            {
                playerPos = playerNavHit.position;
            }

            Vector3 dogPos = playerPos + playerRot * Vector3.forward * MetaverseConstants.SPAWN_OFFSET_FORWARD;
            Quaternion dogRot = playerRot;

            if (NavMesh.SamplePosition(dogPos, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                dogPos = navHit.position;
            }

            // Prefabのインスタンス化
            GameObject playerObj = Instantiate(localPlayerPrefab, playerPos, playerRot);
            GameObject dogObj = Instantiate(localDogPrefab, dogPos, dogRot);

            // 犬のスケールを調整
            float dogScale = MetaverseConstants.DOG_SCALE;
            dogObj.transform.localScale = new Vector3(dogScale, dogScale, dogScale);

            // 1. 犬のDogFollowerにプレイヤーを追従対象として設定
            var dogFollower = dogObj.GetComponent<MetaverseDogFollower>();
            if (dogFollower == null)
            {
                dogFollower = dogObj.AddComponent<MetaverseDogFollower>();
            }
            dogFollower.SetTarget(playerObj.transform);
            var dogAnimator = dogObj.GetComponentInChildren<Animator>();
            if (dogAnimator != null)
            {
                dogFollower.SetAnimator(dogAnimator);
            }

            // DogControllerを無効化（追従モードのため不要）
            var dogController = dogObj.GetComponent<MetaverseDogController>();
            if (dogController != null)
            {
                dogController.enabled = false;
            }

            // PlayerFollowerを無効化（直接操作のため不要）
            var playerFollower = playerObj.GetComponent<MetaversePlayerFollower>();
            if (playerFollower != null)
            {
                playerFollower.enabled = false;
            }

            // 2. カメラのターゲットを設定
            var metaverseCamera = FindFirstObjectByType<MetaverseCamera>();
            if (metaverseCamera != null)
            {
                metaverseCamera.SetTargets(dogObj.transform, playerObj.transform);
                metaverseCamera.SnapToTarget();
            }

            // 3. 移動ボタンUIにプレイヤーコントローラーを設定
            var playerController = playerObj.GetComponent<MetaversePlayerController>();
            if (playerController == null)
            {
                playerController = playerObj.AddComponent<MetaversePlayerController>();
                var playerAnimator = playerObj.GetComponentInChildren<Animator>();
                if (playerAnimator != null)
                {
                    playerController.SetAnimator(playerAnimator);
                }
            }
            var movementUI = FindFirstObjectByType<MovementButtonUI>();
            if (movementUI != null)
            {
                movementUI.SetPlayerController(playerController);
            }

            if (debugMode)
            {
                Debug.Log($"[MetaverseManager] Local entities spawned at player={playerPos}, dog={dogPos}");
            }
        }

        /// <summary>
        /// 犬のスポーン位置を取得
        /// </summary>
        public Vector3 GetDogSpawnPosition()
        {
            if (dogSpawnPoint != null)
            {
                return dogSpawnPoint.position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 犬のスポーン回転を取得
        /// </summary>
        public Quaternion GetDogSpawnRotation()
        {
            if (dogSpawnPoint != null)
            {
                return dogSpawnPoint.rotation;
            }
            return Quaternion.identity;
        }

        /// <summary>
        /// プレイヤーのスポーン位置を取得（基準位置）
        /// </summary>
        public Vector3 GetPlayerSpawnPosition()
        {
            if (playerSpawnPoint != null)
            {
                return playerSpawnPoint.position;
            }
            // 犬のスポーンポイントをプレイヤーの基準位置として使用
            return GetDogSpawnPosition();
        }

        /// <summary>
        /// プレイヤーのスポーン回転を取得
        /// </summary>
        public Quaternion GetPlayerSpawnRotation()
        {
            if (playerSpawnPoint != null)
            {
                return playerSpawnPoint.rotation;
            }
            return GetDogSpawnRotation();
        }

        /// <summary>
        /// 散歩を終了してメインシーンに戻る
        /// </summary>
        public void ExitToMainScene()
        {
            ExitToMainSceneAsync().Forget();
        }

        private async UniTaskVoid ExitToMainSceneAsync()
        {
            Debug.Log("[MetaverseManager] Exiting to main scene...");

            // フェードアウト
            var fade = SceneFadeOverlay.Instance;
            if (fade != null)
            {
                await fade.FadeOut(0.5f);
            }

            // Photon切断
            var networkManager = PhotonNetworkManager.Instance;
            if (networkManager != null && networkManager.IsConnected)
            {
                networkManager.Disconnect();
                // 切断完了を待つ（最大1秒）
                float waitStart = Time.realtimeSinceStartup;
                while (networkManager.IsConnected && Time.realtimeSinceStartup - waitStart < 1f)
                {
                    await UniTask.Yield();
                }
            }

            // PetStateをidleに戻す
            GlobalVariables.CurrentState = PetState.idle;

            // エラー状態をクリア
            MetaverseErrorHandler.ClearError();

            // シーン遷移
            await SceneManager.LoadSceneAsync(mainSceneName);

            // フェードイン
            if (fade != null)
            {
                await fade.FadeIn(0.5f);
            }
        }

        private void OnDestroy()
        {
            MetaverseErrorHandler.OnError -= HandleMetaverseError;
            initCts?.Cancel();
            initCts?.Dispose();

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
