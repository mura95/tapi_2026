using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using TapHouse.Logging;

namespace TapHouse.Reminder
{
    /// <summary>
    /// リマインダー通知システムのメイン制御クラス
    /// - Firebaseからリマインダーデータを読み込み
    /// - 60秒ポーリングで時間チェック
    /// - 通知トリガー時に犬の動作とUI表示を制御
    /// </summary>
    public class ReminderManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DogController _dogController;
        [SerializeField] private TurnAndMoveHandler _turnAndMoveHandler;

        [Header("Audio")]
        [SerializeField] private AudioSource _barkAudioSource;
        [SerializeField] private AudioClip _barkClip;

        [Header("Settings")]
        [SerializeField] private float _checkIntervalSeconds = 60f;
        [SerializeField] private int _reminderToleranceMinutes = 1;
        [SerializeField] private float _timeoutMinutes = 60f;

        // State
        private List<ReminderData> _reminders = new List<ReminderData>();
        private Coroutine _checkCoroutine;
        private Coroutine _timeoutCoroutine;
        private ReminderData _activeReminder;
        private ReminderTime _activeReminderTime;
        private DateTime _activeTriggeredTime;
        private bool _isNotifying = false;
        private ReminderNotificationUI _activeNotificationUI;

        // Firestore
        private string _userId;
        private FirebaseFirestore _firestore;
        private CollectionReference RemindersCollection => _firestore.Collection("users").Document(_userId).Collection("reminders");
        private CollectionReference ReminderLogCollection => _firestore.Collection("users").Document(_userId).Collection("reminderLog");

        void Start()
        {
            try
            {
                var authInstance = FirebaseAuth.DefaultInstance;
                if (authInstance == null)
                {
                    GameLogger.LogError(LogCategory.Firebase, "[ReminderManager] FirebaseAuth not initialized");
                    return;
                }

                var currentUser = authInstance.CurrentUser;
                if (currentUser == null)
                {
                    GameLogger.LogError(LogCategory.General, "[ReminderManager] User not logged in");
                    return;
                }

                _userId = currentUser.UserId;
                _firestore = FirebaseFirestore.DefaultInstance;

                if (_firestore == null)
                {
                    GameLogger.LogError(LogCategory.Firebase, "[ReminderManager] Firestore not initialized");
                    return;
                }

                LoadRemindersOnce();
                StartReminderMonitoring();
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Firebase, $"[ReminderManager] Start failed: {e.Message}");
            }
        }

        /// <summary>
        /// Firestoreからリマインダーを一度だけ読み込み（リトライ付き）
        /// </summary>
        private async void LoadRemindersOnce()
        {
            if (!FirebaseManager.IsConnected)
            {
                GameLogger.Log(LogCategory.Firebase, "[ReminderManager] Offline - using cached reminders");
                return;
            }

            const int maxRetries = 3;
            const int retryDelayMs = 2000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var snapshot = await RemindersCollection.GetSnapshotAsync();
                    ParseReminders(snapshot);
                    return; // 成功したら終了
                }
                catch (Exception e)
                {
                    GameLogger.LogWarning(LogCategory.Firebase,
                        $"[ReminderManager] Failed to load reminders (attempt {attempt}/{maxRetries}): {e.Message}");

                    if (attempt < maxRetries)
                    {
                        await System.Threading.Tasks.Task.Delay(retryDelayMs);
                    }
                }
            }

            GameLogger.LogError(LogCategory.Firebase, "[ReminderManager] Failed to load reminders after all retries");
        }

        private void ParseReminders(QuerySnapshot snapshot)
        {
            _reminders.Clear();

            if (snapshot == null || snapshot.Count == 0)
            {
                GameLogger.Log(LogCategory.General, "[ReminderManager] No reminders found");
                return;
            }

            foreach (var document in snapshot.Documents)
            {
                try
                {
                    var reminder = document.ConvertTo<ReminderData>();
                    if (reminder != null && reminder.enabled)
                    {
                        // ドキュメントIDをidとして設定（Firestoreではドキュメント内にidがない場合）
                        if (string.IsNullOrEmpty(reminder.id))
                        {
                            reminder.id = document.Id;
                        }
                        _reminders.Add(reminder);
                    }
                }
                catch (Exception e)
                {
                    GameLogger.LogWarning(LogCategory.Firebase,
                        $"[ReminderManager] Failed to parse reminder: {e.Message}");
                }
            }

            GameLogger.Log(LogCategory.General,
                $"[ReminderManager] Loaded {_reminders.Count} reminders");
        }

        private void StartReminderMonitoring()
        {
            if (_checkCoroutine != null)
            {
                StopCoroutine(_checkCoroutine);
            }
            _checkCoroutine = StartCoroutine(MonitorReminders());
            GameLogger.Log(LogCategory.General, "[ReminderManager] Monitoring started");
        }

        private IEnumerator MonitorReminders()
        {
            while (true)
            {
                yield return new WaitForSeconds(_checkIntervalSeconds);

                if (!CanTriggerReminder())
                {
                    continue;
                }

                CheckReminders();
            }
        }

        /// <summary>
        /// リマインダーをトリガーできる状態かチェック
        /// </summary>
        private bool CanTriggerReminder()
        {
            var state = GlobalVariables.CurrentState;
            return state != PetState.sleeping &&
                   state != PetState.napping &&
                   state != PetState.reminder;
        }

        private void CheckReminders()
        {
            try
            {
                DateTime now = TimeZoneProvider.Now;
                DayOfWeek today = now.DayOfWeek;
                int currentHour = now.Hour;
                int currentMinute = now.Minute;

                // リマインダーがない場合は早期リターン
                if (_reminders == null || _reminders.Count == 0)
                {
                    return;
                }

                // 優先度順にソート（高い順）
                var sortedReminders = _reminders
                    .Where(r => r != null && r.enabled)
                    .OrderByDescending(r => r.GetEffectivePriority())
                    .ToList();

                foreach (var reminder in sortedReminders)
                {
                    // 曜日チェック
                    if (!reminder.ShouldNotifyOn(today)) continue;

                    // 時間チェック
                    var matchingTime = reminder.GetMatchingTime(currentHour, currentMinute, _reminderToleranceMinutes);
                    if (matchingTime == null) continue;

                    // 今週この曜日・時間に既に通知済みかチェック
                    string key = GetNotificationKey(reminder, matchingTime, today);
                    if (HasNotifiedThisWeek(key)) continue;

                    // 通知トリガー
                    TriggerReminder(reminder, matchingTime);
                    MarkAsNotified(key);
                    return; // 1つずつ通知
                }
            }
            catch (Exception e)
            {
                GameLogger.LogWarning(LogCategory.General, $"[ReminderManager] CheckReminders failed: {e.Message}");
            }
        }

        /// <summary>
        /// 通知済みキーを生成
        /// 週ベースのリマインダーなので、曜日+時間のみで判定
        /// キー形式: Reminder_{id}_{曜日}_{時分}
        /// 例: Reminder_001_1_0900 (月曜9:00)
        /// </summary>
        private string GetNotificationKey(ReminderData reminder, ReminderTime time, DayOfWeek day)
        {
            return $"Reminder_{reminder.id}_{(int)day}_{time.hour:D2}{time.minute:D2}";
        }

        /// <summary>
        /// 今週この曜日・時間に通知済みかチェック
        /// 値には通知した週番号を保存し、同じ週なら通知済みと判定
        /// </summary>
        private bool HasNotifiedThisWeek(string key)
        {
            int savedWeek = PlayerPrefs.GetInt(key, -1);
            int currentWeek = GetCurrentWeekNumber();
            return savedWeek == currentWeek;
        }

        /// <summary>
        /// 通知済みとしてマーク（週番号を保存）
        /// </summary>
        private void MarkAsNotified(string key)
        {
            int currentWeek = GetCurrentWeekNumber();
            PlayerPrefs.SetInt(key, currentWeek);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 現在の週番号を取得（年間通算）
        /// </summary>
        private int GetCurrentWeekNumber()
        {
            DateTime now = TimeZoneProvider.Now;
            // 年 * 100 + 週番号 で一意の週を表現
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int weekOfYear = cal.GetWeekOfYear(now,
                System.Globalization.CalendarWeekRule.FirstDay,
                DayOfWeek.Sunday);
            return now.Year * 100 + weekOfYear;
        }

        /// <summary>
        /// リマインダー通知をトリガー
        /// </summary>
        public void TriggerReminder(ReminderData reminder, ReminderTime time)
        {
            if (_isNotifying) return;

            _isNotifying = true;
            _activeReminder = reminder;
            _activeReminderTime = time;
            _activeTriggeredTime = DateTime.UtcNow;

            // 状態をreminderに変更
            GlobalVariables.CurrentState = PetState.reminder;

            // タイムアウト開始
            _timeoutCoroutine = StartCoroutine(TimeoutWatcher());

            // 通知実行
            StartCoroutine(ExecuteReminderNotification(reminder));

            GameLogger.Log(LogCategory.General,
                $"[ReminderManager] Triggered reminder: {reminder.displayName} at {time}");
        }

        private IEnumerator ExecuteReminderNotification(ReminderData reminder)
        {
            // 1. 犬を中央に移動
            if (_turnAndMoveHandler != null)
            {
                _turnAndMoveHandler.StartTurnAndMove(Vector3.zero, 2.0f);
                yield return new WaitForSeconds(2.5f);
            }
            else
            {
                GameLogger.LogWarning(LogCategory.General, "[ReminderManager] TurnAndMoveHandler is null");
                yield return new WaitForSeconds(0.5f);
            }

            // 2. 吠えアニメーション
            if (_dogController != null)
            {
                _dogController.ActionBark();
            }
            else
            {
                GameLogger.LogWarning(LogCategory.General, "[ReminderManager] DogController is null");
            }

            // 3. 吠え音ループ開始
            StartBarkLoop();

            // 4. 通知UI表示
            bool success = ShowReminderUI(reminder);

            // UI表示に失敗した場合は状態をリセット
            if (!success)
            {
                GameLogger.LogError(LogCategory.UI, "[ReminderManager] Notification failed - resetting state");
                StopBarkLoop();
                ResetReminderState();
            }
        }

        private void StartBarkLoop()
        {
            if (_barkAudioSource != null && _barkClip != null)
            {
                _barkAudioSource.clip = _barkClip;
                _barkAudioSource.loop = true;
                _barkAudioSource.volume = GameAudioSettings.Instance?.BarkVolume ?? 0.5f;
                _barkAudioSource.Play();
            }
        }

        /// <summary>
        /// 吠え音を停止
        /// </summary>
        public void StopBarkLoop()
        {
            if (_barkAudioSource != null && _barkAudioSource.isPlaying)
            {
                _barkAudioSource.Stop();
                _barkAudioSource.loop = false;
            }
        }

        /// <summary>
        /// リマインダーUIを表示
        /// </summary>
        /// <returns>成功した場合true</returns>
        private bool ShowReminderUI(ReminderData reminder)
        {
            // Prefabは Assets/Resources/UI/ReminderNotificationUI に配置すること
            var prefab = Resources.Load<GameObject>("UI/ReminderNotificationUI");
            if (prefab == null)
            {
                GameLogger.LogError(LogCategory.UI, "[ReminderManager] ReminderNotificationUI prefab not found");
                return false;
            }

            var instance = Instantiate(prefab);
            if (instance == null)
            {
                GameLogger.LogError(LogCategory.UI, "[ReminderManager] Failed to instantiate UI prefab");
                return false;
            }

            _activeNotificationUI = instance.GetComponent<ReminderNotificationUI>();
            if (_activeNotificationUI == null)
            {
                GameLogger.LogError(LogCategory.UI, "[ReminderManager] ReminderNotificationUI component not found on prefab");
                Destroy(instance);
                return false;
            }

            _activeNotificationUI.Initialize(this, reminder);
            return true;
        }

        private IEnumerator TimeoutWatcher()
        {
            yield return new WaitForSeconds(_timeoutMinutes * 60f);

            GameLogger.Log(LogCategory.General, "[ReminderManager] Timeout - auto stopping");
            OnReminderTimeout();
        }

        private void OnReminderTimeout()
        {
            StopBarkLoop();
            WriteReminderLog(success: false);
            CloseNotificationUI();
            ResetReminderState();
        }

        /// <summary>
        /// リマインダー状態をリセット（エラー時・タイムアウト時共通）
        /// </summary>
        private void ResetReminderState()
        {
            _isNotifying = false;
            _activeReminder = null;
            _activeReminderTime = null;

            if (GlobalVariables.CurrentState == PetState.reminder)
            {
                GlobalVariables.CurrentState = PetState.idle;
            }
        }

        /// <summary>
        /// リマインダー完了時のコールバック（UIから呼ばれる）
        /// </summary>
        public void OnReminderCompleted()
        {
            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
                _timeoutCoroutine = null;
            }

            StopBarkLoop();
            WriteReminderLog(success: true);
            ResetReminderState();

            GameLogger.Log(LogCategory.General, "[ReminderManager] Reminder completed by user");
        }

        private void CloseNotificationUI()
        {
            if (_activeNotificationUI != null)
            {
                Destroy(_activeNotificationUI.gameObject);
                _activeNotificationUI = null;
            }
        }

        /// <summary>
        /// Firestoreにリマインダーログを書き込み
        /// playLog/feedLogと同等の形式
        /// </summary>
        private async void WriteReminderLog(bool success)
        {
            if (!FirebaseManager.IsConnected || _activeReminder == null || _activeReminderTime == null)
            {
                return;
            }

            var log = new ReminderLog(_activeReminder, _activeReminderTime, _activeTriggeredTime, success);
            int responseTime = log.responseTimeSeconds;

            try
            {
                await ReminderLogCollection.AddAsync(log);

                GameLogger.Log(LogCategory.Firebase,
                    $"[ReminderManager] Log written: success={success}, responseTime={responseTime}s");
            }
            catch (Exception e)
            {
                GameLogger.LogWarning(LogCategory.Firebase,
                    $"[ReminderManager] Failed to write log: {e.Message}");
            }
        }

        void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                // アプリ復帰時にリマインダーを再読み込み
                LoadRemindersOnce();

                // 通知中でなければチェック
                if (!_isNotifying)
                {
                    CheckReminders();
                }
            }
        }

        void OnDestroy()
        {
            if (_checkCoroutine != null)
            {
                StopCoroutine(_checkCoroutine);
            }

            if (_timeoutCoroutine != null)
            {
                StopCoroutine(_timeoutCoroutine);
            }

            StopBarkLoop();
        }

        #region Debug / Test Methods

        /// <summary>
        /// テスト用：服薬リマインダーを表示
        /// </summary>
        [ContextMenu("Test Show Medication Reminder")]
        public void TestShowMedicationReminder()
        {
            var testReminder = CreateTestReminder(ReminderType.Medication, "服薬テスト");
            var testTime = new ReminderTime { hour = DateTime.Now.Hour, minute = DateTime.Now.Minute };
            TriggerReminder(testReminder, testTime);
        }

        /// <summary>
        /// テスト用：食事リマインダーを表示
        /// </summary>
        [ContextMenu("Test Show Meal Reminder")]
        public void TestShowMealReminder()
        {
            var testReminder = CreateTestReminder(ReminderType.Meal, "食事テスト");
            var testTime = new ReminderTime { hour = DateTime.Now.Hour, minute = DateTime.Now.Minute };
            TriggerReminder(testReminder, testTime);
        }

        /// <summary>
        /// テスト用：水分補給リマインダーを表示
        /// </summary>
        [ContextMenu("Test Show Hydration Reminder")]
        public void TestShowHydrationReminder()
        {
            var testReminder = CreateTestReminder(ReminderType.Hydration, "水分補給テスト");
            var testTime = new ReminderTime { hour = DateTime.Now.Hour, minute = DateTime.Now.Minute };
            TriggerReminder(testReminder, testTime);
        }

        /// <summary>
        /// テスト用：通知を強制終了
        /// </summary>
        [ContextMenu("Test Force Stop Reminder")]
        public void TestForceStopReminder()
        {
            OnReminderTimeout();
            GameLogger.Log(LogCategory.General, "[ReminderManager] Test: Force stopped reminder");
        }

        private ReminderData CreateTestReminder(ReminderType type, string displayName)
        {
            return new ReminderData
            {
                id = "test_" + Guid.NewGuid().ToString().Substring(0, 8),
                type = type.ToString().ToLower(),
                displayName = displayName,
                enabled = true
            };
        }

        #endregion
    }
}
