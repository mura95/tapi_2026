using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using TapHouse.Logging;

#if WHISPER_UNITY_INSTALLED
using Whisper;
using Whisper.Utils;
#endif

namespace VoiceCommandSystem.Recognizers
{
    /// <summary>
    /// ローカルWhisperモデルを使用した音声認識
    /// オフライン時に使用を想定していただが、ファイルが重すぎるので、未実装
    /// 注意: whisper.unity パッケージが必要
    /// </summary>
    public class LocalWhisperRecognizer : IVoiceRecognizer
    {
        private const string MODEL_FILENAME = "ggml-tiny.bin";

#if WHISPER_UNITY_INSTALLED
        private WhisperManager whisperManager;
#endif
        private bool isInitialized = false;
        private string modelPath;

        public bool IsInitialized => isInitialized;
        public string RecognizerName => "Local Whisper (ggml-small)";

        public async Task<bool> InitializeAsync()
        {
#if !WHISPER_UNITY_INSTALLED
            GameLogger.LogWarning(LogCategory.Voice,"[LocalWhisper] whisper.unity package is not installed");
            GameLogger.LogWarning(LogCategory.Voice,"[LocalWhisper] Install from: https://github.com/Macoron/whisper.unity");
            return false;
#else
            try
            {
                GameLogger.Log(LogCategory.Voice,"[LocalWhisper] Initializing...");

                // モデルパスの取得
                modelPath = await GetModelPath();
                
                if (string.IsNullOrEmpty(modelPath) || !File.Exists(modelPath))
                {
                    GameLogger.LogError(LogCategory.Voice,$"[LocalWhisper] Model file not found: {modelPath}");
                    return false;
                }

                GameLogger.Log(LogCategory.Voice,$"[LocalWhisper] Loading model from: {modelPath}");

                // Whisperマネージャーの初期化
                whisperManager = new WhisperManager();
                await whisperManager.InitModel(modelPath);

                // 日本語設定
                whisperManager.language = "ja";
                whisperManager.translateToEnglish = false;

                isInitialized = true;
                GameLogger.Log(LogCategory.Voice,"[LocalWhisper] ✅ Initialized successfully");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[LocalWhisper] Initialization failed: {e.Message}");
                GameLogger.LogError(LogCategory.Voice,$"[LocalWhisper] Stack trace: {e.StackTrace}");
                return false;
            }
#endif
        }

        public async Task<string> RecognizeAsync(float[] audioData, int sampleRate = 16000)
        {
#if !WHISPER_UNITY_INSTALLED
            GameLogger.LogWarning(LogCategory.Voice,"[LocalWhisper] whisper.unity package is not installed");
            return null;
#else
            if (!isInitialized)
            {
                GameLogger.LogError(LogCategory.Voice,"[LocalWhisper] Not initialized");
                return null;
            }

            try
            {
                GameLogger.Log(LogCategory.Voice,$"[LocalWhisper] Recognizing audio... ({audioData.Length} samples)");

                // サンプルレート変換（必要な場合）
                if (sampleRate != 16000)
                {
                    GameLogger.LogWarning(LogCategory.Voice,$"[LocalWhisper] Resampling from {sampleRate}Hz to 16000Hz");
                    audioData = ResampleAudio(audioData, sampleRate, 16000);
                }

                // 文字起こし実行
                var result = await whisperManager.GetTextAsync(audioData);
                
                if (result == null || string.IsNullOrEmpty(result.Result))
                {
                    GameLogger.LogWarning(LogCategory.Voice,"[LocalWhisper] No transcription result");
                    return null;
                }

                string transcription = result.Result.Trim();
                GameLogger.Log(LogCategory.Voice,$"[LocalWhisper] ✅ Transcription: {transcription}");
                
                return transcription;
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[LocalWhisper] Recognition failed: {e.Message}");
                GameLogger.LogError(LogCategory.Voice,$"[LocalWhisper] Stack trace: {e.StackTrace}");
                return null;
            }
#endif
        }

        /// <summary>
        /// モデルファイルのパスを取得（Androidの場合はコピー）
        /// </summary>
        private async Task<string> GetModelPath()
        {
            string modelPath;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Androidの場合、StreamingAssetsからpersistentDataPathにコピー
            modelPath = Path.Combine(Application.persistentDataPath, MODEL_FILENAME);
            
            if (!File.Exists(modelPath))
            {
                GameLogger.Log(LogCategory.Voice,$"[LocalWhisper] Copying model to cache...");
                string sourcePath = Path.Combine(Application.streamingAssetsPath, "whisper", MODEL_FILENAME);
                
                using (UnityEngine.Networking.UnityWebRequest www = 
                       UnityEngine.Networking.UnityWebRequest.Get(sourcePath))
                {
                    var operation = www.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        File.WriteAllBytes(modelPath, www.downloadHandler.data);
                        GameLogger.Log(LogCategory.Voice,$"[LocalWhisper] Model copied successfully ({www.downloadHandler.data.Length / 1024 / 1024}MB)");
                    }
                    else
                    {
                        GameLogger.LogError(LogCategory.Voice,$"[LocalWhisper] Failed to copy model: {www.error}");
                        return null;
                    }
                }
            }
#else
            // それ以外の場合はStreamingAssetsから直接読み込み
            modelPath = Path.Combine(Application.streamingAssetsPath, "whisper", MODEL_FILENAME);
#endif

            return modelPath;
        }

        /// <summary>
        /// オーディオのリサンプリング（簡易版）
        /// </summary>
        private float[] ResampleAudio(float[] input, int inputRate, int outputRate)
        {
            if (inputRate == outputRate) return input;

            float ratio = (float)outputRate / inputRate;
            int outputLength = Mathf.CeilToInt(input.Length * ratio);
            float[] output = new float[outputLength];

            for (int i = 0; i < outputLength; i++)
            {
                float srcIndex = i / ratio;
                int srcIndexInt = (int)srcIndex;
                float frac = srcIndex - srcIndexInt;

                if (srcIndexInt + 1 < input.Length)
                {
                    // 線形補間
                    output[i] = Mathf.Lerp(input[srcIndexInt], input[srcIndexInt + 1], frac);
                }
                else if (srcIndexInt < input.Length)
                {
                    output[i] = input[srcIndexInt];
                }
            }

            return output;
        }

        public void Dispose()
        {
#if WHISPER_UNITY_INSTALLED
            whisperManager?.Dispose();
#endif
            isInitialized = false;
            GameLogger.Log(LogCategory.Voice,"[LocalWhisper] Disposed");
        }
    }
}
