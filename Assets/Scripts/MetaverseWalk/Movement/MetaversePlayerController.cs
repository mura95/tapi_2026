using UnityEngine;
using UnityEngine.AI;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.Movement
{
    /// <summary>
    /// メタバース内でのプレイヤーの移動を制御
    /// ボタン入力を受けてプレイヤーを移動させる（操作対象）
    /// NavMeshAgentで壁や地形の制約を受ける
    /// MetaverseDogControllerと同一のPublic APIを持つ
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MetaversePlayerController : MonoBehaviour
    {
        [Header("移動設定")]
        [SerializeField] private float moveSpeed = MetaverseConstants.DEFAULT_PLAYER_CONTROL_SPEED;
        [SerializeField] private float turnAngleMin = 10f;
        [SerializeField] private float turnAngleMax = 80f;
        [SerializeField] private float turnAngleRampTime = 2f;

        [Header("スムージング")]
        [SerializeField] private float speedSmoothTime = 0.2f;
        [SerializeField] private float directionSmoothTime = 0.2f;

        [Header("落下リスポーン")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float fallThresholdY = -10f;

        [Header("アニメーション")]
        [SerializeField] private Animator animator;

        private NavMeshAgent agent;

        /// <summary>
        /// trueの場合、FixedUpdateNetworkから移動が呼ばれるためUpdate内では移動しない
        /// NetworkPlayerControllerが管理する場合にtrueにする
        /// </summary>
        public bool IsNetworkControlled { get; set; }

        private bool isMovingForward;
        private bool isTurning;
        private float targetDirection;
        private float speedParam;
        private float speedVelocity;
        private float currentDirection;
        private float directionVelocity;
        private float turnHoldTime;
        private float currentYaw;

        // アニメーションパラメータ（プレイヤー用: Speedのみ）
        private static readonly int SpeedAnimParam = Animator.StringToHash("Speed");

        public bool IsMoving => speedParam > 0.01f;
        public float CurrentSpeed => speedParam * moveSpeed;
        public float SpeedValue => speedParam;
        public float CurrentDirectionValue => currentDirection;
        public float CurrentYaw => currentYaw;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.updateRotation = false;
                agent.updatePosition = true;
                agent.angularSpeed = 0f;
                agent.speed = moveSpeed;
            }

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            currentYaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            if (transform.position.y < fallThresholdY)
            {
                RespawnAtSpawnPoint();
                return;
            }

            UpdateInputState();

            if (!IsNetworkControlled)
            {
                ApplyMovement(Time.deltaTime);
            }

            UpdateAnimation();
        }

        private void UpdateInputState()
        {
            bool wantsMove = isMovingForward || isTurning;
            float targetSpeed = wantsMove ? 1f : 0f;
            speedParam = Mathf.SmoothDamp(speedParam, targetSpeed, ref speedVelocity, speedSmoothTime);
            if (Mathf.Abs(speedParam - targetSpeed) < 0.05f)
            {
                speedParam = targetSpeed;
                speedVelocity = 0f;
            }

            currentDirection = Mathf.SmoothDamp(currentDirection, targetDirection, ref directionVelocity, directionSmoothTime);
            if (Mathf.Abs(currentDirection - targetDirection) < 0.05f)
            {
                currentDirection = targetDirection;
                directionVelocity = 0f;
            }

            if (isTurning)
            {
                turnHoldTime += Time.deltaTime;
            }
            else
            {
                turnHoldTime = 0f;
            }
        }

        /// <summary>
        /// 実際の移動・回転を適用する
        /// NetworkPlayerController.FixedUpdateNetwork() から呼ばれる（Fusionティック内で実行）
        /// シングルプレイヤー時はUpdate()から直接呼ばれる
        /// </summary>
        public void ApplyMovement(float deltaTime)
        {
            float turnT = Mathf.Clamp01(turnHoldTime / turnAngleRampTime);
            float turnAngle = Mathf.Lerp(turnAngleMin, turnAngleMax, turnT);

            if (Mathf.Abs(currentDirection) > 0.01f)
            {
                currentYaw += currentDirection * turnAngle * deltaTime;
            }

            Vector3 forward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;

            if (speedParam > 0.01f)
            {
                Vector3 moveVector = forward * (speedParam * moveSpeed) * deltaTime;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.Move(moveVector);
                }
                else
                {
                    transform.position += moveVector;
                }
            }
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            animator.SetFloat(SpeedAnimParam, speedParam);
        }

        #region Public Methods - ボタンから呼び出し

        public void StartMoveForward()
        {
            isMovingForward = true;
        }

        public void StopMoveForward()
        {
            isMovingForward = false;
        }

        public void StartTurnLeft()
        {
            targetDirection = -1f;
            isTurning = true;
        }

        public void StartTurnRight()
        {
            targetDirection = 1f;
            isTurning = true;
        }

        public void StopTurn()
        {
            targetDirection = 0f;
            isTurning = false;
        }

        #endregion

        public void SetInitialPosition(Vector3 position, Quaternion rotation)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
            transform.rotation = rotation;
            currentYaw = rotation.eulerAngles.y;
        }

        /// <summary>
        /// Animatorを外部から設定（AddComponent時用）
        /// </summary>
        public void SetAnimator(Animator anim)
        {
            animator = anim;
        }

        private void RespawnAtSpawnPoint()
        {
            Vector3 targetPos;
            Quaternion targetRot;

            if (spawnPoint != null)
            {
                targetPos = spawnPoint.position;
                targetRot = spawnPoint.rotation;
            }
            else
            {
                targetPos = Vector3.zero;
                targetRot = Quaternion.identity;
            }

            if (agent != null && NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                transform.position = targetPos;
            }
            transform.rotation = targetRot;
            currentYaw = targetRot.eulerAngles.y;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
