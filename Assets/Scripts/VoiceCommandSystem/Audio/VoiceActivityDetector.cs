using UnityEngine;
using System.Collections.Generic;
using System;
using TapHouse.Logging;

namespace VoiceCommandSystem.Audio
{
    /// <summary>
    /// Voice Activity Detection (VAD) - 音声検出システム
    /// </summary>
    public class VoiceActivityDetector
    {
        #region Settings

        private readonly float energyThreshold;
        private readonly float silenceDuration;
        private readonly float maxRecordingDuration;
        private readonly float minRecordingDuration;
        private readonly int sampleRate;

        #endregion

        #region State

        private bool isDetectingSpeech = false;
        private float silenceTimer = 0f;
        private float recordingTimer = 0f;
        private List<float> recordedBuffer = new List<float>();

        #endregion

        #region Events

        public event Action OnSpeechStarted;
        public event Action<float[]> OnSpeechEnded;

        #endregion

        #region Constructor

        public VoiceActivityDetector(
            int sampleRate = 16000,
            float energyThreshold = 0.01f,
            float silenceDuration = 0.5f,
            float maxRecordingDuration = 3f,
            float minRecordingDuration = 0.3f)
        {
            this.sampleRate = sampleRate;
            this.energyThreshold = energyThreshold;
            this.silenceDuration = silenceDuration;
            this.maxRecordingDuration = maxRecordingDuration;
            this.minRecordingDuration = minRecordingDuration; // 最小0.3秒（APIの0.1秒要件 + 余裕）
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 音声チャンクを処理してVAD判定
        /// </summary>
        public void ProcessChunk(float[] chunk)
        {
            if (chunk == null || chunk.Length == 0)
                return;

            float energy = CalculateRMS(chunk);
            bool isSpeech = energy > energyThreshold;

            float chunkDuration = chunk.Length / (float)sampleRate;

            if (isSpeech)
            {
                if (!isDetectingSpeech)
                {
                    StartSpeechDetection();
                }

                // 音声データをバッファに追加
                foreach (var sample in chunk)
                {
                    recordedBuffer.Add(sample);
                }

                silenceTimer = 0f;
                recordingTimer += chunkDuration;
            }
            else if (isDetectingSpeech)
            {
                // 無音期間
                silenceTimer += chunkDuration;

                // 無音が続いたら録音終了
                if (silenceTimer >= silenceDuration)
                {
                    StopSpeechDetection();
                }
            }

            // 最大録音時間チェック
            if (isDetectingSpeech && recordingTimer >= maxRecordingDuration)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[VAD] Max recording duration reached");
                StopSpeechDetection();
            }
        }

        /// <summary>
        /// VADをリセット
        /// </summary>
        public void Reset()
        {
            isDetectingSpeech = false;
            silenceTimer = 0f;
            recordingTimer = 0f;
            recordedBuffer.Clear();
        }

        #endregion

        #region Private Methods

        private void StartSpeechDetection()
        {
            isDetectingSpeech = true;
            recordedBuffer.Clear();
            recordingTimer = 0f;
            silenceTimer = 0f;

            GameLogger.Log(LogCategory.Voice,"[VAD] Speech started");
            OnSpeechStarted?.Invoke();
        }

        private void StopSpeechDetection()
        {
            isDetectingSpeech = false;

            float[] audioData = recordedBuffer.ToArray();
            float duration = audioData.Length / (float)sampleRate;

            // 最小録音時間チェック（短すぎる音声はAPIに送信しない）
            if (duration < minRecordingDuration)
            {
                GameLogger.Log(LogCategory.Voice,
                    $"[VAD] Audio too short ({duration:F2}s < {minRecordingDuration}s) - discarded");
                recordedBuffer.Clear();
                return;
            }

            GameLogger.Log(LogCategory.Voice,$"[VAD] Speech ended: {audioData.Length} samples ({duration:F2}s)");

            OnSpeechEnded?.Invoke(audioData);

            recordedBuffer.Clear();
        }

        private float CalculateRMS(float[] samples)
        {
            float sum = 0f;
            foreach (var sample in samples)
            {
                sum += sample * sample;
            }
            return Mathf.Sqrt(sum / samples.Length);
        }

        #endregion

        #region Properties

        public bool IsDetecting => isDetectingSpeech;

        #endregion
    }
}
