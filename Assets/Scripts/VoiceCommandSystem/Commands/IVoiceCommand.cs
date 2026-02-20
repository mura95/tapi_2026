namespace VoiceCommandSystem.Commands
{
    /// <summary>
    /// 音声コマンドのコンテキスト情報
    /// </summary>
    public class VoiceCommandContext
    {
        /// <summary>認識されたテキスト全体</summary>
        public string RecognizedText { get; set; }

        /// <summary>マッチしたキーワード</summary>
        public string MatchedKeyword { get; set; }

        /// <summary>信頼度スコア（0.0 - 1.0）</summary>
        public float Confidence { get; set; }

        /// <summary>認識エンジン名</summary>
        public string RecognizerName { get; set; }

        /// <summary>タイムスタンプ</summary>
        public System.DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 音声コマンドのインターフェース
    /// 新しいコマンドを追加する場合はこれを実装
    /// </summary>
    public interface IVoiceCommand
    {
        /// <summary>
        /// コマンド名（デバッグ用）
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// このコマンドを発火させるキーワード（複数可）
        /// 例: new[] { "おすわり", "座れ", "すわれ" }
        /// </summary>
        string[] Keywords { get; }

        /// <summary>
        /// コマンドの説明
        /// </summary>
        string Description { get; }

        /// <summary>
        /// コマンドの優先度（高い方が優先）
        /// 複数のコマンドがマッチした場合に使用
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// キーワードが認識テキストにマッチするかチェック
        /// デフォルトは部分一致だが、カスタム実装も可能
        /// </summary>
        /// <param name="recognizedText">認識されたテキスト</param>
        /// <returns>マッチした場合はキーワード、しない場合はnull</returns>
        string Match(string recognizedText);

        /// <summary>
        /// コマンドを実行
        /// </summary>
        /// <param name="context">コマンド実行のコンテキスト</param>
        void Execute(VoiceCommandContext context);

        /// <summary>
        /// コマンドが現在実行可能かどうか
        /// 例: 犬が特定の状態でないと実行できない、など
        /// </summary>
        bool CanExecute();
    }
}
