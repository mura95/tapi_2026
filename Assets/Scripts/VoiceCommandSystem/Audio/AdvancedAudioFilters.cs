using UnityEngine;
using NWaves.Signals;
using NWaves.Operations;
using NWaves.Filters;
using NWaves.Filters.BiQuad;
using System.Linq;
using TapHouse.Logging;

namespace VoiceCommandSystem.Audio
{
    /// <summary>
    /// 音声処理パイプライン - Wake Word検出用の前処理
    /// リアルタイム処理と最終処理の両方を提供
    /// </summary>
    public static class AdvancedAudioFilters
    {
        private const int DEFAULT_SAMPLE_RATE = 16000;
        private const float PRE_EMPHASIS_COEF = 0.97f;
        private const float NOISE_DURATION_SEC = 0.3f;
        private const float RMS_TARGET_DB = -20f;

        #region 完全な前処理パイプライン（Wake Word検出用）

        /// <summary>
        /// Wake Word検出用の完全な前処理パイプライン
        /// リサンプリング → プリエンファシス → バンドパス → ノイズ除去 → 正規化
        /// </summary>
        public static float[] ProcessForWakeWord(float[] samples, int originalSampleRate, int targetSampleRate = DEFAULT_SAMPLE_RATE)
        {
            if (samples == null || samples.Length == 0)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[AdvancedAudioFilters] Empty samples received");
                return samples;
            }

            var signal = new DiscreteSignal(originalSampleRate, samples);

            GameLogger.Log(LogCategory.Voice,"[AdvancedAudioFilters] Starting processing pipeline...");

            // 1. リサンプリング（必要な場合）
            if (signal.SamplingRate != targetSampleRate)
            {
                GameLogger.Log(LogCategory.Voice,$"[AdvancedAudioFilters] Resampling: {signal.SamplingRate}Hz -> {targetSampleRate}Hz");
                signal = ResampleSignal(signal, targetSampleRate);
            }

            // 2. プリエンファシス（高周波強調）
            GameLogger.Log(LogCategory.Voice,"[AdvancedAudioFilters] Applying pre-emphasis...");
            signal = ApplyPreEmphasis(signal);

            // 3. バンドパスフィルター（人の声の周波数帯域）
            GameLogger.Log(LogCategory.Voice,"[AdvancedAudioFilters] Applying voice bandpass filter...");
            signal = ApplyVoiceBandpass(signal);

            // 4. ノイズ除去（スペクトラルサブトラクション）
            GameLogger.Log(LogCategory.Voice,"[AdvancedAudioFilters] Reducing background noise...");
            signal = ReduceBackgroundNoise(signal);

            // 5. RMS正規化
            GameLogger.Log(LogCategory.Voice,"[AdvancedAudioFilters] Normalizing RMS...");
            signal = NormalizeRms(signal);

            GameLogger.Log(LogCategory.Voice,"[AdvancedAudioFilters] Processing pipeline complete");
            return signal.Samples;
        }

        #endregion

        #region 個別処理ステップ

        /// <summary>
        /// リサンプリング
        /// </summary>
        public static DiscreteSignal ResampleSignal(DiscreteSignal signal, int targetSampleRate)
        {
            return Operation.Resample(signal, targetSampleRate);
        }

        /// <summary>
        /// プリエンファシスフィルター適用（高周波成分を強調）
        /// </summary>
        public static DiscreteSignal ApplyPreEmphasis(DiscreteSignal signal, float coefficient = PRE_EMPHASIS_COEF)
        {
            var filter = new PreEmphasisFilter(coefficient);
            return filter.ApplyTo(signal);
        }

        /// <summary>
        /// 人の声の周波数帯域に絞るバンドパスフィルター（300Hz～3400Hz）
        /// </summary>
        public static DiscreteSignal ApplyVoiceBandpass(DiscreteSignal signal,
            float lowFreqHz = 300f, float highFreqHz = 3400f)
        {
            try
            {
                float lowCutoff = lowFreqHz / signal.SamplingRate;
                float highCutoff = highFreqHz / signal.SamplingRate;

                // ハイパスフィルター（低周波カット）
                var highPassFilter = new HighPassFilter(lowCutoff);
                signal = highPassFilter.ApplyTo(signal);

                // ローパスフィルター（高周波カット）
                var lowPassFilter = new LowPassFilter(highCutoff);
                signal = lowPassFilter.ApplyTo(signal);

                return signal;
            }
            catch (System.Exception e)
            {
                GameLogger.LogWarning(LogCategory.Voice,$"[AudioProcessingPipeline] Bandpass filter failed: {e.Message}");
                return signal;
            }
        }

        /// <summary>
        /// スペクトラルサブトラクションによる背景ノイズ除去
        /// 最初の数百ミリ秒をノイズプロファイルとして使用
        /// </summary>
        public static DiscreteSignal ReduceBackgroundNoise(DiscreteSignal signal, float noiseDurationSec = NOISE_DURATION_SEC)
        {
            int noiseSamples = (int)(noiseDurationSec * signal.SamplingRate);

            if (signal.Length <= noiseSamples)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[AudioProcessingPipeline] Signal too short for noise reduction, skipping...");
                return signal;
            }

            try
            {
                var noiseProfile = signal.First(noiseSamples);
                return Operation.SpectralSubtract(signal, noiseProfile);
            }
            catch (System.Exception e)
            {
                GameLogger.LogWarning(LogCategory.Voice,$"[AudioProcessingPipeline] Noise reduction failed: {e.Message}");
                return signal;
            }
        }

        /// <summary>
        /// RMS正規化（音量の平均レベルを調整）
        /// </summary>
        public static DiscreteSignal NormalizeRms(DiscreteSignal signal, float rmsDb = RMS_TARGET_DB)
        {
            try
            {
                return Operation.NormalizeRms(signal, rmsDb);
            }
            catch (System.Exception e)
            {
                GameLogger.LogWarning(LogCategory.Voice,$"[AudioProcessingPipeline] RMS normalization failed: {e.Message}");
                return signal;
            }
        }

        #endregion

        #region ユーティリティ

        /// <summary>
        /// ステレオをモノラルに変換
        /// </summary>
        public static float[] ConvertStereoToMono(float[] stereoSamples)
        {
            float[] mono = new float[stereoSamples.Length / 2];
            for (int i = 0; i < mono.Length; i++)
            {
                mono[i] = (stereoSamples[i * 2] + stereoSamples[i * 2 + 1]) / 2f;
            }
            return mono;
        }

        /// <summary>
        /// 信号の基本統計情報を表示
        /// </summary>
        public static void PrintSignalStats(DiscreteSignal signal, string label = "Signal")
        {
            float min = signal.Samples.Min();
            float max = signal.Samples.Max();
            float mean = signal.Samples.Average();
            float rms = Mathf.Sqrt(signal.Samples.Select(s => s * s).Average());

            GameLogger.Log(LogCategory.Voice,$"[AudioProcessingPipeline] {label} Stats:");
            GameLogger.Log(LogCategory.Voice,$"  Length: {signal.Length} samples ({signal.Duration:F3}s)");
            GameLogger.Log(LogCategory.Voice,$"  Sample Rate: {signal.SamplingRate}Hz");
            GameLogger.Log(LogCategory.Voice,$"  Range: [{min:F4}, {max:F4}]");
            GameLogger.Log(LogCategory.Voice,$"  Mean: {mean:F4}, RMS: {rms:F4}");
        }

        #endregion
    }
}