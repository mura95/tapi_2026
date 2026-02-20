namespace TapHouse.MetaverseWalk.Network
{
    /// <summary>
    /// マルチプレイヤーネットワーク機能で使用する定数
    /// </summary>
    public static class NetworkConstants
    {
        // ルーム設定
        public const int MAX_PLAYERS_PER_ROOM = 10;       // 仕様: 最大10人/ルーム
        public const int TICK_RATE = 30;
        public const string FIXED_REGION = "jp";

        // 自動マッチング
        public const int MAX_ROOM_JOIN_ATTEMPTS = 5;
        public const int ROOM_TIME_BUCKET_MINUTES = 30;

        // タイムアウト・リトライ
        public const int ROOM_TTL_MS = 300000;           // 空ルームの保持時間（5分）
        public const int PLAYER_TTL_MS = 60000;           // 再接続猶予（1分）
        public const int MAX_RECONNECT_ATTEMPTS = 3;
        public const int RECONNECT_BASE_INTERVAL_MS = 2000;  // 指数バックオフの基本間隔
        public const int CONNECTION_TIMEOUT_MS = 30000;       // 接続タイムアウト（30秒）
        public const int BACKGROUND_REJOIN_THRESHOLD_SEC = 30; // バックグラウンド復帰の閾値（秒）

        // 同期精度
        public const float POSITION_ACCURACY = 0.01f;
        public const float ROTATION_ACCURACY = 1f;

        // 位置同期
        public const float POSITION_INTERPOLATION_SPEED = 10f;  // リモートプレイヤー補間速度
        public const float ROTATION_INTERPOLATION_SPEED = 10f;

        // プレイヤー名
        public const int MAX_PLAYER_NAME_LENGTH = 10;

        // UI
        public const float NAME_TAG_MAX_DISTANCE = 15f;
        public const float NAME_TAG_HEIGHT_OFFSET = 2f;

        // ルーム名プレフィックス
        public const string ROOM_NAME_PREFIX = "WalkRoom_";
    }
}
