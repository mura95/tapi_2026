using UnityEngine;
using TapHouse.Logging;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// AudioRecorder - マイク管理
    /// </summary>
    public partial class AudioRecorder
    {
        #region Microphone Management

        /// <summary>
        /// マイクデバイスを初期化
        /// </summary>
        private void InitializeMicrophone()
        {
            string[] devices = Microphone.devices;

            if (devices.Length == 0)
            {
                GameLogger.LogError(LogCategory.Voice,"[AudioRecorder] No microphone found!");
                return;
            }

            // インデックスが範囲外の場合は0にリセット
            if (selectedMicrophoneIndex < 0 || selectedMicrophoneIndex >= devices.Length)
            {
                selectedMicrophoneIndex = 0;
            }

            microphoneDevice = devices[selectedMicrophoneIndex];

            Log($"Available microphones: {devices.Length}");
            for (int i = 0; i < devices.Length; i++)
            {
                string marker = (i == selectedMicrophoneIndex) ? " ← Selected" : "";
                Log($"  [{i}] {devices[i]}{marker}");
            }
        }

        /// <summary>
        /// マイクを選択
        /// </summary>
        public void SelectMicrophone(int index)
        {
            if (isRecording)
            {
                GameLogger.LogWarning(LogCategory.Voice,"[AudioRecorder] Cannot change microphone while recording");
                return;
            }

            string[] devices = Microphone.devices;
            if (index < 0 || index >= devices.Length)
            {
                GameLogger.LogError(LogCategory.Voice,$"[AudioRecorder] Invalid microphone index: {index}");
                return;
            }

            selectedMicrophoneIndex = index;
            microphoneDevice = devices[index];

            Log($"Microphone changed to [{index}] {microphoneDevice}");
        }

        /// <summary>
        /// 利用可能なマイクデバイス一覧を取得
        /// </summary>
        public static string[] GetAvailableDevices()
        {
            return Microphone.devices;
        }

        #endregion
    }
}