/// <summary>
/// PlayerPrefsで使用するすべてのキーを一元管理
/// 新しいキーを追加する場合は必ずここに定義すること
/// </summary>
public static class PrefsKeys
{
    // ============================================
    // 認証・ユーザー情報
    // ============================================

    /// <summary>ユーザーのメールアドレス</summary>
    public const string Email = "Email";

    /// <summary>ユーザーのパスワード（暗号化必須）</summary>
    public const string Password = "Password";

    /// <summary>Firebase UID</summary>
    public const string UserId = "UserId";

    /// <summary>表示名</summary>
    public const string DisplayName = "DisplayName";

    /// <summary>ペットの名前</summary>
    public const string PetName = "petName";

    /// <summary>最終ログイン日時</summary>
    public const string LastLoginTime = "LastLoginTime";

    // ============================================
    // 睡眠スケジュール
    // ============================================

    /// <summary>就寝時刻（0-23）</summary>
    public const string SleepHour = "SleepHour";

    /// <summary>起床時刻（0-23）</summary>
    public const string WakeHour = "WakeHour";

    /// <summary>昼寝開始時刻</summary>
    public const string NapSleepHour = "NapSleepHour";

    /// <summary>昼寝終了時刻</summary>
    public const string NapWakeHour = "NapWakeHour";

    /// <summary>眠気カウンター（0-15）</summary>
    public const string NapCount = "NapCount";

    /// <summary>最終昼寝リセット時刻（ISO 8601形式）</summary>
    public const string LastNapEndTime = "LastNapEndTime";

    /// <summary>次回起床時刻（Unix epoch ms）</summary>
    public const string NextWakeEpochMs = "NextWakeEpochMs";

    // ============================================
    // 空腹・食事
    // ============================================

    /// <summary>空腹状態（HungerState enum値）</summary>
    public const string HungerState = "HungerState";

    /// <summary>最終食事時刻（Unix epoch seconds）</summary>
    public const string LastEatTime = "LastEatTime";

    // ============================================
    // 愛情・インタラクション
    // ============================================

    /// <summary>最終インタラクション時刻（Unix epoch seconds、旧形式Binary DateTimeからの自動移行あり）</summary>
    public const string LastInteractionTime = "LastInteractionTime";

    // ============================================
    // 体型・外見
    // ============================================

    /// <summary>犬の体型スケール</summary>
    public const string DogBodyScale = "DogBodyScale";

    /// <summary>最終空腹チェック時刻（Unix epoch seconds、旧形式Binary DateTimeからの自動移行あり）</summary>
    public const string LastHungryCheck = "LastHungryCheck";

    /// <summary>連続満腹回数</summary>
    public const string ConsecutiveFeedings = "ConsecutiveFeedings";

    /// <summary>犬の毛色（DogCoat enum値）</summary>
    public const string DogCoat = "DogCoat";

    // ============================================
    // タイムゾーン
    // ============================================

    /// <summary>タイムゾーンID（IANA形式）</summary>
    public const string TimezoneId = "timezoneId";

    /// <summary>タイムゾーンドロップダウンのインデックス</summary>
    public const string TimezoneIndex = "TimezoneIndex";

    // ============================================
    // UI・設定
    // ============================================

    /// <summary>タブレットモード（0/1）</summary>
    public const string TabletMode = "TabletMode";

    /// <summary>言語設定（Language enum値）</summary>
    public const string Language = "Language";

    // ============================================
    // マルチデバイス
    // ============================================

    /// <summary>サブ機フラグ（0=メイン機、1=サブ機）</summary>
    public const string IsSubDevice = "IsSubDevice";

    /// <summary>デバイス固有ID</summary>
    public const string DeviceId = "DeviceId";

    // ============================================
    // アプリモード
    // ============================================

    /// <summary>アプリモード（TapHouse/TapPocket）</summary>
    public const string AppMode = "AppMode";
}
