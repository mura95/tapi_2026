using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Commands
{
    /// <summary>
    /// 音声コマンドの登録と管理
    /// コマンドのマッチングと実行を担当
    /// </summary>
    public class VoiceCommandRegistry
    {
        private List<IVoiceCommand> commands = new List<IVoiceCommand>();
        private bool caseSensitive = false;

        /// <summary>
        /// コマンドを登録
        /// </summary>
        public void RegisterCommand(IVoiceCommand command)
        {
            if (command == null)
            {
                GameLogger.LogError(LogCategory.Voice,"[CommandRegistry] Cannot register null command");
                return;
            }

            commands.Add(command);
            GameLogger.Log(LogCategory.Voice,$"[CommandRegistry] Registered: {command.CommandName} (keywords: {string.Join(", ", command.Keywords)})");
        }

        /// <summary>
        /// 複数のコマンドを一括登録
        /// </summary>
        public void RegisterCommands(params IVoiceCommand[] commandsToRegister)
        {
            foreach (var command in commandsToRegister)
            {
                RegisterCommand(command);
            }
        }

        /// <summary>
        /// コマンドを解除
        /// </summary>
        public bool UnregisterCommand(IVoiceCommand command)
        {
            if (commands.Remove(command))
            {
                GameLogger.Log(LogCategory.Voice,$"[CommandRegistry] Unregistered: {command.CommandName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// すべてのコマンドをクリア
        /// </summary>
        public void ClearCommands()
        {
            commands.Clear();
            GameLogger.Log(LogCategory.Voice,"[CommandRegistry] All commands cleared");
        }

        /// <summary>
        /// 認識テキストからコマンドを検索して実行
        /// </summary>
        /// <param name="recognizedText">認識されたテキスト</param>
        /// <param name="recognizerName">使用された認識エンジン名</param>
        /// <param name="confidence">信頼度スコア</param>
        /// <returns>実行されたコマンド数</returns>
        public int ExecuteMatchingCommands(string recognizedText, string recognizerName = "Unknown", float confidence = 1.0f)
        {
            if (string.IsNullOrEmpty(recognizedText))
            {
                GameLogger.LogWarning(LogCategory.Voice,"[CommandRegistry] Empty recognized text");
                return 0;
            }

            GameLogger.Log(LogCategory.Voice,$"[CommandRegistry] Processing: '{recognizedText}'");

            // マッチするコマンドを検索
            var matchedCommands = FindMatchingCommands(recognizedText);

            if (matchedCommands.Count == 0)
            {
                GameLogger.Log(LogCategory.Voice,$"[CommandRegistry] ❌ No matching commands for: '{recognizedText}'");
                return 0;
            }

            // 優先度順にソート
            matchedCommands.Sort((a, b) => b.command.Priority.CompareTo(a.command.Priority));

            int executedCount = 0;

            // コマンドを実行
            foreach (var (command, matchedKeyword) in matchedCommands)
            {
                if (!command.CanExecute())
                {
                    GameLogger.LogWarning(LogCategory.Voice,$"[CommandRegistry] ⚠️ Cannot execute: {command.CommandName} (not in valid state)");
                    continue;
                }

                // コンテキスト作成
                var context = new VoiceCommandContext
                {
                    RecognizedText = recognizedText,
                    MatchedKeyword = matchedKeyword,
                    Confidence = confidence,
                    RecognizerName = recognizerName,
                    Timestamp = System.DateTime.Now
                };

                try
                {
                    command.Execute(context);
                    executedCount++;

                    // 最初の1つのみ実行する場合はここでbreak
                    // break;
                }
                catch (System.Exception e)
                {
                    GameLogger.LogError(LogCategory.Voice,$"[CommandRegistry] Command execution failed: {command.CommandName}");
                    GameLogger.LogError(LogCategory.Voice,$"[CommandRegistry] Error: {e.Message}");
                }
            }

            if (executedCount > 0)
            {
                GameLogger.Log(LogCategory.Voice,$"[CommandRegistry] ✅ Executed {executedCount} command(s)");
            }

            return executedCount;
        }

        /// <summary>
        /// マッチするコマンドを検索
        /// </summary>
        private List<(IVoiceCommand command, string matchedKeyword)> FindMatchingCommands(string recognizedText)
        {
            var matches = new List<(IVoiceCommand, string)>();

            foreach (var command in commands)
            {
                string matchedKeyword = command.Match(recognizedText);
                
                if (!string.IsNullOrEmpty(matchedKeyword))
                {
                    matches.Add((command, matchedKeyword));
                    GameLogger.Log(LogCategory.Voice,$"[CommandRegistry] ✓ Matched: {command.CommandName} (keyword: '{matchedKeyword}')");
                }
            }

            return matches;
        }

        /// <summary>
        /// 登録されているコマンドのリストを取得
        /// </summary>
        public IReadOnlyList<IVoiceCommand> GetRegisteredCommands()
        {
            return commands.AsReadOnly();
        }

        /// <summary>
        /// コマンド名で検索
        /// </summary>
        public IVoiceCommand FindCommandByName(string commandName)
        {
            return commands.FirstOrDefault(c => c.CommandName.Equals(commandName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 登録されているコマンド数
        /// </summary>
        public int CommandCount => commands.Count;

        /// <summary>
        /// デバッグ: すべてのコマンドをログ出力
        /// </summary>
        public void LogAllCommands()
        {
            GameLogger.Log(LogCategory.Voice,"========================================");
            GameLogger.Log(LogCategory.Voice,$"[CommandRegistry] Registered Commands ({commands.Count}):");
            GameLogger.Log(LogCategory.Voice,"========================================");
            
            foreach (var command in commands.OrderByDescending(c => c.Priority))
            {
                GameLogger.Log(LogCategory.Voice,$"  • {command.CommandName} (Priority: {command.Priority})");
                GameLogger.Log(LogCategory.Voice,$"    Keywords: {string.Join(", ", command.Keywords)}");
                GameLogger.Log(LogCategory.Voice,$"    Description: {command.Description}");
                GameLogger.Log(LogCategory.Voice,$"    Can Execute: {command.CanExecute()}");
            }
            
            GameLogger.Log(LogCategory.Voice,"========================================");
        }
    }
}
