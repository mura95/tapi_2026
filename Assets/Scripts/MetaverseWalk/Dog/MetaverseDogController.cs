using UnityEngine;
using UnityEngine.AI;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.Dog
{
    /// <summary>
    /// メタバース内での犬の移動を制御
    /// ボタン入力を受けて犬を移動させる（先導役）
    /// NavMeshAgentで壁や地形の制約を受ける
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MetaverseDogController : MonoBehaviour
    {
        [Header("移動設定")]
        [SerializeField] private float moveSpeed = MetaverseConstants.DEFAULT_DOG_SPEED;
        [SerializeField] private float turnAngleMin = 10f;  // 左右キー押し始めの角度
        [SerializeField] private float turnAngleMax = 80f;  // 長押し時の最大角度
        [SerializeField] private float turnAngleRampTime = 2f; // 最小→最大に到達する秒数

        [Header("スムージング")]
        [SerializeField] private float speedSmoothTime = 0.2f; // 速度変化のスムージング秒数
        [SerializeField] private float directionSmoothTime = 0.2f; // 方向転換のスムージング秒数

        [Header("落下リスポーン")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float fallThresholdY = -10f;

        [Header("アニメーション")]
        [SerializeField] private Animator animator;

        private NavMeshAgent agent;

        /// <summary>
        /// trueの場合、FixedUpdateNetworkから移動が呼ばれるためUpdate内では移動しない
        /// NetworkDogControllerが管理する場合にtrueにする
        /// </summary>
        public bool IsNetworkControlled { get; set; }

        private bool isMovingForward;
        private bool isTurning;
        private float targetDirection; // -1: 左, 0: 直進, 1: 右
        private float speedParam;     // 0〜1（animatorにそのまま渡す）
        private float speedVelocity;  // SmoothDamp用
        private float currentDirection;
        private float directionVelocity; // SmoothDamp用
        private float turnHoldTime;   // 左右ボタン長押し経過時間
        private float currentYaw;     // 現在のY軸回転角度（絶対値で管理）

        // アニメーションパラメータ
        private static readonly int SpeedParam = Animator.StringToHash("DogWalkSpeed");
        private static readonly int DirectionParam = Animator.StringToHash("Direction");
        private static readonly int IsWalkingParam = Animator.StringToHash("IsWalking");

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
                // 手動で移動制御するため、自動回転・自動パス追従を無効化
                agent.updateRotation = false;
                agent.updatePosition = true;
                agent.angularSpeed = 0f;
                agent.speed = moveSpeed;
            }

            // NavMeshAgentとRigidbodyが競合しないようにする
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // 初期回転角度を記録
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

            // シングルプレイヤー時はUpdate内で移動を実行
            if (!IsNetworkControlled)
            {
                ApplyMovement(Time.deltaTime);
            }

            UpdateAnimation();
        }

        /// <summary>
        /// 入力状態の計算（速度・方向のスムージング）
        /// Update()から毎フレーム呼ばれる
        /// </summary>
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

            // 方向のスムージング（-1/0/1に瞬時ではなく滑らかに変化）
            currentDirection = Mathf.SmoothDamp(currentDirection, targetDirection, ref directionVelocity, directionSmoothTime);
            if (Mathf.Abs(currentDirection - targetDirection) < 0.05f)
            {
                currentDirection = targetDirection;
                directionVelocity = 0f;
            }

            // 長押し時間に応じて回転角度を増加
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
        /// NetworkDogController.FixedUpdateNetwork() から呼ばれる（Fusionティック内で実行）
        /// シングルプレイヤー時はUpdate()から直接呼ばれる
        /// </summary>
        public void ApplyMovement(float deltaTime)
        {
            float turnT = Mathf.Clamp01(turnHoldTime / turnAngleRampTime);
            float turnAngle = Mathf.Lerp(turnAngleMin, turnAngleMax, turnT);

            // 回転処理（絶対角度で管理）
            if (Mathf.Abs(currentDirection) > 0.01f)
            {
                currentYaw += currentDirection * turnAngle * deltaTime;
            }

            // currentYawから移動方向を計算（transform.forwardに依存しない）
            Vector3 forward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;

            // 前進処理
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

            bool wantsMove = isMovingForward || isTurning;
            animator.SetBool(IsWalkingParam, wantsMove || speedParam > 0.01f);
            animator.SetFloat(SpeedParam, speedParam);
            animator.SetFloat(DirectionParam, currentDirection);

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

        /// <summary>
        /// 初期位置に配置
        /// </summary>
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

            // NavMeshにスナップしてからWarp
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

            // Rigidbodyの速度もリセット
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
