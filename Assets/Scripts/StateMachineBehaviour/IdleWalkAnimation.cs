using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

public class IdleWalkAnimation : StateMachineBehaviour
{
    private enum State
    {
        RotatingToTarget,
        MovingToTarget,
        FinalizingRotation,
        Idle
    }

    private State _currentState = State.Idle;

    private Vector3 targetPosition;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private Quaternion finalRotationStart;
    private Quaternion finalRotationEnd = Quaternion.Euler(0, 0, 0);

    private float rotationStartTime;
    private float finalRotationStartTime;

    private Camera mainCamera;
    private MainUIButtons _mainUIButtons;

    [SerializeField] private Vector3 minPosition = new Vector3(-0.3f, 0f, -4.0f);
    [SerializeField] private Vector3 maxPosition = new Vector3(0.3f, 0f, 0.5f);

    [SerializeField] private float minDistance = 2.0f;
    [SerializeField] private int retryCount = 5;

    [Header("速度/回転")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float acceleration = 0.5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float targetProximity = 0.3f;

    [SerializeField] private float animationSmoothing = 5.0f;
    [SerializeField] private float rotationThreshold = 0.3f;
    [SerializeField] private float smoothingSpeedX = 60f;
    [SerializeField] private float smoothingSpeedY = 60f;

    [Header("タイムアウト")]
    [SerializeField] private float timeoutSeconds = 30f;

    [Header("デバッグ")]
    [SerializeField] private bool enableDebugLog = false;

    private float _currentSpeed = 0f;
    private float _stateStartTime = 0f;

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            GameLogger.Log(LogCategory.Animation,$"[IdleWalkAnimation] {message}");
        }
    }

    private void Awake()
    {
        if (_mainUIButtons == null)
        {
            _mainUIButtons = FindObjectOfType<MainUIButtons>();
        }
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        DebugLog("=== WalkingOnStateEnter ===");

        // sleep状態の場合は処理をスキップ
        if (GlobalVariables.CurrentState == PetState.sleeping)
        {
            DebugLog("Pet is sleeping, skipping idle walk");
            animator.SetInteger("TransitionNo", 1);
            return;
        }

        // reminder状態の場合は処理をスキップ
        if (GlobalVariables.CurrentState == PetState.reminder)
        {
            DebugLog("Pet is in reminder mode, skipping idle walk");
            return;
        }

        _currentState = State.Idle;
        _stateStartTime = Time.time;

        if (_mainUIButtons != null)
        {
            _mainUIButtons.UpdateButtonVisibility(false);
        }

        // ターゲット決定
        targetPosition = GetRandomTargetPosition(
            animator, animator.transform.position, minPosition, maxPosition, minDistance, retryCount
        );

        // 有効な目標位置がない場合は即座にリセット
        if (!IsValidTargetPosition(animator.transform.position, targetPosition))
        {
            DebugLog("No valid target position found - resetting immediately");
            ResetState(animator);
            return;
        }

        initialRotation = animator.transform.rotation;
        Vector3 currentPosXZ = new Vector3(animator.transform.position.x, 0f, animator.transform.position.z);
        Vector3 targetPosXZ = new Vector3(targetPosition.x, 0f, targetPosition.z);
        Vector3 direction = targetPosXZ - currentPosXZ;
        float targetYAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0f, targetYAngle, 0f);

        DebugLog($"[idleWalk]Initial Rotation: {initialRotation.eulerAngles}");
        DebugLog($"[idleWalk]Target Rotation: {targetRotation.eulerAngles}");
        DebugLog($"[idleWalk]Target Position: {targetPosition}");

        float currentYAngle = initialRotation.eulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentYAngle, targetYAngle);
        if (Mathf.Abs(angleDifference) > rotationThreshold)
        {
            animator.SetBool("turning", true);
            _currentState = State.RotatingToTarget;
            DebugLog($"[idleWalk]Starting with rotation to target (angle diff: {angleDifference:F1}°)");
        }
        else
        {
            _currentState = State.MovingToTarget;
            DebugLog("[idleWalk]Starting with movement (no rotation needed)");
        }
        rotationStartTime = Time.time;
        _currentSpeed = 0.0f;
        mainCamera = Camera.main;

        animator.SetFloat("walk_x", 0);
        animator.SetFloat("walk_y", 0);

        GlobalVariables.CurrentState = PetState.moving;
        DebugLog("[idleWalk]State changed to: moving");

#if UNITY_EDITOR
        ShowTarget(animator, targetPosition);
#endif
    }

    /// <summary>
    /// 目標位置が有効かどうかを判定
    /// </summary>
    private bool IsValidTargetPosition(Vector3 currentPos, Vector3 targetPos)
    {
        // Vector3.zero または現在位置に非常に近い場合は無効
        float distSqr = (targetPos - currentPos).sqrMagnitude;
        return distSqr > 0.5f * 0.5f; // 0.5m以上離れていれば有効
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // タイムアウトチェック
        if (Time.time - _stateStartTime > timeoutSeconds)
        {
            GameLogger.LogWarning(LogCategory.Animation,$"[IdleWalkAnimation] Timeout after {timeoutSeconds}s - forcing reset");
            ResetState(animator);
            return;
        }

        switch (_currentState)
        {
            case State.RotatingToTarget:
                {
                    if (RotateTowardsTarget(animator, targetRotation, rotationStartTime))
                    {
                        DebugLog("[idleWalk]Rotation to target completed, switching to MovingToTarget");
                        _currentState = State.MovingToTarget;
                        animator.SetBool("turning", false);
                    }
                    break;
                }

            case State.MovingToTarget:
                {
                    if (MoveAndRotateTowardsTarget(animator))
                    {
                        DebugLog("Movement completed, switching to FinalizingRotation");
                        _currentState = State.FinalizingRotation;
                        finalRotationStartTime = Time.time;
                        finalRotationStart = animator.transform.rotation;

                        // カメラ方向への回転を計算
                        Vector3 cameraDirection = new Vector3(0f, 0.8f, 2.5f) - animator.transform.position;
                        Vector3 horizontalDirection = new Vector3(cameraDirection.x, 0f, cameraDirection.z);

                        // ゼロベクトルチェック（犬がカメラ真下付近にいる場合）
                        if (horizontalDirection.sqrMagnitude < 0.0001f)
                        {
                            DebugLog("[idleWalk] Camera direction is zero - using forward direction");
                            finalRotationEnd = Quaternion.Euler(0f, 0f, 0f); // 前方を向く
                        }
                        else
                        {
                            finalRotationEnd = Quaternion.LookRotation(horizontalDirection.normalized);
                        }
                        DebugLog($"[idleWalk]Final Rotation End: {finalRotationEnd.eulerAngles}");
                    }
                    break;
                }
            case State.FinalizingRotation:
                {
                    if (RotateTowardsTarget(animator, finalRotationEnd, finalRotationStartTime))
                    {
                        DebugLog("[idleWalk]Final rotation completed, resetting state");
                        ResetState(animator);
                        animator.SetBool("turning", false);
                    }
                    break;
                }

            case State.Idle:
                {
                    GlobalVariables.CurrentState = PetState.idle;
                    animator.SetBool("turning", false);
                    break;
                }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 強制終了時のクリーンアップ
        if (_currentState != State.Idle)
        {
            DebugLog("[IdleWalkAnimation] OnStateExit - cleaning up incomplete state");

            // Animatorパラメータをリセット
            animator.SetFloat("walk_x", 0);
            animator.SetFloat("walk_y", 0);
            animator.SetFloat("angle", 0);
            animator.SetBool("turning", false);

            // UIボタンを再表示
            if (_mainUIButtons != null)
            {
                _mainUIButtons.UpdateButtonVisibility(true);
            }

            // グローバル状態をリセット
            if (GlobalVariables.CurrentState == PetState.moving)
            {
                GlobalVariables.CurrentState = PetState.idle;
            }

            _currentState = State.Idle;

#if UNITY_EDITOR
            HideTarget(animator);
#endif
        }
    }

    private bool RotateTowardsTarget(Animator animator, Quaternion endRotation, float rotationStartTime = 0f)
    {
        // Y軸のみの角度差を計算
        float currentY = animator.transform.rotation.eulerAngles.y;
        float targetY = endRotation.eulerAngles.y;
        float remainingAngle = Mathf.DeltaAngle(currentY, targetY);

        // 残り角度が小さければ完了
        if (Mathf.Abs(remainingAngle) < 1.0f)
        {
            animator.transform.rotation = Quaternion.Euler(0f, targetY, 0f);
            animator.SetFloat("angle", 0);
            return true;
        }

        // Y軸の角度を直接変更（Time.deltaTime でフレームレート非依存）
        float rotationStep = rotationSpeed * Time.deltaTime;
        float actualRotation = Mathf.Clamp(remainingAngle, -rotationStep, rotationStep);
        float newY = currentY + actualRotation;
        animator.transform.rotation = Quaternion.Euler(0f, newY, 0f);

        // BlendTreeパラメータは残り角度を正規化
        float normalizedAngle = remainingAngle / 180f;
        animator.SetFloat("angle", normalizedAngle);

        DebugLog($"[idleWalk] Rotating: RemainingAngle={Mathf.Abs(remainingAngle):F1}°, Rotation={animator.transform.rotation.eulerAngles}");

        return false;
    }

    private bool MoveAndRotateTowardsTarget(Animator animator)
    {
        Vector3 current = animator.transform.position;
        Vector3 toTarget = new Vector3(targetPosition.x, current.y, targetPosition.z) - current;
        Vector3 dirXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float dist = dirXZ.magnitude;

        if (dist > 1e-4f)
        {
            Quaternion look = Quaternion.LookRotation(dirXZ);
            animator.transform.rotation = Quaternion.RotateTowards(
                animator.transform.rotation, look, rotationSpeed * Time.deltaTime
            );

            float angleDiff = Quaternion.Angle(animator.transform.rotation, look);
            float normalizedAngle = Mathf.Clamp01(angleDiff / 180f);
            float sign = Mathf.Sign(Vector3.Cross(animator.transform.forward, dirXZ.normalized).y);
            SmoothSetWalkX(animator, normalizedAngle * sign, smoothingSpeedX);
        }

        _currentSpeed = Mathf.Clamp(_currentSpeed + acceleration * Time.deltaTime, 0f, moveSpeed);
        float step = _currentSpeed * Time.deltaTime;

        if (dist <= targetProximity || step >= dist)
        {
            Vector3 snapped = new Vector3(targetPosition.x, current.y, targetPosition.z);
            animator.transform.position = snapped;
            SmoothSetWalkY(animator, 0f, smoothingSpeedY);
            return true;
        }

        Vector3 next = Vector3.MoveTowards(current, new Vector3(targetPosition.x, current.y, targetPosition.z), step);
        animator.transform.position = next;

        float y = _currentSpeed / Mathf.Max(0.001f, moveSpeed);
        SmoothSetWalkY(animator, y, smoothingSpeedY);

        return false;
    }

    private void ResetState(Animator animator)
    {
        DebugLog("=== ResetState ===");

        animator.SetFloat("walk_x", 0);
        animator.SetFloat("walk_y", 0);
        animator.SetInteger("TransitionNo", 0);

        if (_mainUIButtons != null)
            _mainUIButtons.UpdateButtonVisibility(true);

        _currentState = State.Idle;

        DebugLog("State changed to: idle");

#if UNITY_EDITOR
        HideTarget(animator);
#endif
    }

    private void SmoothSetWalkX(Animator animator, float targetWalkX, float smoothingSpeed = 10f)
    {
        float currentWalkX = animator.GetFloat("walk_x");
        float smoothedWalkX = Mathf.Lerp(currentWalkX, targetWalkX, Time.deltaTime * smoothingSpeed);
        animator.SetFloat("walk_x", smoothedWalkX);
    }

    private void SmoothSetWalkY(Animator animator, float targetWalkY, float smoothingSpeed = 10f)
    {
        float currentWalkY = animator.GetFloat("walk_y");
        float smoothedWalkY = Mathf.Lerp(currentWalkY, targetWalkY, Time.deltaTime * smoothingSpeed);
        animator.SetFloat("walk_y", smoothedWalkY);
    }

    private Vector3 GetRandomTargetPosition(
        Animator animator, Vector3 origin, Vector3 minPos, Vector3 maxPos, float minDist, int retries)
    {
        Vector3 forward = animator.transform.forward;
        forward.y = 0f;

        Vector3 bestForward = origin;   // 前方の候補（優先）
        float bestForwardDist = 0f;
        bool foundForward = false;

        Vector3 bestAny = origin;       // 任意方向の候補（フォールバック）
        float bestAnyDist = 0f;

        const float rearThreshold = 120f; // 120°以上は「後ろ方向」とみなす

        float minX = Mathf.Min(minPos.x, maxPos.x);
        float maxX = Mathf.Max(minPos.x, maxPos.x);
        float minZ = Mathf.Min(minPos.z, maxPos.z);
        float maxZ = Mathf.Max(minPos.z, maxPos.z);

        for (int i = 0; i <= retries; i++)
        {
            float randZ = Random.Range(minZ, maxZ);
            float zProgress = Mathf.InverseLerp(0, minZ, randZ);
            float xRange = Mathf.Lerp(0.2f, 0.3f, zProgress);
            float randX = Random.Range(-xRange, xRange);

            const float snap = 0.2f;
            float sx = Mathf.Round(randX / snap) * snap;
            float sz = Mathf.Round(randZ / snap) * snap;

            Vector3 candidate = new Vector3(sx, origin.y, sz);
            candidate = ClampInside(candidate, minPos, maxPos);

            float d = Vector3.Distance(candidate, origin);

            DebugLog($"Candidate {i}: {candidate}, Dist={d:F2}");

            // 距離が最低限なければ候補外
            if (d < minDist) continue;

            // 任意方向の最良候補を更新
            if (d > bestAnyDist)
            {
                bestAnyDist = d;
                bestAny = candidate;
            }

            // 角度チェック
            Vector3 dir = (candidate - origin);
            dir.y = 0f;
            float angle = Vector3.Angle(forward, dir);

            // 前方の候補（120°未満）を優先
            if (angle < rearThreshold && d > bestForwardDist)
            {
                bestForwardDist = d;
                bestForward = candidate;
                foundForward = true;
                DebugLog($"Found forward candidate: {candidate}, angle={angle:F1}°");
            }
        }

        // 前方の候補があればそれを返す
        if (foundForward)
        {
            DebugLog($"Selected FORWARD target: {bestForward}, Dist={bestForwardDist:F1}");
            return bestForward;
        }

        // なければ任意方向の最良候補を返す
        DebugLog($"Selected ANY-DIRECTION target: {bestAny}, Dist={bestAnyDist:F1}");
        return bestAny;
    }

    private static Vector3 ClampInside(Vector3 pos, Vector3 a, Vector3 b)
    {
        float minX = Mathf.Min(a.x, b.x);
        float maxX = Mathf.Max(a.x, b.x);
        float minZ = Mathf.Min(a.z, b.z);
        float maxZ = Mathf.Max(a.z, b.z);

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        return pos;
    }

#if UNITY_EDITOR
    private void ShowTarget(Animator animator, Vector3 targetPosition)
    {
        var targetVisualizer = animator.GetComponent<TargetVisualizer>();
        if (targetVisualizer == null) return;
        targetVisualizer.Show(targetPosition);
    }

    private void HideTarget(Animator animator)
    {
        var targetVisualizer = animator.GetComponent<TargetVisualizer>();
        if (targetVisualizer == null) return;
        targetVisualizer.Hide();
    }
#endif
}
