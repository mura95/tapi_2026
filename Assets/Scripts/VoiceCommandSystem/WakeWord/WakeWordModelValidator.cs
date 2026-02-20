using Unity.InferenceEngine;
using UnityEngine;
using System.Diagnostics;
using TapHouse.Logging;

namespace VoiceCommandSystem.WakeWord
{
    /// <summary>
    /// ONNXモデルの検証とベンチマーク (Inference Engine版)
    /// Unity 6対応
    /// </summary>
    public class WakeWordModelValidator : MonoBehaviour
    {
        [Header("Model")]
        [SerializeField] private ModelAsset modelAsset;

        [Header("Benchmark Settings")]
        [SerializeField] private int benchmarkIterations = 100;
        [SerializeField] private bool runOnStart = false;

        private void Start()
        {
            if (runOnStart)
            {
                ValidateAndBenchmark();
            }
        }

        [ContextMenu("Validate Model")]
        public void ValidateAndBenchmark()
        {
            GameLogger.Log(LogCategory.Voice,"=== Wake Word Model Validation (Inference Engine) ===\n");

            if (modelAsset == null)
            {
                GameLogger.LogError(LogCategory.Voice,"❌ Model not assigned!");
                return;
            }

            // 1. モデル情報の表示
            ValidateModelStructure();

            // 2. 推論テスト
            TestInference();

            // 3. パフォーマンステスト
            BenchmarkPerformance();

            GameLogger.Log(LogCategory.Voice,"\n=== Validation Complete ===");
        }

        private void ValidateModelStructure()
        {
            GameLogger.Log(LogCategory.Voice,"--- Model Structure ---");

            try
            {
                Model runtimeModel = ModelLoader.Load(modelAsset);

                // 入力情報
                GameLogger.Log(LogCategory.Voice,$"Input count: {runtimeModel.inputs.Count}");
                foreach (var input in runtimeModel.inputs)
                {
                    GameLogger.Log(LogCategory.Voice,$"  Input: {input.name}");
                    GameLogger.Log(LogCategory.Voice,$"  Shape: {input.shape}");
                }

                // 出力情報
                GameLogger.Log(LogCategory.Voice,$"\nOutput count: {runtimeModel.outputs.Count}");
                foreach (var output in runtimeModel.outputs)
                {
                    GameLogger.Log(LogCategory.Voice,$"  Output: {output.name}");
                }

                GameLogger.Log(LogCategory.Voice,"✓ Model structure validated\n");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"❌ Model structure validation failed: {e.Message}");
            }
        }

        private void TestInference()
        {
            GameLogger.Log(LogCategory.Voice,"--- Inference Test ---");

            Model runtimeModel = ModelLoader.Load(modelAsset);
            Worker worker = new Worker(runtimeModel, BackendType.CPU);

            // ダミー入力 (40次元 - MFCC特徴量)
            float[] inputData = new float[40];
            for (int i = 0; i < 40; i++)
            {
                inputData[i] = UnityEngine.Random.Range(-1f, 1f);
            }

            Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 40), inputData);

            try
            {
                var startTime = Time.realtimeSinceStartup;

                // 推論実行
                worker.Schedule(inputTensor);
                var output = worker.PeekOutput() as Tensor<float>;

                // データ取得
                float[] outputData = output.DownloadToArray();

                var inferenceTime = (Time.realtimeSinceStartup - startTime) * 1000f;

                GameLogger.Log(LogCategory.Voice,$"Input shape: (1, 40)");
                GameLogger.Log(LogCategory.Voice,$"Output shape: {output.shape}");
                GameLogger.Log(LogCategory.Voice,$"Output values: [{outputData[0]:F4}, {outputData[1]:F4}]");
                GameLogger.Log(LogCategory.Voice,$"Inference time: {inferenceTime:F2} ms");

                // Softmax確率計算
                float exp0 = Mathf.Exp(outputData[0]);
                float exp1 = Mathf.Exp(outputData[1]);
                float prob = exp1 / (exp0 + exp1);
                GameLogger.Log(LogCategory.Voice,$"Wake word probability: {prob:F4}");

                GameLogger.Log(LogCategory.Voice,"✓ Inference test passed\n");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError(LogCategory.Voice,$"❌ Inference test failed: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                inputTensor.Dispose();
                worker.Dispose();
            }
        }

        private void BenchmarkPerformance()
        {
            GameLogger.Log(LogCategory.Voice,"--- Performance Benchmark ---");
            GameLogger.Log(LogCategory.Voice,$"Iterations: {benchmarkIterations}\n");

            Model runtimeModel = ModelLoader.Load(modelAsset);

            // CPU テスト
            TestBackend(BackendType.CPU, runtimeModel, "CPU");

            // GPU テスト
            if (SystemInfo.supportsComputeShaders)
            {
                TestBackend(BackendType.GPUCompute, runtimeModel, "GPU (Compute)");
            }
            else
            {
                GameLogger.Log(LogCategory.Voice,"GPU (Compute): Not supported on this device");
            }
        }

        private void TestBackend(BackendType backend, Model model, string name)
        {
            Worker worker = new Worker(model, backend);

            // ダミー入力 (40次元)
            float[] inputData = new float[40];
            for (int i = 0; i < 40; i++)
            {
                inputData[i] = UnityEngine.Random.Range(-1f, 1f);
            }
            Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 40), inputData);

            // ウォームアップ
            worker.Schedule(inputTensor);
            worker.PeekOutput();

            // ベンチマーク
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < benchmarkIterations; i++)
            {
                worker.Schedule(inputTensor);
                var output = worker.PeekOutput();
            }

            stopwatch.Stop();

            float totalMs = stopwatch.ElapsedMilliseconds;
            float avgMs = totalMs / benchmarkIterations;
            float fps = 1000f / avgMs;

            GameLogger.Log(LogCategory.Voice,$"{name}:");
            GameLogger.Log(LogCategory.Voice,$"  Total time: {totalMs:F1} ms");
            GameLogger.Log(LogCategory.Voice,$"  Average: {avgMs:F2} ms/inference");
            GameLogger.Log(LogCategory.Voice,$"  FPS equivalent: {fps:F1}");
            GameLogger.Log(LogCategory.Voice,$"  CPU usage estimate: ~{avgMs / 16.67f * 100f:F1}% (60 FPS target)");

            // パフォーマンス評価
            if (avgMs < 5f)
            {
                GameLogger.Log(LogCategory.Voice,$"  Performance: ✅ Excellent (< 5ms)");
            }
            else if (avgMs < 10f)
            {
                GameLogger.Log(LogCategory.Voice,$"  Performance: ✓ Good (< 10ms)");
            }
            else if (avgMs < 20f)
            {
                GameLogger.Log(LogCategory.Voice,$"  Performance: ⚠️ Acceptable (< 20ms)");
            }
            else
            {
                GameLogger.Log(LogCategory.Voice,$"  Performance: ❌ Too Slow (> 20ms)");
            }

            GameLogger.Log(LogCategory.Voice,"");

            inputTensor.Dispose();
            worker.Dispose();
        }

        [ContextMenu("Test Feature Extraction")]
        public void TestFeatureExtraction()
        {
            GameLogger.Log(LogCategory.Voice,"=== Feature Extraction Test ===\n");

            // 1秒分のサイン波を生成（440Hz = A4音）
            int sampleRate = 16000;
            float[] audio = new float[sampleRate];
            for (int i = 0; i < sampleRate; i++)
            {
                audio[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / sampleRate) * 0.5f;
            }

            GameLogger.Log(LogCategory.Voice,$"Generated test audio: {audio.Length} samples");
            GameLogger.Log(LogCategory.Voice,$"Sample rate: {sampleRate} Hz");
            GameLogger.Log(LogCategory.Voice,$"Duration: 1.0 second");
            GameLogger.Log(LogCategory.Voice,$"Frequency: 440 Hz (A4 note)");

            // 簡易MFCC抽出（実際のWakeWordDetectorと同じロジック）
            float[] features = ExtractSimpleMFCC(audio);

            GameLogger.Log(LogCategory.Voice,$"\nExtracted features: {features.Length} dimensions");
            GameLogger.Log(LogCategory.Voice,$"Feature values (first 10):");
            for (int i = 0; i < Mathf.Min(10, features.Length); i++)
            {
                GameLogger.Log(LogCategory.Voice,$"  [{i}] = {features[i]:F4}");
            }

            // 特徴量でモデル推論
            if (modelAsset != null)
            {
                GameLogger.Log(LogCategory.Voice,"\nRunning inference with extracted features...");
                TestInferenceWithFeatures(features);
            }

            GameLogger.Log(LogCategory.Voice,"\n✓ Feature extraction test complete");
        }

        private float[] ExtractSimpleMFCC(float[] audio)
        {
            const int N_MFCC = 20;
            const int HOP_LENGTH = 512;

            int frameCount = audio.Length / HOP_LENGTH;
            float[] mfccMeans = new float[N_MFCC];

            for (int coef = 0; coef < N_MFCC; coef++)
            {
                float sum = 0f;
                for (int f = 0; f < frameCount; f++)
                {
                    int start = f * HOP_LENGTH;
                    int end = Mathf.Min(start + HOP_LENGTH, audio.Length);

                    float energy = 0f;
                    for (int i = start; i < end; i++)
                    {
                        energy += audio[i] * audio[i];
                    }
                    energy /= (end - start);

                    sum += Mathf.Log(energy + 1e-9f);
                }
                mfccMeans[coef] = sum / frameCount;
            }

            // Delta計算
            float[] delta = new float[N_MFCC];
            for (int i = 0; i < N_MFCC; i++)
            {
                if (i == 0)
                    delta[i] = mfccMeans[1] - mfccMeans[0];
                else if (i == N_MFCC - 1)
                    delta[i] = mfccMeans[i] - mfccMeans[i - 1];
                else
                    delta[i] = (mfccMeans[i + 1] - mfccMeans[i - 1]) / 2f;
            }

            // 結合
            float[] features = new float[40];
            System.Array.Copy(mfccMeans, 0, features, 0, N_MFCC);
            System.Array.Copy(delta, 0, features, N_MFCC, N_MFCC);

            return features;
        }

        private void TestInferenceWithFeatures(float[] features)
        {
            Model runtimeModel = ModelLoader.Load(modelAsset);
            Worker worker = new Worker(runtimeModel, BackendType.CPU);

            Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 40), features);

            try
            {
                worker.Schedule(inputTensor);
                var output = worker.PeekOutput() as Tensor<float>;

                float[] outputData = output.DownloadToArray();

                float exp0 = Mathf.Exp(outputData[0]);
                float exp1 = Mathf.Exp(outputData[1]);
                float prob = exp1 / (exp0 + exp1);

                GameLogger.Log(LogCategory.Voice,$"Model output: [{outputData[0]:F4}, {outputData[1]:F4}]");
                GameLogger.Log(LogCategory.Voice,$"Wake word probability: {prob:F4} ({prob * 100f:F1}%)");

                if (prob > 0.85f)
                {
                    GameLogger.Log(LogCategory.Voice,"✅ Would trigger wake word detection!");
                }
                else if (prob > 0.5f)
                {
                    GameLogger.Log(LogCategory.Voice,"⚠️ Close to threshold");
                }
                else
                {
                    GameLogger.Log(LogCategory.Voice,"❌ Below detection threshold");
                }
            }
            finally
            {
                inputTensor.Dispose();
                worker.Dispose();
            }
        }

        [ContextMenu("Test with Real Audio")]
        public void TestWithRealAudio()
        {
            GameLogger.Log(LogCategory.Voice,"=== Real Audio Test ===");
            GameLogger.Log(LogCategory.Voice,"マイクから音声を取得してテスト中...\n");

            if (!Microphone.IsRecording(null))
            {
                GameLogger.LogWarning(LogCategory.Voice,"マイクが起動していません。AudioRecorderを起動してください。");
                return;
            }

            // 実装は AudioRecorder と連携
            GameLogger.Log(LogCategory.Voice,"WakeWordDetectorコンポーネントと連携してテストしてください。");
        }
    }
}