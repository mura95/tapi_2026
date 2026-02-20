using UnityEngine;
using System;

namespace TapHouse.MetaverseWalk.Core
{
    /// <summary>
    /// 散歩スケジュールを管理
    /// 設定された時間になると散歩要求状態になる
    /// </summary>
    public class WalkScheduler : MonoBehaviour
    {
        [Header("スケジュール設定")]
        [SerializeField] private int walkHour = 10;
        [SerializeField] private int walkMinute = 0;
        [SerializeField] private int walkWindowMinutes = 60;

        // 公開プロパティ（DebugCanvas等から参照）
        public int WalkHour => walkHour;
        public int WalkMinute => walkMinute;
        public int WalkWindowMinutes => walkWindowMinutes;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private const string LAST_WALK_DATE_KEY = "MetaverseWalk_LastWalkDate";

        public WalkRequestState CurrentState { get; private set; } = WalkRequestState.Inactive;

        public event Action<WalkRequestState> OnStateChanged;

        public static WalkScheduler Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            CheckSchedule();
        }

        /// <summary>
        /// スケジュールをチェックして状態を更新
        /// </summary>
        public void CheckSchedule()
        {
            // DebugCanvasが表示中なら強制アクティブ
            if (DebugCanvasManager.IsDebugMode)
            {
                SetState(WalkRequestState.Active);
                return;
            }

            // 本日すでに散歩済みかチェック
            if (HasWalkedToday())
            {
                SetState(WalkRequestState.Completed);
                return;
            }

            // 現在時刻が散歩時間内かチェック
            if (IsWalkTime())
            {
                SetState(WalkRequestState.Active);
            }
            else
            {
                SetState(WalkRequestState.Inactive);
            }
        }

        /// <summary>
        /// 現在が散歩時間内かどうか
        /// </summary>
        public bool IsWalkTime()
        {
            DateTime now = TimeZoneProvider.Now;
            DateTime walkStart = new DateTime(now.Year, now.Month, now.Day, walkHour, walkMinute, 0);
            DateTime walkEnd = walkStart.AddMinutes(walkWindowMinutes);

            return now >= walkStart && now < walkEnd;
        }

        /// <summary>
        /// 本日すでに散歩したかどうか
        /// 現在未使用（散歩回数制限なし）。将来の回数制限機能用に保持。
        /// </summary>
        public bool HasWalkedToday()
        {
            string lastWalkDate = PlayerPrefs.GetString(LAST_WALK_DATE_KEY, "");
            string today = TimeZoneProvider.Now.Date.ToString("yyyy-MM-dd");
            return lastWalkDate == today;
        }

        /// <summary>
        /// 散歩完了をマーク
        /// 現在未使用（散歩回数制限なし）。将来の回数制限機能用に保持。
        /// </summary>
        public void MarkWalkCompleted()
        {
            string today = TimeZoneProvider.Now.Date.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(LAST_WALK_DATE_KEY, today);
            PlayerPrefs.Save();
            SetState(WalkRequestState.Completed);

            if (debugMode)
            {
                Debug.Log("[WalkScheduler] Walk completed and marked");
            }
        }

        /// <summary>
        /// 散歩開始
        /// </summary>
        public void StartWalk()
        {
            SetState(WalkRequestState.Walking);

            if (debugMode)
            {
                Debug.Log("[WalkScheduler] Walk started");
            }
        }

        /// <summary>
        /// 状態をリセット（デバッグ用）
        /// </summary>
        public void ResetState()
        {
            PlayerPrefs.DeleteKey(LAST_WALK_DATE_KEY);
            PlayerPrefs.Save();
            SetState(WalkRequestState.Inactive);
            CheckSchedule();

            if (debugMode)
            {
                Debug.Log("[WalkScheduler] State reset");
            }
        }

        /// <summary>
        /// 次回の散歩開始時刻を取得
        /// 今日のウィンドウが終了済みなら明日の開始時刻を返す
        /// </summary>
        public DateTime GetNextWalkStart()
        {
            DateTime now = TimeZoneProvider.Now;
            DateTime todayStart = new DateTime(now.Year, now.Month, now.Day, walkHour, walkMinute, 0);
            DateTime todayEnd = todayStart.AddMinutes(walkWindowMinutes);
            return now >= todayEnd ? todayStart.AddDays(1) : todayStart;
        }

        private void SetState(WalkRequestState newState)
        {
            if (CurrentState != newState)
            {
                if (debugMode)
                {
                    Debug.Log($"[WalkScheduler] State: {CurrentState} -> {newState}");
                }

                CurrentState = newState;
                OnStateChanged?.Invoke(newState);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }

    /// <summary>
    /// 散歩要求の状態
    /// </summary>
    public enum WalkRequestState
    {
        Inactive,   // 散歩時間外
        Active,     // 散歩要求中（ボタン表示）
        Walking,    // 散歩中
        Completed   // 本日の散歩完了
    }
}
