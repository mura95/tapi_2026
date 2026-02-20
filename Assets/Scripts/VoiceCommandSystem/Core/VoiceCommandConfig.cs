using UnityEngine;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// 音声コマンドシステムの設定（APIキーなど）
    /// このファイルは .gitignore に追加してGitHubにアップロードしないでください
    /// </summary>
    [CreateAssetMenu(fileName = "VoiceCommandConfig", menuName = "VoiceCommand/Config")]
    public class VoiceCommandConfig : ScriptableObject
    {
        [Header("OpenAI API設定")]
        [Tooltip("OpenAI APIキー（gitignoreで除外されます）")]
        [SerializeField] private string openAIApiKey = "";

        [Header("機能設定")]
        [SerializeField] private bool useOpenAI = true;
        [SerializeField] private bool useLocalWhisper = true;

        public string OpenAIApiKey => openAIApiKey;
        public bool UseOpenAI => useOpenAI;
        public bool UseLocalWhisper => useLocalWhisper;

        /// <summary>
        /// APIキーが設定されているかチェック
        /// </summary>
        public bool HasApiKey()
        {
            return !string.IsNullOrEmpty(openAIApiKey);
        }

        /// <summary>
        /// APIキーを設定（実行時）
        /// </summary>
        public void SetApiKey(string apiKey)
        {
            openAIApiKey = apiKey;
        }
    }
}
