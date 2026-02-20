using UnityEngine;
using UnityEngine.AI;
using TapHouse.Logging;

/// <summary>
/// NavMesh パス検証付きアイドル歩行アニメーション
///
/// 旧版 IdleWalkAnimation.cs をベースに、NavMesh によるパス有効性チェックを追加。
/// 移動・回転処理自体は旧版と同じ transform 操作を使用。
///
/// 順序: 目標方向に回転 → 移動 → カメラ方向に回転
/// </summary>
public class IdleWalkAnimationNavMesh : StateMachineBehaviour
{
    private enum State
    {
        RotatingToTarget,
        MovingToTarget,
        FinalizingRotation,
        Idle
    }

    private State _currentState = State.Idle;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Quaternion _finalRotationEnd = Quaternion.Euler(0, 0, 0);

    private MainUIButtons _mainUIButtons;
    private DogController _dogController;
    private bool _isInitialized = false;

    [Header("移動範囲")]
    [SerializeField] private Vector3 minPosition = new Vector3(-0.3f, 0f, -4.0f);
    [SerializeField] private Vector3 maxPosition = new Vector3(0.3f, 0f, 0.5f);
    [SerializeField] private float minDistance = 2.0f;
    [SerializeField] private int retryCount = 5;

    [Header("速度/回転")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float acceleration = 0.5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float targetProximity = 0.3f;

    [Header("アニメーション")]
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
            GameLogger.Log(LogCategory.Animation, $"[IdleWalkNavMesh] {message}");
        }
    }

    /// <summary>
    /// 初回初期化（OnStateEnterで呼び出し）
    /// </summary>
    private void Initialize(Animator animator)
    {
        if (_isInitialized) return;

        _mainUIButtons = Object.FindObjectOfType<MainUIButtons>();
        _dogController = animator.GetComponent<DogController>();
        _isInitialized = true;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null)
        {
            GameLogger.LogError(LogCategory.Animation, "[IdleWalkNavMesh] Animator is null");
            return;
        }

        Initialize(animator);
        DebugLog("=== OnStateEnter ===");

        // sleep状態の場合は処理をスキップ
        if (GlobalVariables.CurrentState == PetState.sleeping)
        {
            DebugLog("Pet is sleeping, skipping idle walk");
            SafeSetInteger(animator, "TransitionNo", 1);
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

        // ターゲット決定（NavMesh検証付き）
        _targetPosition = GetRandomTargetPositionWithNavMesh(
            animator, animator.transform.position, minPosition, maxPosition, minDistance, retryCount
        );

        // 有効な目標位置がない場合は即座にリセット
        if (!IsValidTargetPosition(animator.transform.position, _targetPosition))
        {
            DebugLog("No valid target position found - resetting immediately");
            ResetState(animator);
            return;
        }

        // 回転計算
        Vector3 currentPosXZ = new Vector3(animator.transform.position.x, 0f, animator.transform.position.z);
        Vector3 targetPosXZ = new Vector3(_targetPosition.x, 0f, _targetPosition.z);
        Vector3 direction = targetPosXZ - currentPosXZ;

        // ゼロベクトルチェック
        if (direction.sqrMagnitude < 0.0001f)
        {
            DebugLog("Direction is zero - resetting immediately");
            ResetState(animator);
            return;
        }

        float targetYAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        _targetRotation = Quaternion.Euler(0f, targetYAngle, 0f);

        DebugLog($"Target Rotation: {_targetRotation.eulerAngles}");
        DebugLog($"Target Position: {_targetPosition}");

        float currentYAngle = animator.transform.rotation.eulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentYAngle, targetYAngle);

        if (Mathf.Abs(angleDifference) > rotationThreshold)
        {
            _currentState = State.RotatingToTarget;
            DebugLog($"Starting with rotation to target (angle diff: {angleDifference:F1}°)");
        }
        else
        {
            _currentState = State.MovingToTarget;
            DebugLog("Starting with movement (no rotation needed)");
        }

        _currentSpeed = 0.0f;

        SafeSetFloat(animator, "walk_x", 0);
        SafeSetFloat(animator, "walk_y", 0);

        GlobalVariables.CurrentState = PetState.moving;
        DebugLog("State changed to: moving");

#if UNITY_EDITOR
        ShowTarget(animator, _targetPosition);
#endif
    }

    /// <summary>
    /// 目標位置が有効かどうかを判定
    /// </summary>
    private bool IsValidTargetPosition(Vector3 currentPos, Vector3 targetPos)
    {
        // 距離チェック
        float distSqr = (targetPos - currentPos).sqrMagnitude;
        if (distSqr < 0.5f * 0.5f) return false;

        // NavMeshで到達可能かチェック
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(currentPos, targetPos, NavMesh.AllAreas, path))
        {
            return path.status == NavMeshPathStatus.PathComplete;
        }

        return true; // NavMeshが設定されていない場合は許可
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null) return;

        // タイムアウトチェック
        if (Time.time - _stateStartTime > timeoutSeconds)
        {
            GameLogger.LogWarning(LogCategory.Animation, $"[IdleWalkNavMesh] Timeout after {timeoutSeconds}s - forcing reset");
            ResetState(animator);
            return;
        }

        switch (_currentState)
        {
            case State.RotatingToTarget:
                if (RotateTowardsTarget(animator, _targetRotation))
                {
                    DebugLog("Rotation to target completed, switching to MovingToTarget");
                    _currentState = State.MovingToTarget;
                }
                break;

            case State.MovingToTarget:
                if (MoveAndRotateTowardsTarget(animator))
                {
                    DebugLog("Movement completed, switching to FinalizingRotation");
                    _currentState = State.FinalizingRotation;

                    // カメラ方向への回転を計算
                    Vector3 cameraDirection = new Vector3(0f, 0.8f, 2.5f) - animator.transform.position;
                    Vector3 horizontalDirection = new Vector3(cameraDirection.x, 0f, cameraDirection.z);

                    // ゼロベクトルチェック
                    if (horizontalDirection.sqrMagnitude < 0.0001f)
                    {
                        DebugLog("Camera direction is zero - using forward direction");
                        _finalRotationEnd = Quaternion.Euler(0f, 0f, 0f);
                    }
                    else
                    {
                        _finalRotationEnd = Quaternion.LookRotation(horizontalDirection.normalized);
                    }
                    DebugLog($"Final Rotation End: {_finalRotationEnd.eulerAngles}");
                }
                break;

            case State.FinalizingRotation:
                if (RotateTowardsTarget(animator, _finalRotationEnd))
                {
                    DebugLog("Final rotation completed, resetting state");
                    ResetState(animator);
                }
                break;

            case State.Idle:
                if (GlobalVariables.CurrentState == PetState.moving)
                {
                    GlobalVariables.CurrentState = PetState.idle;
                }
                break;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // nullチェック
        if (animator == null) return;

        // 強制終了時のクリーンアップ
        if (_currentState != State.Idle)
        {
            DebugLog("OnStateExit - cleaning up incomplete state");

            // LateUpdate回転制御をクリア
            if (_dogController != null)
            {
                _dogController.ClearPendingRotation();
            }

            SafeSetFloat(animator, "walk_x", 0);
            SafeSetFloat(animator, "walk_y", 0);
            SafeSetFloat(animator, "angle", 0);

            if (_mainUIButtons != null)
            {
                _mainUIButtons.UpdateButtonVisibility(true);
            }

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

    private bool RotateTowardsTarget(Animator animator, Quaternion endRotation)
    {
        if (animator == null) return true;

        // Y軸のみの角度差を計算
        float currentY = animator.transform.rotation.eulerAngles.y;
        float targetY = endRotation.eulerAngles.y;
        float remainingAngle = Mathf.DeltaAngle(currentY, targetY);

        // 残り角度が小さければ完了
        if (Mathf.Abs(remainingAngle) < 1.0f)
        {
            // LateUpdateで回転を適用
            if (_dogController != null)
            {
                _dogController.SetPendingRotation(Quaternion.Euler(0f, targetY, 0f));
            }
            SafeSetFloat(animator, "angle", 0);
            SmoothSetWalkX(animator, 0f, smoothingSpeedX);
            SmoothSetWalkY(animator, 0f, smoothingSpeedY);
            return true;
        }

        // Y軸の角度を計算
        float rotationStep = rotationSpeed * Time.deltaTime;
        float actualRotation = Mathf.Clamp(remainingAngle, -rotationStep, rotationStep);
        float newY = currentY + actualRotation;

        // LateUpdateで回転を適用（アニメーション評価後に上書き）
        if (_dogController != null)
        {
            _dogController.SetPendingRotation(Quaternion.Euler(0f, newY, 0f));
        }

        // BlendTreeパラメータは残り角度を正規化
        float normalizedAngle = remainingAngle / 180f;
        SafeSetFloat(animator, "angle", normalizedAngle);

        // 5ポイントBlendTree用: 回転中のアニメーション
        float turnDirection = Mathf.Sign(remainingAngle);
        float turnIntensity = Mathf.Clamp01(Mathf.Abs(remainingAngle) / 45f);
        SmoothSetWalkX(animator, turnDirection * turnIntensity, smoothingSpeedX);
        SmoothSetWalkY(animator, 0f, smoothingSpeedY);

        DebugLog($"Rotating: RemainingAngle={Mathf.Abs(remainingAngle):F1}°, newY={newY:F1}°, walk_x={turnDirection * turnIntensity:F2}");

        return false;
    }

    private bool MoveAndRotateTowardsTarget(Animator animator)
    {
        if (animator == null) return true;

        Vector3 current = animator.transform.position;
        Vector3 toTarget = new Vector3(_targetPosition.x, current.y, _targetPosition.z) - current;
        Vector3 dirXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float dist = dirXZ.magnitude;

        if (dist > 1e-4f)
        {
            Quaternion look = Quaternion.LookRotation(dirXZ);
            Quaternion newRotation = Quaternion.RotateTowards(
                animator.transform.rotation, look, rotationSpeed * Time.deltaTime
            );

            // LateUpdateで回転を適用（アニメーション評価後に上書き）
            if (_dogController != null)
            {
                _dogController.SetPendingRotation(newRotation);
            }

            float angleDiff = Quaternion.Angle(newRotation, look);
            float normalizedAngle = Mathf.Clamp01(angleDiff / 180f);
            float sign = Mathf.Sign(Vector3.Cross(newRotation * Vector3.forward, dirXZ.normalized).y);
            SmoothSetWalkX(animator, normalizedAngle * sign, smoothingSpeedX);
        }

        _currentSpeed = Mathf.Clamp(_currentSpeed + acceleration * Time.deltaTime, 0f, moveSpeed);
        float step = _currentSpeed * Time.deltaTime;

        if (dist <= targetProximity || step >= dist)
        {
            Vector3 snapped = new Vector3(_targetPosition.x, current.y, _targetPosition.z);
            animator.transform.position = snapped;
            SmoothSetWalkY(animator, 0f, smoothingSpeedY);
            return true;
        }

        Vector3 next = Vector3.MoveTowards(current, new Vector3(_targetPosition.x, current.y, _targetPosition.z), step);
        animator.transform.position = next;

        float y = _currentSpeed / Mathf.Max(0.001f, moveSpeed);
        SmoothSetWalkY(animator, y, smoothingSpeedY);

        return false;
    }

    private void ResetState(Animator animator)
    {
        DebugLog("=== ResetState ===");

        // LateUpdate回転制御をクリア
        if (_dogController != null)
        {
            _dogController.ClearPendingRotation();
        }

        SafeSetFloat(animator, "walk_x", 0);
        SafeSetFloat(animator, "walk_y", 0);
        SafeSetInteger(animator, "TransitionNo", 0);

        if (_mainUIButtons != null)
        {
            _mainUIButtons.UpdateButtonVisibility(true);
        }

        _currentState = State.Idle;

        if (GlobalVariables.CurrentState == PetState.moving)
        {
            GlobalVariables.CurrentState = PetState.idle;
        }

        DebugLog("State changed to: idle");

#if UNITY_EDITOR
        HideTarget(animator);
#endif
    }

    #region Safe Animator Access
    private void SafeSetFloat(Animator animator, string name, float value)
    {
        if (animator == null) return;
        try
        {
            animator.SetFloat(name, value);
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LogCategory.Animation, $"[IdleWalkNavMesh] Failed to set float {name}: {e.Message}");
        }
    }

    private void SafeSetInteger(Animator animator, string name, int value)
    {
        if (animator == null) return;
        try
        {
            animator.SetInteger(name, value);
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LogCategory.Animation, $"[IdleWalkNavMesh] Failed to set integer {name}: {e.Message}");
        }
    }

    private float SafeGetFloat(Animator animator, string name)
    {
        if (animator == null) return 0f;
        try
        {
            return animator.GetFloat(name);
        }
        catch (System.Exception e)
        {
            GameLogger.LogWarning(LogCategory.Animation, $"[IdleWalkNavMesh] Failed to get float {name}: {e.Message}");
            return 0f;
        }
    }

    #endregion

    #region Smooth Animation

    private void SmoothSetWalkX(Animator animator, float targetWalkX, float smoothingSpeed = 10f)
    {
        float currentWalkX = SafeGetFloat(animator, "walk_x");
        float smoothedWalkX = Mathf.Lerp(currentWalkX, targetWalkX, Time.deltaTime * smoothingSpeed);
        SafeSetFloat(animator, "walk_x", smoothedWalkX);
    }

    private void SmoothSetWalkY(Animator animator, float targetWalkY, float smoothingSpeed = 10f)
    {
        float currentWalkY = SafeGetFloat(animator, "walk_y");
        float smoothedWalkY = Mathf.Lerp(currentWalkY, targetWalkY, Time.deltaTime * smoothingSpeed);
        SafeSetFloat(animator, "walk_y", smoothedWalkY);
    }

    #endregion

    #region Target Position

    private Vector3 GetRandomTargetPositionWithNavMesh(
        Animator animator, Vector3 origin, Vector3 minPos, Vector3 maxPos, float minDist, int retries)
    {
        if (animator == null) return origin;

        Vector3 forward = animator.transform.forward;
        forward.y = 0f;

        Vector3 bestForward = origin;
        float bestForwardDist = 0f;
        bool foundForward = false;

        Vector3 bestAny = origin;
        float bestAnyDist = 0f;

        const float rearThreshold = 120f;

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

            // NavMesh上に補正
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                candidate = hit.position;
            }

            float d = Vector3.Distance(candidate, origin);

            DebugLog($"Candidate {i}: {candidate}, Dist={d:F2}");

            if (d < minDist) continue;

            // NavMeshで到達可能かチェック
            NavMeshPath path = new NavMeshPath();
            bool canReach = NavMesh.CalculatePath(origin, candidate, NavMesh.AllAreas, path);
            if (canReach && path.status != NavMeshPathStatus.PathComplete)
            {
                DebugLog($"Candidate {i}: Path not complete, skipping");
                continue;
            }

            if (d > bestAnyDist)
            {
                bestAnyDist = d;
                bestAny = candidate;
            }

            Vector3 dir = (candidate - origin);
            dir.y = 0f;
            float angle = Vector3.Angle(forward, dir);

            if (angle < rearThreshold && d > bestForwardDist)
            {
                bestForwardDist = d;
                bestForward = candidate;
                foundForward = true;
                DebugLog($"Found forward candidate: {candidate}, angle={angle:F1}°");
            }
        }

        if (foundForward)
        {
            DebugLog($"Selected FORWARD target: {bestForward}, Dist={bestForwardDist:F1}");
            return bestForward;
        }

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

    #endregion

#if UNITY_EDITOR
    private void ShowTarget(Animator animator, Vector3 targetPosition)
    {
        if (animator == null) return;
        var targetVisualizer = animator.GetComponent<TargetVisualizer>();
        if (targetVisualizer == null) return;
        targetVisualizer.Show(targetPosition);
    }

    private void HideTarget(Animator animator)
    {
        if (animator == null) return;
        var targetVisualizer = animator.GetComponent<TargetVisualizer>();
        if (targetVisualizer == null) return;
        targetVisualizer.Hide();
    }
#endif
}
