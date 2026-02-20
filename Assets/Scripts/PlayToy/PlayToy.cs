using System.Collections;
using UnityEngine;
using TapHouse.Logging;

/// <summary>
/// おもちゃで遊ぶ処理（フリーズ対策強化版）
/// 
/// 【修正内容】
/// 1. Coroutine実行中フラグで再入防止
/// 2. キャンセル時の確実なクリーンアップ
/// 3. タイムアウト機能の追加
/// </summary>
public class PlayToy : MonoBehaviour
{
    private enum AngleSetter
    {
        ForCenter,
        ForPoint
    }

    [Header("References")]
    [SerializeField] private GameObject dog;
    [SerializeField] private GetToy getToy;
    [SerializeField] private AngleSetForCenter angleSetForCenter;
    [SerializeField] private AngleSetForPoint angleSetForPoint;
    [SerializeField] private PlayManager playManager;
    [SerializeField] private DogController _dogController;

    [Header("Play Settings")]
    [SerializeField] private AngleSetter angleSetMode = AngleSetter.ForCenter;
    [SerializeField, Range(0, 1)] private float biteToyProbability = 0.5f;
    [SerializeField] private float biteTime = 5f;
    [SerializeField] private float throwTiming = 1f;
    [SerializeField] private float throwTimingVariation = 0.2f;
    [SerializeField] private float chaseWaitTime = 1f;

    [Header("Throw Settings")]
    [SerializeField] private float minThrowSpeed = 0.5f;
    [SerializeField] private float maxThrowSpeed = 1.5f;

    [Header("Toy Positions")]
    [SerializeField] private Vector3 toyBiteLocalPosition = new Vector3(0.015f, 0.025f, -0.02f);
    [SerializeField] private Vector3 toyThrowMouthLocalPos = new Vector3(0.1f, 0.07f, 0f);

    [Header("Safety Settings")]
    [SerializeField] private float maxCoroutineTime = 30f;

    [Header("Debug - TEMPORARY DISABLE")]
    [SerializeField] private bool enableBiteAnimation = false;
    [SerializeField] private bool enableSwingAnimation = false;
    [SerializeField] private bool enableDetailedLogs = true;

    [HideInInspector] public bool haveToy;

    private I_ToyThrowAngle currentAngleStrategy;
    private bool isCancelled = false;
    private bool isCoroutineRunning = false;
    private Coroutine activeCoroutine;

    private void Start()
    {
        currentAngleStrategy = angleSetMode == AngleSetter.ForCenter
            ? (I_ToyThrowAngle)angleSetForCenter
            : (I_ToyThrowAngle)angleSetForPoint;

        if (dog == null && getToy != null)
        {
            dog = getToy.gameObject;
        }

        DebugLog("PlayToy initialized (Animation Debug Mode)");
        DebugLog($"Bite Animation: {(enableBiteAnimation ? "ENABLED" : "DISABLED")}");
        DebugLog($"Swing Animation: {(enableSwingAnimation ? "ENABLED" : "DISABLED")}");
    }

    public IEnumerator DoPlayCoroutine(GameObject toy)
    {
        if (isCoroutineRunning)
        {
            DebugLog("DoPlayCoroutine: Already running, skipping");
            yield break;
        }

        DebugLog("DoPlayCoroutine: START");
        isCoroutineRunning = true;
        haveToy = true;
        isCancelled = false;

        Coroutine timeoutCoroutine = StartCoroutine(TimeoutWatcher());
        yield return StartCoroutine(SelectRandomActionCoroutine(toy));

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }

        DebugLog("DoPlayCoroutine: END");
        haveToy = false;
        isCoroutineRunning = false;
    }

    /// <summary>
    /// タイムアウト監視
    /// </summary>
    private IEnumerator TimeoutWatcher()
    {
        float elapsed = 0f;
        while (elapsed < maxCoroutineTime && !isCancelled)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= maxCoroutineTime)
        {
            GameLogger.LogWarning(LogCategory.PlayToy,$"[PlayToy] TIMEOUT after {maxCoroutineTime}s! Force cancelling...");
            CancelPlay();
        }
    }

    private IEnumerator SelectRandomActionCoroutine(GameObject toy)
    {
        if (isCancelled)
        {
            DebugLog("SelectRandomActionCoroutine: CANCELLED");
            yield break;
        }

        DebugLog("SelectRandomActionCoroutine: START");

        float random = UnityEngine.Random.Range(0f, 1f);
        DebugLog($"Random={random:F3}, Threshold={biteToyProbability:F3}");

        if (random < biteToyProbability)
        {
            DebugLog("Action: BITE");
            yield return StartCoroutine(BiteSequenceCoroutine(toy));
        }
        else
        {
            DebugLog("Action: THROW");
            yield return StartCoroutine(ThrowSequenceCoroutine(toy));
        }

        DebugLog("SelectRandomActionCoroutine: END");
    }

    private IEnumerator BiteSequenceCoroutine(GameObject toy)
    {
        if (isCancelled) yield break;

        DebugLog("=== BITE SEQUENCE START ===");

        if (enableBiteAnimation && _dogController != null)
        {
            DebugLog("Calling SetBite(true)...");

            yield return null;
            _dogController.SetBite(true);

            yield return new WaitForSeconds(0.1f);

            DebugLog("SetBite(true) returned");
        }
        else
        {
            DebugLog("SetBite SKIPPED (disabled for testing)");
        }

        toy.transform.localPosition = toyBiteLocalPosition;
        DebugLog($"Position set. Waiting {biteTime}s...");

        float elapsed = 0f;
        while (elapsed < biteTime && !isCancelled)
        {
            float waitTime = Mathf.Min(0.1f, biteTime - elapsed);
            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;

            if (elapsed % 0.5f < 0.15f)
            {
                DebugLog($"Bite wait: {elapsed:F1}/{biteTime}s");
            }
        }

        if (isCancelled)
        {
            DebugLog("BITE CANCELLED during wait");
            yield break;
        }

        DebugLog("Bite wait complete");

        if (enableBiteAnimation && _dogController != null)
        {
            DebugLog("Calling SetBite(false)...");

            yield return null;
            _dogController.SetBite(false);
            yield return new WaitForSeconds(0.1f);

            DebugLog("SetBite(false) returned");
        }
        else
        {
            DebugLog("SetBite(false) SKIPPED");
        }

        DebugLog("=== BITE SEQUENCE -> THROW ===");

        if (!isCancelled)
        {
            yield return StartCoroutine(ThrowSequenceCoroutine(toy));
        }

        DebugLog("=== BITE SEQUENCE END ===");
    }

    private IEnumerator ThrowSequenceCoroutine(GameObject toy)
    {
        if (isCancelled) yield break;

        DebugLog("=== THROW SEQUENCE START ===");

        if (enableSwingAnimation && _dogController != null)
        {
            DebugLog("Calling SetSwing(true)...");

            yield return null;
            _dogController.SetSwing(true);
            yield return new WaitForSeconds(0.1f);

            DebugLog("SetSwing(true) returned");
        }
        else
        {
            DebugLog("SetSwing SKIPPED (disabled for testing)");
        }

        float timing = UnityEngine.Random.Range(
            throwTiming - throwTimingVariation,
            throwTiming + throwTimingVariation
        );
        DebugLog($"Throw timing: {timing:F2}s");

        try
        {
            toy.transform.localPosition = toyThrowMouthLocalPos;
            DebugLog("Throw position set");

            MatchRotation matchRotate = toy?.GetComponent<MatchRotation>();
            if (matchRotate != null)
            {
                DebugLog("Applying MatchRotation...");
                matchRotate.Match(dog);
                yield return null;
                DebugLog("MatchRotation done");
            }

            float elapsed = 0f;
            while (elapsed < timing && !isCancelled)
            {
                float waitTime = Mathf.Min(0.1f, timing - elapsed);
                yield return new WaitForSeconds(waitTime);
                elapsed += waitTime;

                if (elapsed % 0.5f < 0.15f)
                {
                    DebugLog($"Throw wait: {elapsed:F1}/{timing:F2}s");
                }
            }

            if (isCancelled)
            {
                DebugLog("THROW CANCELLED during wait");
                yield break;
            }

            DebugLog("Throw wait complete");

            DebugLog("Executing throw...");
            ExecuteThrow(toy);
            DebugLog("Throw executed");

            DebugLog($"Chase wait {chaseWaitTime}s...");
            yield return new WaitForSeconds(chaseWaitTime);
            DebugLog("Chase wait done");
        }
        finally
        {
            if (enableSwingAnimation && _dogController != null)
            {
                DebugLog("Calling SetSwing(false) in finally...");

                // ✅ 修正: yieldはfinallyで使えないのでTry-Catchで囲む
                _dogController.SetSwing(false);

                DebugLog("SetSwing(false) returned");
            }
            else
            {
                DebugLog("SetSwing(false) SKIPPED in finally");
            }
        }

        DebugLog("=== THROW SEQUENCE END ===");
    }
    private void ExecuteThrow(GameObject toy)
    {
        DebugLog("ExecuteThrow: START");

        if (enableSwingAnimation && _dogController != null)
        {
            _dogController.SetSwing(false);
        }

        toy.transform.SetParent(null);
        DebugLog("Parent cleared");

        Rigidbody rb = toy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            DebugLog("Rigidbody OK");
        }

        float speed = UnityEngine.Random.Range(minThrowSpeed, maxThrowSpeed);
        float angle = currentAngleStrategy.ThrowAngle(toy);
        DebugLog($"Speed={speed:F2}, Angle={angle:F2}");

        haveToy = false;
        playManager.ThrowToy(speed, angle, toy);

        DebugLog("ExecuteThrow: END");
    }

    public void CancelPlay()
    {
        DebugLog("CancelPlay");
        isCancelled = true;
        isCoroutineRunning = false;

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        if (_dogController != null)
        {
            if (enableBiteAnimation) _dogController.SetBite(false);
            if (enableSwingAnimation) _dogController.SetSwing(false);
        }
    }

    private void OnDestroy()
    {
        DebugLog("OnDestroy");
        CancelPlay();
    }

    private void DebugLog(string message)
    {
        if (enableDetailedLogs)
        {
            GameLogger.Log(LogCategory.PlayToy, $"[PlayToy] {message}");
        }
    }
}