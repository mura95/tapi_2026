using UnityEngine;

namespace TapHouse.Logging
{
    /// <summary>
    /// ログ出力の設定を管理するScriptableObject
    /// Assets/Resources/Config/LoggerSettings.asset として配置してください。
    /// </summary>
    [CreateAssetMenu(fileName = "LoggerSettings", menuName = "TapHouse/Config/LoggerSettings")]
    public class LoggerSettings : ScriptableObject
    {
        [Header("グローバル設定")]
        [Tooltip("falseにすると全てのログが停止します")]
        public bool enableAllLogs = true;

        [Tooltip("このレベル以上のログのみ表示されます")]
        public LogLevel minimumLogLevel = LogLevel.Info;

        [Header("表示オプション")]
        [Tooltip("[Sleep] などのカテゴリ名を表示")]
        public bool showCategoryPrefix = true;

        [Tooltip("[INFO] などのログレベルを表示")]
        public bool showLogLevel = true;

        [Tooltip("(FileName.cs:123) のようにファイル情報を表示")]
        public bool showFileInfo = true;

        [Tooltip("HH:mm:ss 形式のタイムスタンプを表示")]
        public bool showTimestamp = false;

        [Header("カテゴリ別設定")]
        [Tooltip("スリープシステム関連のログ")]
        public bool enableSleep = true;

        [Tooltip("音声コマンド関連のログ")]
        public bool enableVoice = true;

        [Tooltip("犬の状態（Love/Demand/Hunger）関連のログ")]
        public bool enableDog = true;

        [Tooltip("Firebase通信関連のログ")]
        public bool enableFirebase = true;

        [Tooltip("おもちゃ遊び関連のログ")]
        public bool enablePlayToy = true;

        [Tooltip("顔認識関連のログ")]
        public bool enableFace = true;

        [Tooltip("UI関連のログ")]
        public bool enableUI = true;

        [Tooltip("アニメーション関連のログ")]
        public bool enableAnimation = true;

        [Tooltip("オーディオ関連のログ")]
        public bool enableAudio = true;

        [Tooltip("その他の一般的なログ")]
        public bool enableGeneral = true;

        /// <summary>
        /// 指定されたカテゴリが有効かどうかを判定
        /// </summary>
        public bool IsCategoryEnabled(LogCategory category)
        {
            return category switch
            {
                LogCategory.Sleep => enableSleep,
                LogCategory.Voice => enableVoice,
                LogCategory.Dog => enableDog,
                LogCategory.Firebase => enableFirebase,
                LogCategory.PlayToy => enablePlayToy,
                LogCategory.Face => enableFace,
                LogCategory.UI => enableUI,
                LogCategory.Animation => enableAnimation,
                LogCategory.Audio => enableAudio,
                LogCategory.General => enableGeneral,
                LogCategory.All => true,
                LogCategory.None => false,
                _ => true
            };
        }

        /// <summary>
        /// 指定されたログレベルが出力対象かどうかを判定
        /// </summary>
        public bool IsLogLevelEnabled(LogLevel level)
        {
            return level >= minimumLogLevel;
        }
    }
}
