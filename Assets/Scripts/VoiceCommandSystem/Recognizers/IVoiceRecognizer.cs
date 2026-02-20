using System.Threading.Tasks;

namespace VoiceCommandSystem.Recognizers
{
    /// <summary>
    /// 音声認識エンジンのインターフェース
    /// OpenAI APIやローカルWhisperなど、異なる実装を抽象化
    /// </summary>
    public interface IVoiceRecognizer
    {
        /// <summary>
        /// 認識エンジンが初期化されているか
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 認識エンジンの名前
        /// </summary>
        string RecognizerName { get; }

        /// <summary>
        /// 初期化処理
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// 音声データを文字起こし
        /// </summary>
        /// <param name="audioData">16kHz モノラル音声データ</param>
        /// <param name="sampleRate">サンプルレート（デフォルト: 16000）</param>
        /// <returns>認識されたテキスト（日本語）</returns>
        Task<string> RecognizeAsync(float[] audioData, int sampleRate = 16000);

        /// <summary>
        /// リソースの解放
        /// </summary>
        void Dispose();
    }
}
