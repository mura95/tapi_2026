using Fusion;
using UnityEngine;
using UnityEngine.AI;
using TapHouse.MetaverseWalk.Core;
using TapHouse.MetaverseWalk.Dog;

namespace TapHouse.MetaverseWalk.Network
{
    /// <summary>
    /// ネットワーク同期対応の犬コントローラー
    /// ローカル: MetaverseDogFollowerで自動追従、NavMeshAgentの状態を[Networked]に書き込み
    /// リモート: [Networked]の値で位置補間・Animator適用
    /// </summary>
    public class NetworkDogController : NetworkBehaviour
    {
        [Header("アニメーション")]
        [SerializeField] private Animator animator;

        // ネットワーク同期プロパティ
        [Networked] public float NetworkedSpeed { get; set; }
        [Networked] public NetworkBool NetworkedIsWalking { get; set; }
        [Networked] public Quaternion NetworkedYaw { get; set; }
        [Networked] public Vector3 NetworkedPosition { get; set; }

        private MetaverseDogFollower localDogFollower;
        private NavMeshAgent agent;

        // Animatorパラメータハッシュ
        private static readonly int SpeedParam = Animator.StringToHash("DogWalkSpeed");
        private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");

        // デバッグ用
        [System.NonSerialized] public float DebugPositionError;
        private float debugLogTimer;

        /// <summary>
        /// ローカル犬の追従コンポーネント（NetworkSpawnManager用）
        /// </summary>
        public MetaverseDogFollower LocalDogFollower => localDogFollower;

        public override void Spawned()
        {
            var dogController = GetComponent<MetaverseDogController>();

            agent = GetComponent<NavMeshAgent>();

            Debug.Log($"[NetworkDogController] Spawned: HasStateAuthority={HasStateAuthority}, " +
                      $"dogController={dogController != null}, navAgent={agent != null}");

            if (HasStateAuthority)
            {
                // ローカル犬: DogControllerを無効化し、DogFollowerを追加
                if (dogController != null)
                {
                    dogController.enabled = false;
                    dogController.IsNetworkControlled = true;
                }

                // NavMeshAgentを有効化（DogFollowerの移動に必要）
                if (agent != null)
                {
                    agent.enabled = true;
                }

                // Rigidbodyをkinematicに（NavMeshAgentとの競合防止）
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // MetaverseDogFollowerを追加
                localDogFollower = gameObject.AddComponent<MetaverseDogFollower>();
               // localDogFollower.IsNetworkControlled = true;


                // Animatorを設定
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
                if (animator != null)
                {
                    localDogFollower.SetAnimator(animator);
                }

                // 初期位置をネットワークに書き込み
                NetworkedPosition = transform.position;
                //NetworkedYaw = transform.eulerAngles.y;
            }
            else
            {
                // リモート犬: ローカル入力・追従を無効化
                if (dogController != null)
                {
                    dogController.enabled = false;
                }

                // Rigidbodyがあれば無効化
                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                // NavMeshAgentを無効化（ネットワーク補間が位置を制御）
                if (agent != null)
                {
                    agent.enabled = false;
                    agent.updateRotation = false;
                }

                // リモート犬の初期位置をNetworked値に合わせる
                transform.position = NetworkedPosition;
                //transform.rotation = Quaternion.Euler(0f, NetworkedYaw, 0f);
            }

            // 犬のスケールを調整
            float s = MetaverseConstants.DOG_SCALE;
            transform.localScale = new Vector3(s, s, s);

            // Animatorの参照を確保
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            // NavMeshAgentのvelocityからアニメーション状態を書き込み
            if (agent != null && agent.enabled)
            {
                float speed = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
                NetworkedSpeed = speed;
                NetworkedIsWalking = agent.velocity.magnitude > 0.1f;
            }

            // 位置をネットワークに書き込み
            NetworkedPosition = transform.position;
            if (localDogFollower != null)
            {
                NetworkedYaw = Quaternion.Euler(0f, localDogFollower.CurrentYaw, 0f);
            }
        }

        public override void Render()
        {
            if (HasStateAuthority) return;
            // デバッグ: yawを1度単位で1秒ごとに出力
            /*debugLogTimer += Time.deltaTime;
            if (debugLogTimer >= 1f)
            {
                debugLogTimer = 0f;
                float actualYaw = Mathf.Round(transform.eulerAngles.y);
                float netYaw = Mathf.Round(NetworkedYaw);
                var vel = agent != null ? agent.velocity : Vector3.zero;
                Debug.Log($"[NetworkDogController] local={HasStateAuthority} actualYaw={actualYaw} netYaw={netYaw} " +
                          $"vel=({vel.x:F1},{vel.z:F1}) speed={NetworkedSpeed:F2}");
            }*/

            if (HasStateAuthority)
            {
                // ローカル犬: LateUpdateで確定した回転をネットワークに書き込み
                //NetworkedYaw = transform.eulerAngles.y;
                return;
            }

            // リモートの犬: 位置を補間で適用
            float lerpSpeed = NetworkConstants.POSITION_INTERPOLATION_SPEED * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, NetworkedPosition, lerpSpeed);

            // デバッグ用: 位置誤差の追跡
            DebugPositionError = Vector3.Distance(transform.position, NetworkedPosition);

            // 回転を補間で適用
           /*// Quaternion targetRot = Quaternion.Euler(0f, NetworkedYaw, 0f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, NetworkedYaw,
                NetworkConstants.ROTATION_INTERPOLATION_SPEED * Time.deltaTime);*/

            // アニメーション適用
            if (animator != null)
            {
                animator.SetFloat(SpeedParam, NetworkedSpeed);
                animator.SetBool(IsWalkingParam, NetworkedIsWalking);
            }
        }

        private void LateUpdate()
        {
            // State authority: handled by MetaverseDogFollower.LateUpdate()
            if (HasStateAuthority) return;

            // ✅ Remote: override Animator after it runs, just like local does
            transform.rotation = NetworkedYaw;
        }

    }

     
    }
