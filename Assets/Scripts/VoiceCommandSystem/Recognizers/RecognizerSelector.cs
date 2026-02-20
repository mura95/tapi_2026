using System;
using System.Threading.Tasks;
using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Recognizers
{
    /// <summary>
    /// ネットワーク状態に応じて認識エンジンを自動選択
    /// オンライン: OpenAI Whisper API
    /// オフライン: ローカルWhisper
    /// </summary>
    public class RecognizerSelector : IVoiceRecognizer
    {
        private OpenAIVoiceRecognizer openAIRecognizer;
        private LocalWhisperRecognizer localRecognizer;
        private IVoiceRecognizer currentRecognizer;

        private bool preferOnline = true;
        private bool fallbackToLocal = true;

        public bool IsInitialized => currentRecognizer?.IsInitialized ?? false;
        public string RecognizerName => currentRecognizer?.RecognizerName ?? "Not Selected";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="openAIApiKey">OpenAI APIキー（オンライン使用時）</param>
        /// <param name="preferOnline">オンライン優先（デフォルト: true）</param>
        /// <param name="fallbackToLocal">オフライン時にローカルを使用（デフォルト: true）</param>
        public RecognizerSelector(string openAIApiKey, bool preferOnline = true, bool fallbackToLocal = true)
        {
            this.preferOnline = preferOnline;
            this.fallbackToLocal = fallbackToLocal;

            // 認識エンジンの初期化
            if (!string.IsNullOrEmpty(openAIApiKey))
            {
                openAIRecognizer = new OpenAIVoiceRecognizer(openAIApiKey);
            }

            localRecognizer = new LocalWhisperRecognizer();
        }

        public async Task<bool> InitializeAsync()
        {
            GameLogger.Log(LogCategory.Voice,"[RecognizerSelector] Initializing recognizers...");

            bool openAISuccess = false;
            bool localSuccess = false;

            // OpenAI認識エンジンの初期化
            if (openAIRecognizer != null)
            {
                try
                {
                    openAISuccess = await openAIRecognizer.InitializeAsync();
                }
                catch (Exception e)
                {
                    GameLogger.LogWarning(LogCategory.Voice,$"[RecognizerSelector] OpenAI initialization failed: {e.Message}");
                }
            }

            // ローカル認識エンジンの初期化
            if (fallbackToLocal)
            {
                try
                {
                    localSuccess = await localRecognizer.InitializeAsync();
                }
                catch (Exception e)
                {
                    GameLogger.LogWarning(LogCategory.Voice,$"[RecognizerSelector] Local Whisper initialization failed: {e.Message}");
                }
            }

            // 結果サマリー
            GameLogger.Log(LogCategory.Voice,$"[RecognizerSelector] OpenAI: {(openAISuccess ? "✅" : "❌")} | Local: {(localSuccess ? "✅" : "❌")}");

            if (!openAISuccess && !localSuccess)
            {
                GameLogger.LogError(LogCategory.Voice,"[RecognizerSelector] ❌ No recognizers available!");
                return false;
            }

            return true;
        }

        public async Task<string> RecognizeAsync(float[] audioData, int sampleRate = 16000)
        {
            // 認識エンジンの選択
            SelectRecognizer();

            if (currentRecognizer == null || !currentRecognizer.IsInitialized)
            {
                GameLogger.LogError(LogCategory.Voice,"[RecognizerSelector] No available recognizer");
                return null;
            }

            GameLogger.Log(LogCategory.Voice,$"[RecognizerSelector] Using: {currentRecognizer.RecognizerName}");

            try
            {
                // 認識実行
                string result = await currentRecognizer.RecognizeAsync(audioData, sampleRate);

                // 失敗時のフォールバック
                if (string.IsNullOrEmpty(result) && ShouldFallback())
                {
                    GameLogger.LogWarning(LogCategory.Voice,"[RecognizerSelector] Primary recognizer failed, trying fallback...");
                    
                    // 別の認識エンジンを試す
                    IVoiceRecognizer fallbackRecognizer = GetFallbackRecognizer();
                    if (fallbackRecognizer != null && fallbackRecognizer.IsInitialized)
                    {
                        GameLogger.Log(LogCategory.Voice,$"[RecognizerSelector] Fallback to: {fallbackRecognizer.RecognizerName}");
                        result = await fallbackRecognizer.RecognizeAsync(audioData, sampleRate);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[RecognizerSelector] Recognition failed: {e.Message}");
                
                // 例外時もフォールバックを試みる
                if (ShouldFallback())
                {
                    IVoiceRecognizer fallbackRecognizer = GetFallbackRecognizer();
                    if (fallbackRecognizer != null && fallbackRecognizer.IsInitialized)
                    {
                        GameLogger.Log(LogCategory.Voice,$"[RecognizerSelector] Exception fallback to: {fallbackRecognizer.RecognizerName}");
                        try
                        {
                            return await fallbackRecognizer.RecognizeAsync(audioData, sampleRate);
                        }
                        catch (Exception fallbackEx)
                        {
                            GameLogger.LogError(LogCategory.Voice,$"[RecognizerSelector] Fallback also failed: {fallbackEx.Message}");
                        }
                    }
                }
                
                return null;
            }
        }

        /// <summary>
        /// 現在のネットワーク状態に応じて認識エンジンを選択
        /// </summary>
        private void SelectRecognizer()
        {
            if (preferOnline && IsOnline() && openAIRecognizer != null && openAIRecognizer.IsInitialized)
            {
                currentRecognizer = openAIRecognizer;
            }
            else if (fallbackToLocal && localRecognizer != null && localRecognizer.IsInitialized)
            {
                currentRecognizer = localRecognizer;
            }
            else if (openAIRecognizer != null && openAIRecognizer.IsInitialized)
            {
                currentRecognizer = openAIRecognizer;
            }
            else
            {
                currentRecognizer = null;
            }
        }

        /// <summary>
        /// フォールバック用の認識エンジンを取得
        /// </summary>
        private IVoiceRecognizer GetFallbackRecognizer()
        {
            if (currentRecognizer == openAIRecognizer && localRecognizer?.IsInitialized == true)
            {
                return localRecognizer;
            }
            else if (currentRecognizer == localRecognizer && openAIRecognizer?.IsInitialized == true)
            {
                return openAIRecognizer;
            }
            return null;
        }

        /// <summary>
        /// フォールバックすべきかどうか
        /// </summary>
        private bool ShouldFallback()
        {
            return fallbackToLocal && 
                   ((currentRecognizer == openAIRecognizer && localRecognizer?.IsInitialized == true) ||
                    (currentRecognizer == localRecognizer && openAIRecognizer?.IsInitialized == true));
        }

        /// <summary>
        /// オンライン状態かどうかをチェック
        /// </summary>
        private bool IsOnline()
        {
            // Unity のネットワーク到達性をチェック
            bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
            
            if (!hasInternet)
            {
                GameLogger.Log(LogCategory.Voice,"[RecognizerSelector] Offline - using local recognizer");
            }
            
            return hasInternet;
        }

        /// <summary>
        /// 優先順位を変更
        /// </summary>
        public void SetPreferOnline(bool prefer)
        {
            preferOnline = prefer;
            GameLogger.Log(LogCategory.Voice,$"[RecognizerSelector] Prefer online set to: {prefer}");
        }

        /// <summary>
        /// フォールバックを有効/無効化
        /// </summary>
        public void SetFallbackEnabled(bool enabled)
        {
            fallbackToLocal = enabled;
            GameLogger.Log(LogCategory.Voice,$"[RecognizerSelector] Fallback to local set to: {enabled}");
        }

        /// <summary>
        /// 現在使用中の認識エンジンを取得
        /// </summary>
        public IVoiceRecognizer GetCurrentRecognizer()
        {
            SelectRecognizer();
            return currentRecognizer;
        }

        public void Dispose()
        {
            openAIRecognizer?.Dispose();
            localRecognizer?.Dispose();
            currentRecognizer = null;
            GameLogger.Log(LogCategory.Voice,"[RecognizerSelector] Disposed");
        }
    }
}
