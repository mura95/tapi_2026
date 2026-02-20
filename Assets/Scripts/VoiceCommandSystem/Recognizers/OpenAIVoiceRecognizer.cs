using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TapHouse.Logging;

namespace VoiceCommandSystem.Recognizers
{
    /// <summary>
    /// OpenAI Whisper API を使用した音声認識
    /// オンライン接続時に使用
    /// </summary>
    public class OpenAIVoiceRecognizer : IVoiceRecognizer
    {
        private const string API_ENDPOINT = "https://api.openai.com/v1/audio/transcriptions";
        private const string MODEL = "whisper-1";

        private string apiKey;
        private bool isInitialized = false;

        public bool IsInitialized => isInitialized;
        public string RecognizerName => "OpenAI Whisper API";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="apiKey">OpenAI API キー</param>
        public OpenAIVoiceRecognizer(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<bool> InitializeAsync()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                GameLogger.LogError(LogCategory.Voice,"[OpenAI] API key is not set");
                return false;
            }

            // API接続テスト（オプション）
            isInitialized = true;
            GameLogger.Log(LogCategory.Voice,"[OpenAI] Initialized successfully");
            return true;
        }

        public async Task<string> RecognizeAsync(float[] audioData, int sampleRate = 16000)
        {
            if (!isInitialized)
            {
                GameLogger.LogError(LogCategory.Voice,"[OpenAI] Not initialized");
                return null;
            }

            try
            {
                // 音声データをWAV形式に変換
                byte[] wavData = ConvertToWav(audioData, sampleRate);

                // マルチパートフォームデータを作成
                WWWForm form = new WWWForm();
                form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
                form.AddField("model", MODEL);
                form.AddField("language", "ja"); // 日本語
                form.AddField("response_format", "json");

                // APIリクエスト送信
                using (UnityWebRequest request = UnityWebRequest.Post(API_ENDPOINT, form))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    request.timeout = 30; // 30秒タイムアウト

                    GameLogger.Log(LogCategory.Voice,$"[OpenAI] Sending request... (audio size: {wavData.Length / 1024}KB)");

                    var operation = request.SendWebRequest();

                    // 非同期待機
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    // レスポンス処理
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;
                        string transcription = ParseTranscriptionResponse(jsonResponse);

                        GameLogger.Log(LogCategory.Voice,$"[OpenAI] ✅ Transcription: {transcription}");
                        return transcription;
                    }
                    else
                    {
                        GameLogger.LogError(LogCategory.Voice,$"[OpenAI] ❌ Request failed: {request.error}");
                        GameLogger.LogError(LogCategory.Voice,$"[OpenAI] Response: {request.downloadHandler.text}");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[OpenAI] Exception: {e.Message}");
                GameLogger.LogError(LogCategory.Voice,$"[OpenAI] Stack trace: {e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// float配列をWAV形式のバイト配列に変換
        /// </summary>
        private byte[] ConvertToWav(float[] samples, int sampleRate)
        {
            int byteCount = samples.Length * 2;
            byte[] wavData = new byte[44 + byteCount];

            // WAVヘッダー
            Array.Copy(Encoding.ASCII.GetBytes("RIFF"), 0, wavData, 0, 4);
            BitConverter.GetBytes(36 + byteCount).CopyTo(wavData, 4);
            Array.Copy(Encoding.ASCII.GetBytes("WAVE"), 0, wavData, 8, 4);
            Array.Copy(Encoding.ASCII.GetBytes("fmt "), 0, wavData, 12, 4);
            BitConverter.GetBytes(16).CopyTo(wavData, 16); // fmt チャンクサイズ
            BitConverter.GetBytes((short)1).CopyTo(wavData, 20); // PCM
            BitConverter.GetBytes((short)1).CopyTo(wavData, 22); // モノラル
            BitConverter.GetBytes(sampleRate).CopyTo(wavData, 24);
            BitConverter.GetBytes(sampleRate * 2).CopyTo(wavData, 28); // バイトレート
            BitConverter.GetBytes((short)2).CopyTo(wavData, 32); // ブロックアライン
            BitConverter.GetBytes((short)16).CopyTo(wavData, 34); // ビット深度
            Array.Copy(Encoding.ASCII.GetBytes("data"), 0, wavData, 36, 4);
            BitConverter.GetBytes(byteCount).CopyTo(wavData, 40);

            // サンプルデータ
            int offset = 44;
            foreach (float sample in samples)
            {
                short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * 32767f);
                BitConverter.GetBytes(intSample).CopyTo(wavData, offset);
                offset += 2;
            }

            return wavData;
        }

        /// <summary>
        /// OpenAI APIのJSONレスポンスから文字起こしテキストを抽出
        /// </summary>
        private string ParseTranscriptionResponse(string jsonResponse)
        {
            try
            {
                // 簡易的なJSON解析（JsonUtilityはネストしたJSONに対応していないため）
                var response = JsonUtility.FromJson<TranscriptionResponse>(jsonResponse);
                return response?.text ?? string.Empty;
            }
            catch (Exception e)
            {
                GameLogger.LogWarning(LogCategory.Voice,$"[OpenAI] Failed to parse JSON: {e.Message}");
                
                // フォールバック: 手動で "text" フィールドを抽出
                int textIndex = jsonResponse.IndexOf("\"text\"");
                if (textIndex >= 0)
                {
                    int startIndex = jsonResponse.IndexOf("\"", textIndex + 7) + 1;
                    int endIndex = jsonResponse.IndexOf("\"", startIndex);
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        return jsonResponse.Substring(startIndex, endIndex - startIndex);
                    }
                }
                
                return string.Empty;
            }
        }

        [Serializable]
        private class TranscriptionResponse
        {
            public string text;
        }

        public void Dispose()
        {
            // リソースの解放（必要に応じて）
            isInitialized = false;
            GameLogger.Log(LogCategory.Voice,"[OpenAI] Disposed");
        }
    }
}
