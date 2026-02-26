using UnityEngine;
using UnityEngine.AI;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.Dog
{
    /// <summary>
    /// 犬がプレイヤーの前方を自動追従する
    /// NavMeshAgentでパス追従、回転はLateUpdateで手動制御（Animator後に実行）
    ///
    /// 【回転の設計】
    /// Animatorが毎フレームtransform.rotationを上書きするため、
    /// transform.rotationを起点としたSlerpは永遠に収束しない。
    /// 独立したfloat(desiredYaw)で回転を管理し、LateUpdateで直接上書きする。
    /// target.rotationもUpdate時（Animator評価前）にキャッシュする。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MetaverseDogFollower : MonoBehaviour
    {
        public float CurrentYaw => desiredYaw;
        [Header("追従対象")]
        [SerializeField] private Transform target;

        [Header("追従設定")]
        [SerializeField] private float followDistance = MetaverseConstants.DEFAULT_DOG_FOLLOW_DISTANCE;
        [SerializeField] private float stopDistance = MetaverseConstants.DEFAULT_DOG_STOP_DISTANCE;
        [SerializeField] private float normalSpeed = MetaverseConstants.DEFAULT_DOG_FOLLOWER_SPEED;
        [SerializeField] private float catchUpSpeed = MetaverseConstants.DEFAULT_DOG_CATCHUP_SPEED;
        [SerializeField] private float maxDistanceBeforeTeleport = 8f;

        [Header("回転設定")]
        [SerializeField] private float rotationSpeed = 10f;

        [Header("アニメーション")]
        [SerializeField] private Animator animator;

        private NavMeshAgent agent;
        private bool followEnabled = true;

        // 目的地の更新を間引くための閾値（毎フレームSetDestinationを防ぐ）
        private Vector3 lastSetDestination;
        private const float DEST_UPDATE_THRESHOLD = 0.5f;

        // 回転管理（Animatorに依存しない独立した値）
        private float desiredYaw;
        private float cachedTargetYaw;

        // Animatorパラメータ（犬用）
        private static readonly int SpeedParam = Animator.StringToHash("DogWalkSpeed");
        private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");

        public float CurrentSpeed => agent != null ? agent.velocity.magnitude : 0f;
        public bool IsWalking => agent != null && agent.velocity.magnitude > 0.1f;
        public float DistanceToTarget => target != null ? Vector3.Distance(transform.position, target.position) : 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            SetupAgent();
            desiredYaw = transform.eulerAngles.y;
        }

        private void SetupAgent()
        {
            if (agent == null) return;

            agent.speed = normalSpeed;
            agent.angularSpeed = 0f;
            agent.acceleration = 10f;
            agent.stoppingDistance = stopDistance;
            agent.autoBraking = true;
            agent.updateRotation = false;
        }

        private void Update()
        {
            if (!followEnabled || target == null) return;

            // Animator評価前にプレイヤーのyawをキャッシュ
            // （前フレームのLateUpdateで設定された正しい値がまだ残っている）
            cachedTargetYaw = target.eulerAngles.y;

            FollowTarget();
            UpdateAnimation();
        }

        /// <summary>
        /// LateUpdateで回転を適用（Animator評価後なので上書きされない）
        /// </summary>
        private void LateUpdate()
        {
            if (!followEnabled || target == null) return;

            UpdateRotation();
        }

        private void FollowTarget()
        {
            float distToTarget = Vector3.Distance(transform.position, target.position);

            if (distToTarget > maxDistanceBeforeTeleport)
            {
                TeleportToFollowPosition();
                return;
            }

            Vector3 followPos = GetFollowPosition();
            float distToFollow = Vector3.Distance(transform.position, followPos);

            if (distToFollow > stopDistance)
            {
                if (Vector3.Distance(followPos, lastSetDestination) > DEST_UPDATE_THRESHOLD)
                {
                    if (NavMesh.SamplePosition(followPos, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(navHit.position);
                        lastSetDestination = navHit.position;
                    }
                }

                agent.speed = distToTarget > followDistance * 2 ? catchUpSpeed : normalSpeed;
            }
            else
            {
                if (agent.hasPath)
                {
                    agent.ResetPath();
                    lastSetDestination = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 回転を手動で制御（LateUpdateから呼ばれる）
        /// desiredYawを独立管理し、transform.rotationを読まない（Animator干渉を完全排除）
        /// </summary>
        private void UpdateRotation()
        {
            float targetYaw;

            if (agent.velocity.sqrMagnitude > 0.25f)
            {
                // 移動中: velocity方向を向く
                Vector3 velFlat = new Vector3(agent.velocity.x, 0f, agent.velocity.z);
                if (velFlat.sqrMagnitude > 0.01f)
                {
                    targetYaw = Mathf.Atan2(velFlat.x, velFlat.z) * Mathf.Rad2Deg;
                }
                else
                {
                    return;
                }
            }
            else
            {
                // 停止中: プレイヤーと同じ方向を向く（Update時にキャッシュした値を使用）
                targetYaw = cachedTargetYaw;
            }

            // 独立したfloatで補間（Animatorがtransform.rotationを書き換えても影響なし）
            desiredYaw = Mathf.LerpAngle(desiredYaw, targetYaw, rotationSpeed * Time.deltaTime);

            // Animatorが書いた値を上書き
            transform.rotation = Quaternion.Euler(0f, desiredYaw, 0f);
        }

        private Vector3 GetFollowPosition()
        {
            return target.position + target.forward * followDistance;
        }

        private void TeleportToFollowPosition()
        {
            Vector3 teleportPosition = GetFollowPosition();

            if (NavMesh.SamplePosition(teleportPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
                desiredYaw = cachedTargetYaw;
                transform.rotation = Quaternion.Euler(0f, desiredYaw, 0f);
                lastSetDestination = Vector3.zero;
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            float speed = agent.speed > 0.01f
                ? Mathf.Clamp01(agent.velocity.magnitude / agent.speed)
                : 0f;
            animator.SetFloat(SpeedParam, speed);
            animator.SetBool(IsWalkingParam, agent.velocity.magnitude > 0.1f);
        }

        public void SetFollowEnabled(bool enabled)
        {
            followEnabled = enabled;
            if (agent != null)
            {
                agent.enabled = enabled;
            }
        }

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }

        public void SetAnimator(Animator anim)
        {
            animator = anim;
        }

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
            desiredYaw = rotation.eulerAngles.y;
            transform.rotation = rotation;
            lastSetDestination = Vector3.zero;
        }
    }
}
