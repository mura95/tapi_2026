namespace TapHouse.Reminder
{
    /// <summary>
    /// リマインダーの種類を定義
    /// 新しいリマインダータイプを追加する場合はここに追加
    /// </summary>
    public enum ReminderType
    {
        /// <summary>服薬</summary>
        Medication,

        /// <summary>食事</summary>
        Meal,

        /// <summary>水分補給</summary>
        Hydration,

        /// <summary>運動・体操</summary>
        Exercise,

        /// <summary>睡眠・休憩</summary>
        Rest,

        /// <summary>通院・予定</summary>
        Appointment
    }
}
