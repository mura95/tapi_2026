using Fusion;
using UnityEngine;
using UnityEngine.AI;
using TapHouse.MetaverseWalk.Movement;

namespace TapHouse.MetaverseWalk.Network
{
    /// <summary>
    /// ネットワーク同期対応のプレイヤーコントローラー
    /// ローカル: MetaversePlayerControllerで直接操作、状態を[Networked]に書き込み
    /// リモート: [Networked]の値で位置補間・Animator適用
    /// </summary>
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("アニメーション")]
        [SerializeField] private Animator animator;

        [Header("名前タグ")]
        [SerializeField] private PlayerNameTag nameTag;

        // ネットワーク同期プロパティ
        [Networked] public NetworkString<_32> PlayerName { get; set; }
        [Networked] public float AnimSpeed { get; set; }
        [Networked] public Vector3 NetworkedPosition { get; set; }
        [Networked] public float NetworkedYaw { get; set; }

        private MetaversePlayerController localController;
        private NavMeshAgent agent;

        // Animatorパラメータハッシュ
        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        // デバッグ用: 位置同期のズレを追跡
        [System.NonSerialized] public float DebugPositionError;

        /// <summary>
        /// ローカルプレイヤーのコントローラー（NetworkSpawnManager用）
        /// </summary>
        public MetaversePlayerController LocalPlayerController => localController;

        public override void Spawned()
        {
            var follower = GetComponent<MetaversePlayerFollower>();
            agent = GetComponent<NavMeshAgent>();

            if (HasStateAuthority)
            {
                // ローカルプレイヤー: Followerを無効化し、直接操作コントローラーを追加
                if (follower != null)
                {
                    follower.SetFollowEnabled(false);
                    follower.enabled = false;
                }

                // MetaversePlayerControllerを追加
                localController = gameObject.AddComponent<MetaversePlayerController>();
                localController.IsNetworkControlled = true;

                // Animatorを設定
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
                if (animator != null)
                {
                    localController.SetAnimator(animator);
                }

                // 名前を設定
                string displayName = PlayerPrefs.GetString(PrefsKeys.DisplayName, "Guest");
                if (displayName.Length > NetworkConstants.MAX_PLAYER_NAME_LENGTH)
                {
                    displayName = displayName.Substring(0, NetworkConstants.MAX_PLAYER_NAME_LENGTH);
                }
                PlayerName = displayName;

                // 初期位置をネットワークに書き込み
                NetworkedPosition = transform.position;
                NetworkedYaw = transform.eulerAngles.y;

                // 名前タグはローカルプレイヤーでは非表示
                if (nameTag != null)
                {
                    nameTag.Initialize(isLocal: true);
                }
            }
            else
            {
                // リモートプレイヤー: NavMesh追従を無効化（ネットワーク補間が位置を制御）
                if (follower != null)
                {
                    follower.SetFollowEnabled(false);
                    follower.enabled = false;
                }

                if (agent != null)
                {
                    agent.enabled = false;
                }

                // 名前タグを有効化
                if (nameTag != null)
                {
                    nameTag.Initialize(isLocal: false);
                    nameTag.SetName(PlayerName.ToString());
                }

                // リモートプレイヤーの初期位置をNetworked値に合わせる
                transform.position = NetworkedPosition;
                transform.rotation = Quaternion.Euler(0f, NetworkedYaw, 0f);
            }

            // Animatorの参照を確保
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            if (localController != null)
            {
                // Fusionティック内で移動を実行
                localController.ApplyMovement(Runner.DeltaTime);

                // ローカルの状態をネットワークに書き込み
                AnimSpeed = localController.SpeedValue;
                NetworkedYaw = localController.CurrentYaw;
                NetworkedPosition = transform.position;
            }
        }

        public override void Render()
        {
            if (HasStateAuthority) return;

            // リモートプレイヤー: 位置を補間で適用
            float lerpSpeed = NetworkConstants.POSITION_INTERPOLATION_SPEED * Time.deltaTime;
            Vector3 targetPos = NetworkedPosition;
            transform.position = Vector3.Lerp(transform.position, targetPos, lerpSpeed);

            Quaternion targetRot = Quaternion.Euler(0f, NetworkedYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                NetworkConstants.ROTATION_INTERPOLATION_SPEED * Time.deltaTime);

            // デバッグ用: 位置誤差の追跡
            DebugPositionError = Vector3.Distance(transform.position, targetPos);

            // アニメーション速度を適用
            if (animator != null)
            {
                animator.SetFloat(SpeedParam, AnimSpeed);
            }

            // 名前の更新（変更があった場合）
            if (nameTag != null)
            {
                nameTag.SetName(PlayerName.ToString());
            }
        }

        /// <summary>
        /// ローカル操作の即応性のためLateUpdateで回転を即座に適用
        /// </summary>
        private void LateUpdate()
        {
            if (HasStateAuthority && localController != null)
            {
                transform.rotation = Quaternion.Euler(0f, localController.CurrentYaw, 0f);
            }
        }
    }
}
