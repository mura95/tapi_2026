using UnityEngine;
using UnityEngine.AI;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.Movement
{
    /// <summary>
    /// プレイヤーの追従を制御
    /// 犬の後ろを自動で追従する（追従役）
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MetaversePlayerFollower : MonoBehaviour
    {
        [Header("追従対象")]
        [SerializeField] private Transform dog;

        [Header("追従設定")]
        [SerializeField] private float followDistance = MetaverseConstants.DEFAULT_FOLLOW_DISTANCE;
        [SerializeField] private float stopDistance = MetaverseConstants.DEFAULT_STOP_DISTANCE;
        [SerializeField] private float normalSpeed = MetaverseConstants.DEFAULT_PLAYER_SPEED;
        [SerializeField] private float catchUpSpeed = 4.5f;
        [SerializeField] private float maxDistanceBeforeTeleport = 8f;

        [Header("アニメーション")]
        [SerializeField] private Animator animator;

        private NavMeshAgent agent;
        private bool followEnabled = true;

        // アニメーションパラメータ
        private static readonly int SpeedParam = Animator.StringToHash("Speed");

        public float CurrentSpeed => agent != null ? agent.velocity.magnitude : 0f;
        public float DistanceToDog => dog != null ? Vector3.Distance(transform.position, dog.position) : 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            SetupAgent();
        }

        private void SetupAgent()
        {
            if (agent != null)
            {
                agent.speed = normalSpeed;
                agent.angularSpeed = 360f;
                agent.acceleration = 10f;
                agent.stoppingDistance = stopDistance;
                agent.autoBraking = true;
            }
        }

        private void Update()
        {
            if (!followEnabled || dog == null) return;

            FollowDog();
            UpdateAnimation();
        }

        private void FollowDog()
        {
            float distanceToDog = Vector3.Distance(transform.position, dog.position);

            // 距離が離れすぎたらテレポート
            if (distanceToDog > maxDistanceBeforeTeleport)
            {
                TeleportBehindDog();
                return;
            }

            if (distanceToDog > followDistance)
            {
                // 犬の後方位置を計算
                Vector3 followPosition = GetFollowPosition();

                // NavMeshで有効な位置にサンプリング
                if (NavMesh.SamplePosition(followPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                }

                // 距離に応じて速度調整（離れすぎたら追いつく）
                agent.speed = distanceToDog > followDistance * 2 ? catchUpSpeed : normalSpeed;
            }
            else if (distanceToDog < stopDistance)
            {
                // 犬に近すぎたら停止
                if (agent.hasPath)
                {
                    agent.ResetPath();
                }
            }
        }

        private Vector3 GetFollowPosition()
        {
            // 犬の後方位置（リードを持っているイメージ）
            Vector3 offset = -dog.forward * followDistance;
            return dog.position + offset;
        }

        private void TeleportBehindDog()
        {
            Vector3 teleportPosition = GetFollowPosition();

            if (NavMesh.SamplePosition(teleportPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
                transform.rotation = dog.rotation;
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            float speed = Mathf.Clamp01(agent.velocity.magnitude / agent.speed);
            animator.SetFloat(SpeedParam, speed);
        }

        /// <summary>
        /// 追従の有効/無効を切り替え
        /// リモートプレイヤーではNetworkTransformが位置を制御するため無効化する
        /// </summary>
        public void SetFollowEnabled(bool enabled)
        {
            followEnabled = enabled;
            if (agent != null)
            {
                agent.enabled = enabled;
            }
        }

        /// <summary>
        /// 追従対象の犬を設定
        /// </summary>
        public void SetDog(Transform dogTransform)
        {
            dog = dogTransform;
        }

        /// <summary>
        /// 初期位置に配置
        /// </summary>
        public void SetInitialPosition(Vector3 position, Quaternion rotation)
        {
            if (agent != null)
            {
                agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
            transform.rotation = rotation;
        }
    }
}
