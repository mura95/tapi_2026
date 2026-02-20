using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;

namespace TapHouse.MetaverseWalk.Network
{
    /// <summary>
    /// メタバース散歩のエラーコード定義とメッセージマッピング
    /// 高齢者向け: 技術用語を使わず平易な日本語で表示
    /// </summary>
    public enum MetaverseErrorCode
    {
        None = 0,
        E001_NoNetwork,         // ネットワーク未接続
        E002_RoomSearchTimeout, // ルーム検索タイムアウト
        E003_RoomFull,          // ルーム満員
        E004_Disconnected,      // 接続中の切断
        E005_MicPermission,     // マイク権限なし（M3向け予約）
        E006_ServerError,       // Photonサーバーエラー
        E007_BackgroundTimeout, // バックグラウンド復帰タイムアウト
        E008_ReconnectFailed,   // 再接続失敗（3回リトライ後）
    }

    /// <summary>
    /// エラー情報をまとめた構造体
    /// </summary>
    public struct MetaverseError
    {
        public MetaverseErrorCode Code;
        public string UserMessage;     // ユーザーに表示するメッセージ（平易な日本語）
        public string DebugMessage;    // デバッグログ用の詳細情報
        public bool CanRetry;          // リトライボタンを表示するか
        public bool AutoReturn;        // 自動でメインシーンに戻るか

        public override string ToString() => $"[{Code}] {UserMessage} (debug: {DebugMessage})";
    }

    /// <summary>
    /// エラーハンドリングの一元管理
    /// ShutdownReason → MetaverseError への変換、エラーイベントの発行を行う
    /// </summary>
    public static class MetaverseErrorHandler
    {
        /// <summary>
        /// エラー発生時のイベント（UIが購読して表示する）
        /// </summary>
        public static event Action<MetaverseError> OnError;

        /// <summary>
        /// エラーをクリアした時のイベント
        /// </summary>
        public static event Action OnErrorCleared;

        /// <summary>
        /// 直近のエラー（デバッグ用に保持）
        /// </summary>
        public static MetaverseError? LastError { get; private set; }

        /// <summary>
        /// エラーコードからユーザー向けエラー情報を生成
        /// </summary>
        public static MetaverseError CreateError(MetaverseErrorCode code, string debugDetail = null)
        {
            var error = code switch
            {
                MetaverseErrorCode.E001_NoNetwork => new MetaverseError
                {
                    Code = code,
                    UserMessage = "インターネットに接続してください",
                    DebugMessage = debugDetail ?? "Network unreachable",
                    CanRetry = true,
                    AutoReturn = false,
                },
                MetaverseErrorCode.E002_RoomSearchTimeout => new MetaverseError
                {
                    Code = code,
                    UserMessage = "うまくつながりませんでした\nもう一度お試しください",
                    DebugMessage = debugDetail ?? "Room search timeout",
                    CanRetry = true,
                    AutoReturn = false,
                },
                MetaverseErrorCode.E003_RoomFull => new MetaverseError
                {
                    Code = code,
                    UserMessage = "ただいま混み合っています\nしばらくお待ちください",
                    DebugMessage = debugDetail ?? "All rooms full",
                    CanRetry = true,
                    AutoReturn = false,
                },
                MetaverseErrorCode.E004_Disconnected => new MetaverseError
                {
                    Code = code,
                    UserMessage = "つながらなくなりました\nもう一度つなぎなおしています...",
                    DebugMessage = debugDetail ?? "Connection lost",
                    CanRetry = false,  // 自動再接続を試みるのでリトライボタンは不要
                    AutoReturn = false,
                },
                MetaverseErrorCode.E005_MicPermission => new MetaverseError
                {
                    Code = code,
                    UserMessage = "マイクが使えません\n設定からマイクを許可してください",
                    DebugMessage = debugDetail ?? "Microphone permission denied",
                    CanRetry = false,
                    AutoReturn = false,
                },
                MetaverseErrorCode.E006_ServerError => new MetaverseError
                {
                    Code = code,
                    UserMessage = "ただいまお休み中です\nしばらくしてからお試しください",
                    DebugMessage = debugDetail ?? "Photon server error",
                    CanRetry = false,
                    AutoReturn = true,
                },
                MetaverseErrorCode.E007_BackgroundTimeout => new MetaverseError
                {
                    Code = code,
                    UserMessage = "しばらく離れていたため\nお散歩を終了しました",
                    DebugMessage = debugDetail ?? $"Background timeout (>{NetworkConstants.BACKGROUND_REJOIN_THRESHOLD_SEC}s)",
                    CanRetry = false,
                    AutoReturn = true,
                },
                MetaverseErrorCode.E008_ReconnectFailed => new MetaverseError
                {
                    Code = code,
                    UserMessage = "つなぎなおせませんでした\nお部屋にもどります",
                    DebugMessage = debugDetail ?? $"Reconnect failed after {NetworkConstants.MAX_RECONNECT_ATTEMPTS} attempts",
                    CanRetry = false,
                    AutoReturn = true,
                },
                _ => new MetaverseError
                {
                    Code = code,
                    UserMessage = "うまくいきませんでした",
                    DebugMessage = debugDetail ?? "Unknown error",
                    CanRetry = true,
                    AutoReturn = false,
                }
            };

            return error;
        }

        /// <summary>
        /// Photon ShutdownReason → MetaverseErrorCode 変換
        /// </summary>
        public static MetaverseErrorCode FromShutdownReason(ShutdownReason reason)
        {
            return reason switch
            {
                ShutdownReason.Ok => MetaverseErrorCode.None,
                ShutdownReason.GameClosed => MetaverseErrorCode.None,
                ShutdownReason.ServerInRoom => MetaverseErrorCode.E006_ServerError,
                ShutdownReason.GameNotFound => MetaverseErrorCode.E002_RoomSearchTimeout,
                ShutdownReason.MaxCcuReached => MetaverseErrorCode.E003_RoomFull,
                ShutdownReason.GameIdAlreadyExists => MetaverseErrorCode.E002_RoomSearchTimeout,
                ShutdownReason.OperationTimeout => MetaverseErrorCode.E002_RoomSearchTimeout,
                ShutdownReason.OperationCanceled => MetaverseErrorCode.None,
                _ => MetaverseErrorCode.E004_Disconnected,
            };
        }

        /// <summary>
        /// Photon NetConnectFailedReason → MetaverseErrorCode 変換
        /// </summary>
        public static MetaverseErrorCode FromConnectFailedReason(NetConnectFailedReason reason)
        {
            return reason switch
            {
                NetConnectFailedReason.Timeout => MetaverseErrorCode.E002_RoomSearchTimeout,
                NetConnectFailedReason.ServerFull => MetaverseErrorCode.E003_RoomFull,
                NetConnectFailedReason.ServerRefused => MetaverseErrorCode.E006_ServerError,
                _ => MetaverseErrorCode.E004_Disconnected,
            };
        }

        /// <summary>
        /// エラーを発行（UIに通知 + デバッグログ出力）
        /// </summary>
        public static void RaiseError(MetaverseErrorCode code, string debugDetail = null)
        {
            var error = CreateError(code, debugDetail);
            LastError = error;

            Debug.LogWarning($"[MetaverseError] {error}");
            OnError?.Invoke(error);
        }

        /// <summary>
        /// エラー状態をクリア
        /// </summary>
        public static void ClearError()
        {
            LastError = null;
            OnErrorCleared?.Invoke();
        }

        /// <summary>
        /// ネットワーク到達可能性チェック
        /// </summary>
        public static bool IsNetworkAvailable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
