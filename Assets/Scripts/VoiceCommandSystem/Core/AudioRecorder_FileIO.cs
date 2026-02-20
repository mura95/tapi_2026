using UnityEngine;
using System;
using System.IO;
using TapHouse.Logging;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// AudioRecorder - ファイル保存処理
    /// </summary>
    public partial class AudioRecorder
    {
        #region File System Initialization

        /// <summary>
        /// ファイルシステムを初期化
        /// </summary>
        private void InitializeFileSystem()
        {
            // 保存先ディレクトリを設定
            savePath = Path.Combine(Application.persistentDataPath, saveFolderName);

            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                GameLogger.Log(LogCategory.Voice, $"[AudioRecorder] Created save directory: {savePath}");
            }

            // 既存ファイル数をカウント
            string[] existingFiles = Directory.GetFiles(savePath, $"{filePrefix}_*.wav");
            recordingCounter = existingFiles.Length;

            // 起動時に保存先を明確に表示
            GameLogger.Log(LogCategory.Voice, "────────────────────────────────────");
            GameLogger.Log(LogCategory.Voice, $"📁 [AudioRecorder] 録音保存先:");
            GameLogger.Log(LogCategory.Voice, $"   {savePath}");
            GameLogger.Log(LogCategory.Voice, $"   既存ファイル数: {recordingCounter}");
            GameLogger.Log(LogCategory.Voice, "────────────────────────────────────");
        }

        #endregion

        #region Automatic Saving

        /// <summary>
        /// 録音を自動保存
        /// </summary>
        private void SaveRecordingAutomatically(float[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[AudioRecorder] Cannot save empty audio data");
                return;
            }

            try
            {
                // ファイル名を生成
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"{filePrefix}_{recordingCounter:D4}_{timestamp}.wav";
                string fullPath = Path.Combine(savePath, filename);

                // 音声の長さを計算
                float duration = audioData.Length / (float)sampleRate;

                // WAVファイルとして保存
                SaveWavFile(fullPath, audioData, sampleRate);

                recordingCounter++;

                // デバッグ出力（保存場所とファイル名を明確に表示）
                GameLogger.Log(LogCategory.Voice, "════════════════════════════════════");
                GameLogger.Log(LogCategory.Voice, $"💾 [AudioRecorder] 録音ファイル保存完了");
                GameLogger.Log(LogCategory.Voice, $"📁 フォルダ: {savePath}");
                GameLogger.Log(LogCategory.Voice, $"📄 ファイル: {filename}");
                GameLogger.Log(LogCategory.Voice, $"⏱️ 長さ: {duration:F2}秒 ({audioData.Length} samples)");
                GameLogger.Log(LogCategory.Voice, $"📍 フルパス: {fullPath}");
                GameLogger.Log(LogCategory.Voice, "════════════════════════════════════");
            }
            catch (Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[AudioRecorder] Failed to save recording: {e.Message}");
            }
        }

        /// <summary>
        /// 保存フォルダを開く（エディタ専用）
        /// </summary>
        public void OpenSaveFolder()
        {
            string path = savePath;

#if UNITY_EDITOR
            if (Directory.Exists(path))
            {
                Application.OpenURL("file://" + path);
            }
            else
            {
                GameLogger.LogWarning(LogCategory.Voice,$"[AudioRecorder] Directory does not exist: {path}");
            }
#else
            GameLogger.Log(LogCategory.Voice,$"[AudioRecorder] Save path: {path}");
#endif
        }

        #endregion

        #region WAV File Writing

        /// <summary>
        /// WAVファイルとして保存
        /// </summary>
        private void SaveWavFile(string filepath, float[] audioData, int sampleRate, int channels = 1)
        {
            const int HEADER_SIZE = 44;

            using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
            {
                // ヘッダー用の空白を書き込み
                byte emptyByte = new byte();
                for (int i = 0; i < HEADER_SIZE; i++)
                {
                    fileStream.WriteByte(emptyByte);
                }

                // オーディオデータを書き込み
                WriteAudioData(fileStream, audioData);

                // ヘッダーを書き込み
                WriteWavHeader(fileStream, sampleRate, channels, audioData.Length);
            }
        }

        /// <summary>
        /// オーディオデータを書き込み
        /// </summary>
        private void WriteAudioData(FileStream fileStream, float[] samples)
        {
            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];

            int rescaleFactor = 32767; // 16-bit max value

            for (int i = 0; i < samples.Length; i++)
            {
                // float [-1, 1] → short [-32768, 32767]
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        /// <summary>
        /// WAVヘッダーを書き込み
        /// </summary>
        private void WriteWavHeader(FileStream fileStream, int sampleRate, int channels, int sampleCount)
        {
            fileStream.Seek(0, SeekOrigin.Begin);

            // RIFF header
            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            // Chunk size
            byte[] chunkSize = BitConverter.GetBytes((int)fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            // WAVE header
            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            // fmt chunk
            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            // Subchunk1 size (16 for PCM)
            byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            // Audio format (1 for PCM)
            ushort audioFormat = 1;
            fileStream.Write(BitConverter.GetBytes(audioFormat), 0, 2);

            // Number of channels
            fileStream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);

            // Sample rate
            fileStream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Byte rate (SampleRate * NumChannels * BitsPerSample/8)
            byte[] byteRate = BitConverter.GetBytes(sampleRate * channels * 2);
            fileStream.Write(byteRate, 0, 4);

            // Block align (NumChannels * BitsPerSample/8)
            ushort blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            // Bits per sample
            ushort bitsPerSample = 16;
            fileStream.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);

            // data chunk
            byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            // Subchunk2 size (NumSamples * NumChannels * BitsPerSample/8)
            byte[] subChunk2 = BitConverter.GetBytes(sampleCount * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }

        #endregion
    }
}