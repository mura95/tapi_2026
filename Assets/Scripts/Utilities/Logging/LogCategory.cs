using System;

namespace TapHouse.Logging
{
    /// <summary>
    /// ログのカテゴリを定義するFlags列挙型
    /// 新しいカテゴリを追加する場合は、ここに追加し、
    /// LoggerSettingsにも対応するフィールドを追加してください。
    /// </summary>
    [Flags]
    public enum LogCategory
    {
        None        = 0,
        Sleep       = 1 << 0,   // スリープシステム全般
        Voice       = 1 << 1,   // 音声コマンド
        Dog         = 1 << 2,   // 犬の状態（Love/Demand/Hunger）
        Firebase    = 1 << 3,   // Firebase通信
        PlayToy     = 1 << 4,   // おもちゃ遊び
        Face        = 1 << 5,   // 顔認識
        UI          = 1 << 6,   // UI関連
        Animation   = 1 << 7,   // アニメーション
        Audio       = 1 << 8,   // オーディオ
        General     = 1 << 9,   // その他
        All         = ~0
    }

    /// <summary>
    /// ログの重要度レベル
    /// </summary>
    public enum LogLevel
    {
        Info = 0,       // 通常の情報（開発時のみ見たい）
        Warning = 1,    // 注意が必要（想定外だが動作は継続）
        Error = 2       // エラー（問題が発生）
    }
}
