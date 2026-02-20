using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Commands
{
    /// <summary>
    /// 音声コマンドの基底クラス
    /// 新しいコマンドを作成する際はこれを継承
    /// </summary>
    public abstract class VoiceCommandBase : IVoiceCommand
    {
        public abstract string CommandName { get; }
        public abstract string[] Keywords { get; }
        public abstract string Description { get; }
        public virtual int Priority => 0; // デフォルト優先度

        /// <summary>
        /// キーワードマッチング（デフォルト実装: 部分一致）
        /// </summary>
        public virtual string Match(string recognizedText)
        {
            if (string.IsNullOrEmpty(recognizedText))
                return null;

            string normalizedText = NormalizeText(recognizedText);

            foreach (string keyword in Keywords)
            {
                string normalizedKeyword = NormalizeText(keyword);
                
                if (normalizedText.Contains(normalizedKeyword))
                {
                    return keyword; // マッチしたキーワードを返す
                }
            }

            return null;
        }

        /// <summary>
        /// テキストの正規化（空白除去、小文字化など）
        /// </summary>
        protected virtual string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // 空白・タブ・改行を除去
            text = text.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            
            // 小文字化（英語の場合）
            text = text.ToLower();

            return text;
        }

        /// <summary>
        /// コマンド実行
        /// </summary>
        public abstract void Execute(VoiceCommandContext context);

        /// <summary>
        /// コマンドが実行可能かチェック（デフォルト: 常に実行可能）
        /// </summary>
        public virtual bool CanExecute()
        {
            return true;
        }

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        protected void LogExecution(VoiceCommandContext context)
        {
            GameLogger.Log(LogCategory.Voice,$"[Command] 🎯 Executing: {CommandName}");
            GameLogger.Log(LogCategory.Voice,$"[Command]   Matched: '{context.MatchedKeyword}' in '{context.RecognizedText}'");
            GameLogger.Log(LogCategory.Voice,$"[Command]   Confidence: {context.Confidence:F2}");
        }
    }
}
