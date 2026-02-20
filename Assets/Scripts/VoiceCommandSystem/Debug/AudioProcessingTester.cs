using UnityEngine;
using System;
using System.IO;
using System.Collections;
using VoiceCommandSystem.Core;
using VoiceCommandSystem.Audio;
using TapHouse.Logging;

namespace VoiceCommandSystem.Debug
{
    /// <summary>
    /// 音声処理テスター
    /// 4パターンの処理設定で録音し、比較用に保存
    /// </summary>
    public class AudioProcessingTester : MonoBehaviour
    {
        [Header("録音設定")]
        [SerializeField] private float recordDuration = 3f;
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private string testFolderName = "AudioProcessingTest";

        [Header("テスト開始")]
        [SerializeField] private KeyCode startTestKey = KeyCode.F5;

        [Header("状態表示")]
        [SerializeField] private bool isTestRunning = false;
        [SerializeField] private string currentTestName = "";
        [SerializeField] private int currentTestIndex = 0;

        private string savePath;
        private string sessionFolder;
        private AudioClip micClip;
        private string microphoneDevice;

        private readonly string[] testNames = new string[]
        {
            "01_Raw_NoProcessing",
            "02_RealtimeOnly",
            "03_FinalOnly",
            "04_Both_CurrentSetting"
        };

        private void Start()
        {
            // マイクデバイスを取得
            if (Microphone.devices.Length > 0)
            {
                microphoneDevice = Microphone.devices[0];
                GameLogger.Log(LogCategory.Voice, $"[AudioProcessingTester] Microphone: {microphoneDevice}");
            }
            else
            {
                GameLogger.LogError(LogCategory.Voice, "[AudioProcessingTester] No microphone found!");
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(startTestKey) && !isTestRunning)
            {
                StartCoroutine(RunAllTests());
            }
        }

        /// <summary>
        /// 全4パターンのテストを実行
        /// </summary>
        private IEnumerator RunAllTests()
        {
            isTestRunning = true;

            // セッションフォルダを作成（タイムスタンプ付き）
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            savePath = Path.Combine(Application.persistentDataPath, testFolderName);
            sessionFolder = Path.Combine(savePath, $"Test_{timestamp}");

            if (!Directory.Exists(sessionFolder))
            {
                Directory.CreateDirectory(sessionFolder);
            }

            GameLogger.Log(LogCategory.Voice, "════════════════════════════════════════════════════");
            GameLogger.Log(LogCategory.Voice, "🧪 [AudioProcessingTester] テスト開始");
            GameLogger.Log(LogCategory.Voice, $"📁 保存先: {sessionFolder}");
            GameLogger.Log(LogCategory.Voice, $"⏱️ 録音時間: {recordDuration}秒 x 4パターン");
            GameLogger.Log(LogCategory.Voice, "════════════════════════════════════════════════════");

            // カウントダウン
            for (int i = 3; i > 0; i--)
            {
                GameLogger.Log(LogCategory.Voice, $"🔔 {i}秒後に開始...");
                yield return new WaitForSeconds(1f);
            }

            // テスト1: 処理なし（生音声）
            yield return StartCoroutine(RecordAndSaveTest(0, false, false));

            yield return new WaitForSeconds(1f);

            // テスト2: リアルタイム処理のみ
            yield return StartCoroutine(RecordAndSaveTest(1, true, false));

            yield return new WaitForSeconds(1f);

            // テスト3: 最終処理のみ
            yield return StartCoroutine(RecordAndSaveTest(2, false, true));

            yield return new WaitForSeconds(1f);

            // テスト4: 両方（現在の設定）
            yield return StartCoroutine(RecordAndSaveTest(3, true, true));

            // 完了
            GameLogger.Log(LogCategory.Voice, "════════════════════════════════════════════════════");
            GameLogger.Log(LogCategory.Voice, "✅ [AudioProcessingTester] 全テスト完了!");
            GameLogger.Log(LogCategory.Voice, $"📁 保存先: {sessionFolder}");
            GameLogger.Log(LogCategory.Voice, "");
            GameLogger.Log(LogCategory.Voice, "保存されたファイル:");
            foreach (var name in testNames)
            {
                GameLogger.Log(LogCategory.Voice, $"  📄 {name}.wav");
            }
            GameLogger.Log(LogCategory.Voice, "════════════════════════════════════════════════════");

            // 比較用のREADMEを作成
            CreateReadme();

            isTestRunning = false;
            currentTestName = "完了";

#if UNITY_EDITOR
            // エディタの場合はフォルダを開く
            Application.OpenURL("file://" + sessionFolder);
#endif
        }

        /// <summary>
        /// 1パターンの録音とテスト
        /// </summary>
        private IEnumerator RecordAndSaveTest(int testIndex, bool applyRealtime, bool applyFinal)
        {
            currentTestIndex = testIndex;
            currentTestName = testNames[testIndex];

            GameLogger.Log(LogCategory.Voice, "────────────────────────────────────────");
            GameLogger.Log(LogCategory.Voice, $"🎙️ テスト {testIndex + 1}/4: {currentTestName}");
            GameLogger.Log(LogCategory.Voice, $"   Realtime Processing: {applyRealtime}");
            GameLogger.Log(LogCategory.Voice, $"   Final Processing: {applyFinal}");
            GameLogger.Log(LogCategory.Voice, "────────────────────────────────────────");

            // 録音開始
            micClip = Microphone.Start(microphoneDevice, false, (int)recordDuration + 1, sampleRate);

            if (micClip == null)
            {
                GameLogger.LogError(LogCategory.Voice, "[AudioProcessingTester] Failed to start recording");
                yield break;
            }

            GameLogger.Log(LogCategory.Voice, $"🔴 録音中... ({recordDuration}秒)");

            // 録音待機
            yield return new WaitForSeconds(recordDuration);

            // 録音停止
            int currentPosition = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);

            // データ取得
            int recordedSamples = Mathf.Min(currentPosition, (int)(recordDuration * sampleRate));
            float[] rawAudioData = new float[recordedSamples];
            micClip.GetData(rawAudioData, 0);

            GameLogger.Log(LogCategory.Voice, $"⏹️ 録音完了: {rawAudioData.Length} samples ({rawAudioData.Length / (float)sampleRate:F2}秒)");

            // 処理適用
            float[] processedData = rawAudioData;

            if (applyRealtime)
            {
                GameLogger.Log(LogCategory.Voice, "   → Realtime処理適用中...");
                var processor = new RealtimeAudioProcessor(sampleRate);
                processedData = processor.ProcessChunk(processedData);
            }

            if (applyFinal)
            {
                GameLogger.Log(LogCategory.Voice, "   → Final処理適用中...");
                processedData = AdvancedAudioFilters.ProcessForWakeWord(processedData, sampleRate);
            }

            // 保存
            string filename = $"{currentTestName}.wav";
            string fullPath = Path.Combine(sessionFolder, filename);
            SaveWavFile(fullPath, processedData, sampleRate);

            GameLogger.Log(LogCategory.Voice, $"💾 保存完了: {filename}");

            // クリップ解放
            if (micClip != null)
            {
                Destroy(micClip);
                micClip = null;
            }
        }

        /// <summary>
        /// 比較用READMEを作成
        /// </summary>
        private void CreateReadme()
        {
            string readmePath = Path.Combine(sessionFolder, "README.txt");
            string content = $@"音声処理テスト結果
==================

テスト日時: {DateTime.Now:yyyy/MM/dd HH:mm:ss}
録音時間: {recordDuration}秒
サンプルレート: {sampleRate}Hz

ファイル一覧:
─────────────────────────────────────────────────────

01_Raw_NoProcessing.wav
  - Realtime Processing: OFF
  - Final Processing: OFF
  - 説明: 生の録音データ（処理なし）

02_RealtimeOnly.wav
  - Realtime Processing: ON
  - Final Processing: OFF
  - 説明: リアルタイム処理のみ適用
    - Pre-emphasis (0.97)
    - Bandpass Filter (300-3400Hz)

03_FinalOnly.wav
  - Realtime Processing: OFF
  - Final Processing: ON
  - 説明: 最終処理のみ適用
    - Pre-emphasis (0.97)
    - Bandpass Filter (300-3400Hz)
    - Spectral Subtraction (最初0.3秒をノイズとして除去)
    - RMS Normalization (-20dB)

04_Both_CurrentSetting.wav
  - Realtime Processing: ON
  - Final Processing: ON
  - 説明: 両方適用（現在の設定）
    ⚠️ Pre-emphasisとBandpassが二重適用される

─────────────────────────────────────────────────────

比較のポイント:
1. 01と02を比較 → リアルタイム処理の影響
2. 01と03を比較 → 最終処理の影響
3. 02と04を比較 → 二重適用の影響
4. 03と04を比較 → 二重適用の影響

推奨再生ソフト: Audacity（波形とスペクトログラムを確認可能）
";

            File.WriteAllText(readmePath, content);
            GameLogger.Log(LogCategory.Voice, $"📝 README作成: {readmePath}");
        }

        #region WAV File Writing

        private void SaveWavFile(string filepath, float[] audioData, int sampleRate, int channels = 1)
        {
            const int HEADER_SIZE = 44;

            using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
            {
                byte emptyByte = new byte();
                for (int i = 0; i < HEADER_SIZE; i++)
                {
                    fileStream.WriteByte(emptyByte);
                }

                WriteAudioData(fileStream, audioData);
                WriteWavHeader(fileStream, sampleRate, channels, audioData.Length);
            }
        }

        private void WriteAudioData(FileStream fileStream, float[] samples)
        {
            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];
            int rescaleFactor = 32767;

            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                intData[i] = (short)(clamped * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        private void WriteWavHeader(FileStream fileStream, int sampleRate, int channels, int sampleCount)
        {
            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            byte[] chunkSize = BitConverter.GetBytes((int)fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            ushort audioFormat = 1;
            fileStream.Write(BitConverter.GetBytes(audioFormat), 0, 2);

            fileStream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);

            fileStream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            byte[] byteRate = BitConverter.GetBytes(sampleRate * channels * 2);
            fileStream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            ushort bitsPerSample = 16;
            fileStream.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);

            byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            byte[] subChunk2 = BitConverter.GetBytes(sampleCount * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            if (!isTestRunning) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;

            string status = $"🧪 テスト中: {currentTestIndex + 1}/4\n{currentTestName}";

            GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100), status, style);
        }

        #endregion
    }
}
