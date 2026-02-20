using System;
using System.Collections.Generic;
using Firebase.Firestore;

namespace TapHouse.Reminder
{
    /// <summary>
    /// リマインダーの時間データ
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class ReminderTime
    {
        [FirestoreProperty("hour")]
        public int hour { get; set; }

        [FirestoreProperty("minute")]
        public int minute { get; set; }

        public ReminderTime() { }

        public ReminderTime(int hour, int minute)
        {
            this.hour = hour;
            this.minute = minute;
        }

        public override string ToString()
        {
            return $"{hour:D2}:{minute:D2}";
        }
    }

    /// <summary>
    /// リマインダーデータ（Firestoreから読み込む）
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class ReminderData
    {
        [FirestoreProperty("id")]
        public string id { get; set; }

        [FirestoreProperty("type")]
        public string type { get; set; }

        [FirestoreProperty("displayName")]
        public string displayName { get; set; }

        [FirestoreProperty("times")]
        public List<ReminderTime> times { get; set; }

        [FirestoreProperty("daysOfWeek")]
        public List<int> daysOfWeek { get; set; }

        [FirestoreProperty("enabled")]
        public bool enabled { get; set; }

        [FirestoreProperty("createdAt")]
        public long createdAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public long updatedAt { get; set; }

        /// <summary>
        /// 優先度（数値が大きいほど優先）
        /// 同時刻に複数のリマインダーがある場合、優先度が高いものを先に通知
        /// デフォルト: タイプに応じた優先度を使用
        /// </summary>
        [FirestoreProperty("priority")]
        public int priority { get; set; } = -1;  // -1 = 未設定（タイプのデフォルトを使用）

        /// <summary>
        /// 実効優先度を取得（設定値またはタイプのデフォルト）
        /// </summary>
        public int GetEffectivePriority()
        {
            if (priority >= 0) return priority;

            // タイプに応じたデフォルト優先度
            return GetReminderType() switch
            {
                ReminderType.Medication => 100,   // 服薬: 最高優先
                ReminderType.Meal => 80,          // 食事
                ReminderType.Hydration => 60,     // 水分補給
                ReminderType.Appointment => 50,   // 予定
                ReminderType.Exercise => 40,      // 運動
                ReminderType.Rest => 20,          // 休憩
                _ => 0
            };
        }

        /// <summary>
        /// typeをReminderType enumに変換
        /// </summary>
        public ReminderType GetReminderType()
        {
            if (Enum.TryParse<ReminderType>(type, true, out var result))
            {
                return result;
            }
            return ReminderType.Medication;
        }

        /// <summary>
        /// 指定した曜日に通知するかチェック
        /// </summary>
        /// <param name="day">曜日（DayOfWeek）</param>
        /// <returns>通知する場合true</returns>
        public bool ShouldNotifyOn(DayOfWeek day)
        {
            // 空配列またはnullの場合は毎日通知
            if (daysOfWeek == null || daysOfWeek.Count == 0)
            {
                return true;
            }

            // DayOfWeekは日曜=0, 月曜=1, ..., 土曜=6
            return daysOfWeek.Contains((int)day);
        }

        /// <summary>
        /// 指定した時間が通知対象かチェック
        /// </summary>
        /// <param name="hour">時</param>
        /// <param name="minute">分</param>
        /// <param name="toleranceMinutes">許容範囲（分）</param>
        /// <returns>一致した時間、なければnull</returns>
        public ReminderTime GetMatchingTime(int hour, int minute, int toleranceMinutes = 1)
        {
            if (times == null) return null;

            foreach (var time in times)
            {
                int scheduledMinutes = time.hour * 60 + time.minute;
                int currentMinutes = hour * 60 + minute;
                int diff = currentMinutes - scheduledMinutes;

                // 許容範囲内（0〜toleranceMinutes分後）
                if (diff >= 0 && diff <= toleranceMinutes)
                {
                    return time;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// リマインダーログ（Firestoreに書き込む）
    /// playLog/feedLogと同等の形式
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class ReminderLog
    {
        [FirestoreProperty("reminderId")]
        public string reminderId { get; set; }

        [FirestoreProperty("type")]
        public string type { get; set; }

        [FirestoreProperty("scheduledHour")]
        public int scheduledHour { get; set; }

        [FirestoreProperty("scheduledMinute")]
        public int scheduledMinute { get; set; }

        [FirestoreProperty("triggeredAt")]
        public Timestamp triggeredAt { get; set; }

        [FirestoreProperty("completedAt")]
        public Timestamp completedAt { get; set; }

        [FirestoreProperty("responseTimeSeconds")]
        public int responseTimeSeconds { get; set; }

        [FirestoreProperty("success")]
        public bool success { get; set; }

        public ReminderLog() { }

        public ReminderLog(ReminderData reminder, ReminderTime time, DateTime triggeredTime, bool success)
        {
            this.reminderId = reminder.id;
            this.type = reminder.type;
            this.scheduledHour = time.hour;
            this.scheduledMinute = time.minute;
            this.triggeredAt = Timestamp.FromDateTime(triggeredTime.ToUniversalTime());
            this.completedAt = Timestamp.FromDateTime(DateTime.UtcNow);
            this.responseTimeSeconds = (int)(DateTime.UtcNow - triggeredTime.ToUniversalTime()).TotalSeconds;
            this.success = success;
        }
    }
}
