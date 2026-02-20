using UnityEngine;
using NWaves.Filters;
using NWaves.Filters.BiQuad;
using NWaves.Signals;
using TapHouse.Logging;

namespace VoiceCommandSystem.Audio
{
    /// <summary>
    /// リアルタイム音声処理パイプライン
    /// チャンク単位でフィルタリングを適用
    /// </summary>
    public class RealtimeAudioProcessor
    {
        #region Filters

        private PreEmphasisFilter preEmphasisFilter;
        private BiQuadFilter bandpassFilterLow;
        private BiQuadFilter bandpassFilterHigh;

        #endregion

        #region Settings

        private readonly int sampleRate;
        private const float PRE_EMPHASIS_COEF = 0.97f;
        private const float LOW_FREQ_HZ = 300f;
        private const float HIGH_FREQ_HZ = 3400f;

        #endregion

        #region Constructor

        public RealtimeAudioProcessor(int sampleRate = 16000)
        {
            this.sampleRate = sampleRate;
            InitializeFilters();
        }

        #endregion

        #region Initialization

        private void InitializeFilters()
        {
            // プリエンファシスフィルター
            preEmphasisFilter = new PreEmphasisFilter(PRE_EMPHASIS_COEF);

            // バンドパスフィルター（ハイパス + ローパスの組み合わせ）
            try
            {
                // 300Hz以下をカット（ハイパス）
                bandpassFilterLow = new HighPassFilter(LOW_FREQ_HZ / sampleRate);

                // 3400Hz以上をカット（ローパス）
                bandpassFilterHigh = new LowPassFilter(HIGH_FREQ_HZ / sampleRate);

                GameLogger.Log(LogCategory.Voice,$"[RealtimeAudioProcessor] Filters initialized (bandpass: {LOW_FREQ_HZ}-{HIGH_FREQ_HZ}Hz)");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"[RealtimeAudioProcessor] Failed to initialize filters: {e.Message}");
                bandpassFilterLow = null;
                bandpassFilterHigh = null;
            }
        }

        #endregion

        #region Processing

        /// <summary>
        /// リアルタイムでチャンクを処理（サンプル単位フィルタリング）
        /// </summary>
        public float[] ProcessChunk(float[] chunk)
        {
            if (chunk == null || chunk.Length == 0)
                return chunk;

            float[] output = new float[chunk.Length];

            for (int i = 0; i < chunk.Length; i++)
            {
                // プリエンファシス
                float sample = preEmphasisFilter.Process(chunk[i]);

                // バンドパス（利用可能な場合）
                if (bandpassFilterLow != null && bandpassFilterHigh != null)
                {
                    sample = (float)bandpassFilterLow.Process(sample);
                    sample = (float)bandpassFilterHigh.Process(sample);
                }

                output[i] = sample;
            }

            return output;
        }

        /// <summary>
        /// フィルター状態をリセット
        /// </summary>
        public void Reset()
        {
            preEmphasisFilter.Reset();
            if (bandpassFilterLow != null)
            {
                bandpassFilterLow.Reset();
            }
            if (bandpassFilterHigh != null)
            {
                bandpassFilterHigh.Reset();
            }
        }

        #endregion
    }
}