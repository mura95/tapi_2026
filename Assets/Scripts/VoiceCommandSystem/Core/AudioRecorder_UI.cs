using UnityEngine;

namespace VoiceCommandSystem.Core
{
    /// <summary>
    /// AudioRecorder - UI表示
    /// </summary>
    public partial class AudioRecorder
    {
        #region UI Drawing

        /// <summary>
        /// PTT状態とマイク選択UIを表示
        /// </summary>
        private void DrawUI()
        {
            DrawRecordingStatus();
            DrawMicrophoneSelector();
        }

        /// <summary>
        /// 録音状態表示（PTT/VAD両対応）
        /// </summary>
        private void DrawRecordingStatus()
        {
            // 画面左上に表示
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperLeft;

            string status = "";
            Color statusColor = Color.gray;

            if (recordingMode == RecordingMode.PushToTalk)
            {
                // PTTモード
                statusColor = isPTTPressed ? Color.red : Color.gray;
                status = isPTTPressed ? "🎤 RECORDING" : $"Press [{pushToTalkKey}] to talk";
            }
            else if (recordingMode == RecordingMode.ContinuousVAD)
            {
                // VADモード
                if (IsVADActive)
                {
                    statusColor = Color.red;
                    status = "🎤 RECORDING (VAD)";
                }
                else
                {
                    statusColor = Color.green;
                    status = "👂 Listening...";
                }
            }

            style.normal.textColor = statusColor;
            GUI.Label(new Rect(10, 10, 400, 30), status, style);

            // モード表示
            GUIStyle modeStyle = new GUIStyle(GUI.skin.label);
            modeStyle.fontSize = 14;
            modeStyle.normal.textColor = Color.yellow;
            string modeText = recordingMode == RecordingMode.PushToTalk ? "Mode: Push-to-Talk" : "Mode: Continuous (VAD)";
            GUI.Label(new Rect(10, 40, 400, 25), modeText, modeStyle);

            // 録音時間表示（PTTモード）
            if (recordingMode == RecordingMode.PushToTalk && isPTTPressed && isRecording && micClip != null)
            {
                int currentPos = Microphone.GetPosition(microphoneDevice);
                float duration = currentPos / (float)sampleRate;
                GUI.Label(new Rect(10, 65, 400, 30), $"Duration: {duration:F1}s", style);
            }

            // 保存情報表示
            if (autoSaveRecordings)
            {
                GUIStyle smallStyle = new GUIStyle(GUI.skin.label);
                smallStyle.fontSize = 14;
                smallStyle.normal.textColor = Color.white;
                int yPos = recordingMode == RecordingMode.PushToTalk ? 95 : 70;
                GUI.Label(new Rect(10, yPos, 400, 25), $"💾 Auto-save: ON ({recordingCounter} files)", smallStyle);
            }

            // 処理状態表示
            if (applyRealtimeProcessing || applyProcessing)
            {
                GUIStyle filterStyle = new GUIStyle(GUI.skin.label);
                filterStyle.fontSize = 12;
                filterStyle.normal.textColor = Color.cyan;
                int yPos = autoSaveRecordings ? 120 : 95;
                string processingText = "";
                if (applyRealtimeProcessing) processingText += "Realtime ";
                if (applyProcessing) processingText += "Final";
                GUI.Label(new Rect(10, yPos, 400, 25), $"🔊 Processing: {processingText.Trim()}", filterStyle);
            }
        }

        /// <summary>
        /// マイク選択UI
        /// </summary>
        private void DrawMicrophoneSelector()
        {
            string[] devices = Microphone.devices;
            if (devices.Length <= 1)
                return; // マイクが1つ以下なら表示不要

            // 画面右上に表示
            float x = Screen.width - 310;
            float y = 10;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.alignment = TextAnchor.UpperLeft;
            GUI.Box(new Rect(x, y, 300, 100 + devices.Length * 25), "🎤 Microphone Selection", boxStyle);

            y += 30;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 12;
            labelStyle.normal.textColor = Color.white;

            for (int i = 0; i < devices.Length; i++)
            {
                bool isSelected = (i == selectedMicrophoneIndex);
                string deviceName = devices[i];

                // デバイス名が長い場合は短縮
                if (deviceName.Length > 30)
                {
                    deviceName = deviceName.Substring(0, 27) + "...";
                }

                string buttonText = isSelected ? $"● {deviceName}" : $"○ {deviceName}";

                if (GUI.Button(new Rect(x + 10, y, 280, 20), buttonText))
                {
                    // VADモードでは変更不可
                    if (recordingMode != RecordingMode.ContinuousVAD && !isRecording)
                    {
                        SelectMicrophone(i);
                    }
                }

                y += 25;
            }

            // 録音中の警告
            if (isRecording || recordingMode == RecordingMode.ContinuousVAD)
            {
                GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
                warningStyle.fontSize = 10;
                warningStyle.normal.textColor = Color.yellow;
                string warning = recordingMode == RecordingMode.ContinuousVAD
                    ? "⚠️ Change mode to switch mic"
                    : "⚠️ Stop recording to change mic";
                GUI.Label(new Rect(x + 10, y, 280, 20), warning, warningStyle);
            }
        }

        #endregion
    }
}