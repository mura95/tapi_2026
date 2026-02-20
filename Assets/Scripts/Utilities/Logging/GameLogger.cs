using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace TapHouse.Logging
{
    /// <summary>
    /// カテゴリ・ログレベル・ファイル情報対応のロガー
    ///
    /// 使用例:
    /// GameLogger.Log(LogCategory.Sleep, "スリープ開始");
    /// GameLogger.Log(LogCategory.Sleep, LogLevel.Warning, "予定より遅れて起床");
    /// GameLogger.LogError(LogCategory.Firebase, "接続に失敗しました");
    /// </summary>
    public static class GameLogger
    {
        private static LoggerSettings _settings;
        private static bool _initialized;

        /// <summary>
        /// ロガーを初期化します。
        /// ゲーム起動時に一度呼び出してください。
        /// </summary>
        public static void Initialize(LoggerSettings settings)
        {
            _settings = settings;
            _initialized = true;
        }

        /// <summary>
        /// Resources/Config/LoggerSettings からロードして初期化します。
        /// </summary>
        public static void InitializeFromResources()
        {
            var settings = Resources.Load<LoggerSettings>("Config/LoggerSettings");
            if (settings == null)
            {
                Debug.LogWarning("[GameLogger] LoggerSettings not found in Resources/Config/. Using default settings.");
                settings = ScriptableObject.CreateInstance<LoggerSettings>();
            }
            Initialize(settings);
        }

        /// <summary>
        /// 設定が初期化されていない場合に自動初期化を試みる
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                InitializeFromResources();
            }
        }

        /// <summary>
        /// Infoレベルのログを出力
        /// </summary>
        public static void Log(
            LogCategory category,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Log(category, LogLevel.Info, message, filePath, lineNumber);
        }

        /// <summary>
        /// 指定レベルのログを出力
        /// </summary>
        public static void Log(
            LogCategory category,
            LogLevel level,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            EnsureInitialized();

            if (_settings == null || !_settings.enableAllLogs) return;
            if (!_settings.IsCategoryEnabled(category)) return;
            if (!_settings.IsLogLevelEnabled(level)) return;

            string formattedMessage = FormatMessage(category, level, message, filePath, lineNumber);
            OutputLog(level, formattedMessage);
        }

        /// <summary>
        /// Warningレベルのログを出力
        /// </summary>
        public static void LogWarning(
            LogCategory category,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Log(category, LogLevel.Warning, message, filePath, lineNumber);
        }

        /// <summary>
        /// Errorレベルのログを出力
        /// </summary>
        public static void LogError(
            LogCategory category,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Log(category, LogLevel.Error, message, filePath, lineNumber);
        }

        /// <summary>
        /// 例外をログ出力（Debug.LogExceptionの代替）
        /// </summary>
        public static void LogException(
            LogCategory category,
            Exception exception,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (exception == null) return;

            string message = $"{exception.GetType().Name}: {exception.Message}";
            if (exception.StackTrace != null)
            {
                message += $"\n{exception.StackTrace}";
            }

            // InnerExceptionがある場合は追加
            if (exception.InnerException != null)
            {
                message += $"\n--- Inner Exception ---\n{exception.InnerException.GetType().Name}: {exception.InnerException.Message}";
            }

            Log(category, LogLevel.Error, message, filePath, lineNumber);
        }

        /// <summary>
        /// 例外をログ出力（追加メッセージ付き）
        /// </summary>
        public static void LogException(
            LogCategory category,
            Exception exception,
            string additionalMessage,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (exception == null) return;

            string message = $"{additionalMessage} - {exception.GetType().Name}: {exception.Message}";
            if (exception.StackTrace != null)
            {
                message += $"\n{exception.StackTrace}";
            }

            Log(category, LogLevel.Error, message, filePath, lineNumber);
        }

        /// <summary>
        /// メッセージをフォーマット
        /// </summary>
        private static string FormatMessage(
            LogCategory category,
            LogLevel level,
            string message,
            string filePath,
            int lineNumber)
        {
            var parts = new System.Text.StringBuilder();

            // タイムスタンプ
            if (_settings.showTimestamp)
            {
                parts.Append($"{DateTime.Now:HH:mm:ss} ");
            }

            // カテゴリ
            if (_settings.showCategoryPrefix)
            {
                parts.Append($"[{category}]");
            }

            // ログレベル
            if (_settings.showLogLevel && level != LogLevel.Info)
            {
                parts.Append($"[{level.ToString().ToUpper()}]");
            }

            // スペース追加
            if (parts.Length > 0)
            {
                parts.Append(" ");
            }

            // メッセージ本文
            parts.Append(message);

            // ファイル情報
            if (_settings.showFileInfo && !string.IsNullOrEmpty(filePath))
            {
                string fileName = Path.GetFileName(filePath);
                parts.Append($" ({fileName}:{lineNumber})");
            }

            return parts.ToString();
        }

        /// <summary>
        /// ログレベルに応じたDebug.Logを呼び出し
        /// </summary>
        private static void OutputLog(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Info:
                    Debug.Log(message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
            }
        }

        #region Conditional Methods for Performance

        /// <summary>
        /// 条件付きログ - 条件がtrueの場合のみ出力
        /// </summary>
        public static void LogIf(
            bool condition,
            LogCategory category,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (condition)
            {
                Log(category, LogLevel.Info, message, filePath, lineNumber);
            }
        }

        /// <summary>
        /// 条件付きログ - 条件がtrueの場合のみ出力（レベル指定）
        /// </summary>
        public static void LogIf(
            bool condition,
            LogCategory category,
            LogLevel level,
            string message,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (condition)
            {
                Log(category, level, message, filePath, lineNumber);
            }
        }

        #endregion
    }
}
