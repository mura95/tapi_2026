using UnityEngine;
using TMPro;
using System;
using System.Text;
using TapHouse.Logging;
using TapHouse.MultiDevice;
using TapHouse.MetaverseWalk.Core;

/// <summary>
/// DebugCanvasを管理する
/// DebugCanvasには開発用の情報（state, dog's position, wifi状態など）を表示
/// 表示/非表示の切り替えは外部（FirebaseUIManagerやDebugButton）から呼び出される
///
/// デバッグモードの一元管理:
/// IsDebugMode を参照することで、散歩ボタン強制表示等すべてのデバッグ機能を制御
/// </summary>
public class DebugCanvasManager : MonoBehaviour
{
    [Header("Debug Canvas")]
    [SerializeField] private GameObject debugCanvas;

    [Header("Debug Info Text (Single)")]
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("References (Optional)")]
    [SerializeField] private DogController dogController;
    [SerializeField] private Transform dogTransform;
    [SerializeField] private SleepController sleepController;
    [SerializeField] private DogStateController dogStateController;
    [SerializeField] private DogBodyShapeManager bodyShapeManager;

    private static bool isDebugCanvasActive = false;

    /// <summary>
    /// デバッグモードが有効かどうか（他システムから参照用）
    /// </summary>
    public static bool IsDebugMode => isDebugCanvasActive;

    // 更新頻度の制御
    private float updateInterval = 0.5f; // 0.5秒ごとに更新
    private float lastUpdateTime;

    private readonly StringBuilder sb = new StringBuilder(1024);

    private void Start()
    {
        // SleepControllerが未設定の場合、シーンから検索を試みる
        if (sleepController == null)
        {
            sleepController = FindObjectOfType<SleepController>();
            if (sleepController == null)
            {
                GameLogger.LogWarning(LogCategory.UI, "[DebugCanvasManager] SleepController が見つかりません。睡眠スケジュール情報は表示されません。");
            }
        }

        // SleepControllerの_autoTestModeがtrueの場合は初期表示
        bool shouldShowInitially = sleepController != null && sleepController.IsAutoTestMode;

        // DebugCanvasを初期状態で表示/非表示
        if (debugCanvas != null)
        {
            debugCanvas.SetActive(shouldShowInitially);
            isDebugCanvasActive = shouldShowInitially;
            if (shouldShowInitially)
            {
                GameLogger.Log(LogCategory.UI, "[DebugCanvasManager] AutoTestMode検出: DebugCanvas を初期表示");
                UpdateDebugInfo();
            }
        }
        else
        {
            GameLogger.LogError(LogCategory.UI, "[DebugCanvasManager] DebugCanvas が設定されていません！");
        }

        // DogTransformが未設定の場合、DogControllerから取得を試みる
        if (dogTransform == null && dogController != null)
        {
            dogTransform = dogController.transform;
        }

        // DogStateControllerが未設定の場合、シーンから検索を試みる
        if (dogStateController == null)
        {
            dogStateController = FindObjectOfType<DogStateController>();
            if (dogStateController == null)
            {
                GameLogger.LogWarning(LogCategory.UI, "[DebugCanvasManager] DogStateController が見つかりません。Demand情報は表示されません。");
            }
        }

        // DogBodyShapeManagerが未設定の場合、シーンから検索を試みる
        if (bodyShapeManager == null)
        {
            bodyShapeManager = FindObjectOfType<DogBodyShapeManager>();
            if (bodyShapeManager == null)
            {
                GameLogger.LogWarning(LogCategory.UI, "[DebugCanvasManager] DogBodyShapeManager が見つかりません。体型情報は表示されません。");
            }
        }

        lastUpdateTime = Time.unscaledTime;
    }

    private void Update()
    {
        // DebugCanvasが表示されている場合、デバッグ情報を更新
        if (isDebugCanvasActive && Time.unscaledTime - lastUpdateTime >= updateInterval)
        {
            UpdateDebugInfo();
            lastUpdateTime = Time.unscaledTime;
        }
    }

    /// <summary>
    /// DebugCanvasの表示/非表示を切り替え
    /// </summary>
    public void ToggleDebugCanvas()
    {
        if (debugCanvas == null) return;

        isDebugCanvasActive = !isDebugCanvasActive;
        debugCanvas.SetActive(isDebugCanvasActive);

        GameLogger.Log(LogCategory.UI, $"[DebugCanvasManager] DebugCanvas {(isDebugCanvasActive ? "表示" : "非表示")}");

        // 表示時は即座に情報を更新
        if (isDebugCanvasActive)
        {
            UpdateDebugInfo();
        }

        // WalkSchedulerに状態変更を通知（散歩ボタンの表示制御）
        WalkScheduler.Instance?.CheckSchedule();
    }

    /// <summary>
    /// デバッグ情報を更新（1つのテキストに全情報を統合）
    /// </summary>
    private void UpdateDebugInfo()
    {
        if (debugText == null) return;

        // 画面サイズに応じたフォントサイズ自動調整
        debugText.fontSize = Mathf.Clamp(Screen.height * 0.018f, 12f, 32f);

        sb.Clear();

        // ── State ──
        sb.AppendLine("── State ──");
        sb.AppendLine($"{GlobalVariables.CurrentState}");

        // ── Position ──
        if (dogTransform != null)
        {
            Vector3 pos = dogTransform.position;
            Vector3 rot = dogTransform.eulerAngles;
            sb.AppendLine();
            sb.AppendLine("── Position ──");
            sb.AppendLine($"Pos: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
            sb.AppendLine($"Rot: ({rot.x:F1}\u00b0, {rot.y:F1}\u00b0, {rot.z:F1}\u00b0)");
        }

        // ── Network ──
        sb.AppendLine();
        sb.AppendLine("── Network ──");
        sb.AppendLine($"{GetNetworkStatus()} | Ping: {GetPingStatus()}");

        // ── Device ──
        sb.AppendLine();
        sb.AppendLine("── Device ──");
        sb.AppendLine(GetDeviceRoleInfo());

        // ── Sleep ──
        sb.AppendLine();
        sb.AppendLine("── Sleep ──");
        sb.AppendLine(GetSleepScheduleInfo());

        // ── Wake ──
        sb.AppendLine();
        sb.AppendLine("── Wake ──");
        sb.AppendLine(GetWakeScheduleInfo());

        // ── Counters ──
        sb.AppendLine();
        sb.AppendLine("── Counters ──");
        sb.AppendLine(GetCountersInfo());

        // ── Personality ──
        sb.AppendLine();
        sb.AppendLine("── Personality ──");
        sb.AppendLine(GetPersonalityInfo());

        // ── Hunger ──
        sb.AppendLine();
        sb.AppendLine("── Hunger ──");
        sb.AppendLine($"{GlobalVariables.CurrentHungerState}");

        // ── Body ──
        sb.AppendLine();
        sb.AppendLine("── Body ──");
        sb.AppendLine(GetBodyShapeInfo());

        // ── Walk ──
        sb.AppendLine();
        sb.AppendLine("── Walk ──");
        sb.AppendLine(GetWalkInfo());

        debugText.text = sb.ToString();
    }

    private string GetNetworkStatus()
    {
        switch (Application.internetReachability)
        {
            case NetworkReachability.NotReachable:
                return "No Connection";
            case NetworkReachability.ReachableViaCarrierDataNetwork:
                return "Mobile Data";
            case NetworkReachability.ReachableViaLocalAreaNetwork:
                return "WiFi Connected";
            default:
                return "N/A";
        }
    }

    private string GetPingStatus()
    {
        return Application.internetReachability == NetworkReachability.NotReachable ? "N/A" : "OK";
    }

    private string GetDeviceRoleInfo()
    {
        if (DogLocationSync.Instance != null)
        {
            var role = DogLocationSync.Instance.CurrentRole;
            bool hasDog = DogLocationSync.Instance.HasDog;
            string roleText = role == DeviceRole.Main ? "MAIN" : "SUB";
            string dogStatus = hasDog ? "犬あり" : "犬なし";

            float remainingSec = DogLocationSync.Instance.RemainingTimeoutSeconds;
            string timeoutInfo = "";
            if (remainingSec >= 0)
            {
                int minutes = (int)(remainingSec / 60);
                int seconds = (int)(remainingSec % 60);
                timeoutInfo = $"\nTimeout: {minutes:D2}:{seconds:D2}";
            }

            return $"{roleText} / {dogStatus}{timeoutInfo}";
        }
        return "N/A";
    }

    private string GetSleepScheduleInfo()
    {
        if (sleepController != null && sleepController.NextScheduledSleepTime.HasValue)
        {
            DateTime nextSleep = sleepController.NextScheduledSleepTime.Value;
            DateTime now = DateTime.Now;
            TimeSpan timeUntilSleep = nextSleep - now;

            string until = timeUntilSleep.TotalSeconds > 0
                ? $"{timeUntilSleep.Hours:D2}:{timeUntilSleep.Minutes:D2}:{timeUntilSleep.Seconds:D2}"
                : "Overdue";

            return $"Next: {nextSleep:MM/dd HH:mm}\nUntil: {until}";
        }
        return "Next: null\nUntil: N/A";
    }

    private string GetWakeScheduleInfo()
    {
        if (sleepController != null && sleepController.NextScheduledWakeTime.HasValue)
        {
            DateTime nextWake = sleepController.NextScheduledWakeTime.Value;
            DateTime now = DateTime.Now;
            TimeSpan timeUntilWake = nextWake - now;

            string until = timeUntilWake.TotalSeconds > 0
                ? $"{timeUntilWake.Hours:D2}:{timeUntilWake.Minutes:D2}:{timeUntilWake.Seconds:D2}"
                : "Overdue";

            return $"Next: {nextWake:MM/dd HH:mm}\nUntil: {until}";
        }
        return "Next: null\nUntil: N/A";
    }

    private string GetCountersInfo()
    {
        int napCount = sleepController != null ? sleepController.CurrentNapCount : 0;

        int demandValue = 0;
        string demandLevel = "N/A";
        if (dogStateController != null && dogStateController.DemandManager != null)
        {
            demandValue = dogStateController.DemandManager.Value;
            demandLevel = dogStateController.DemandManager.CurrentDemandLevel.ToString();
        }

        int loveValue = 0;
        string loveLevel = "N/A";
        if (dogStateController != null && dogStateController.LoveManager != null)
        {
            loveValue = dogStateController.LoveManager.Value;
            loveLevel = dogStateController.LoveManager.CurrentLoveLevel.ToString();
        }

        return $"Attention: {GlobalVariables.AttentionCount} | Nap: {napCount}\n" +
               $"Demand: {demandValue} ({demandLevel})\n" +
               $"Love: {loveValue} ({loveLevel})";
    }

    private string GetPersonalityInfo()
    {
        if (dogStateController != null && dogStateController.Personality != null)
        {
            var p = dogStateController.Personality;
            return $"{p.personalityName}\n" +
                   $"Love: \u2191x{p.loveIncreaseMultiplier:F1} \u2193x{p.loveDecreaseMultiplier:F1}\n" +
                   $"Demand: \u2191x{p.demandIncreaseMultiplier:F1} \u2193x{p.demandDecreaseMultiplier:F1}\n" +
                   $"Hunger: x{p.hungerProgressMultiplier:F1}";
        }
        return "N/A";
    }

    private string GetBodyShapeInfo()
    {
        if (bodyShapeManager != null)
        {
            float scale = bodyShapeManager.CurrentBodyScale;
            BodyShapeLevel level = bodyShapeManager.CurrentBodyShape;
            return $"{level} / Scale: {scale:F3}";
        }
        return "N/A";
    }

    private string GetWalkInfo()
    {
        if (WalkScheduler.Instance == null) return "State: N/A";

        var ws = WalkScheduler.Instance;
        var state = ws.CurrentState;
        string stateLabel = IsDebugMode && state == WalkRequestState.Active
            ? "Active (Debug)" : state.ToString();

        // 次回散歩時間
        DateTime nextStart = ws.GetNextWalkStart();
        DateTime nextEnd = nextStart.AddMinutes(ws.WalkWindowMinutes);
        string schedule = $"Time: {nextStart:HH:mm}-{nextEnd:HH:mm}";

        // 散歩時間までの残り / 散歩ウィンドウの残り
        DateTime now = DateTime.Now;
        string countdown;
        if (state == WalkRequestState.Active || state == WalkRequestState.Walking)
        {
            // ウィンドウ終了までの残り
            TimeSpan remaining = nextEnd - now;
            countdown = $"Window: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }
        else
        {
            // 次回開始までの残り
            TimeSpan untilNext = nextStart - now;
            countdown = untilNext.TotalSeconds > 0
                ? $"Until: {untilNext.Hours:D2}:{untilNext.Minutes:D2}:{untilNext.Seconds:D2}"
                : "Until: --:--:--";
        }

        return $"State: {stateLabel}\n{schedule}\n{countdown}";
    }

    /// <summary>
    /// 外部から明示的にDebugCanvasを表示
    /// </summary>
    public void ShowDebugCanvas()
    {
        if (debugCanvas != null && !isDebugCanvasActive)
        {
            ToggleDebugCanvas();
        }
    }

    /// <summary>
    /// 外部から明示的にDebugCanvasを非表示
    /// </summary>
    public void HideDebugCanvas()
    {
        if (debugCanvas != null && isDebugCanvasActive)
        {
            ToggleDebugCanvas();
        }
    }

    /// <summary>
    /// デバッグ情報を手動で更新
    /// </summary>
    public void ForceUpdateDebugInfo()
    {
        if (isDebugCanvasActive)
        {
            UpdateDebugInfo();
        }
    }
}
