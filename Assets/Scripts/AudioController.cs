// This script processes audio input from the microphone to detect loud sounds in real-time.
// It uses waveform data to calculate the audio level, scales a cube object visually based on audio amplitude,
// and triggers specific actions when the audio exceeds a threshold.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For UI components
using System.Linq;
using TapHouse.Logging;

public class AudioController : MonoBehaviour
{
    private AudioClip m_AudioClip; // Holds the audio recorded from the microphone
    private int m_LastAudioPos; // Keeps track of the last audio sample position
    private float m_AudioLevel; // Current audio level
    public DogController characterController; // Reference to the DogController for triggering actions

    [SerializeField] private GameObject m_Cube; // Cube object to visualize audio amplitude
    [SerializeField, Range(10, 100)] private float m_AmpGain = 10; // Amplitude gain for scaling the cube

    [SerializeField, Range(0.1f, 1.0f)] private float m_AudioThreshold = 0.2f; // Threshold for loud sound detection

    private float lastTriggerTime = 0f; // Last time a loud sound was detected
    private float triggerCooldown = 1.0f; // Minimum interval between triggers in seconds

    void Start()
    {
        GameLogger.Log(LogCategory.Audio,$"=== Using Default Microphone ===");
        string microphoneDevice = Microphone.devices.FirstOrDefault();
        if (string.IsNullOrEmpty(microphoneDevice))
        {
            GameLogger.LogError(LogCategory.Audio,"No microphone found!");
            enabled = false; // Disable the script if no microphone is available
            return;
        }

        GameLogger.Log(LogCategory.Audio,$"Microphone device: {microphoneDevice}");

        // Start the microphone with a 10-second buffer and a sample rate of 48000 Hz
        m_AudioClip = Microphone.Start(microphoneDevice, true, 10, 48000);
    }

    void Update()
    {
        int nowAudioPos = Microphone.GetPosition(null);
        if (nowAudioPos == m_LastAudioPos) return; // Skip processing if the microphone position hasn't changed

        float[] waveData = GetUpdatedAudio();
        if (waveData.Length == 0) return;

        // Calculate the audio level using the average of absolute waveform values
        m_AudioLevel = waveData.Average(Mathf.Abs);
        // Update the cube's scale based on the audio level
        m_Cube.transform.localScale = new Vector3(0.2f, 0.2f + m_AmpGain * m_AudioLevel, 0.2f);

        if (m_AudioLevel > m_AudioThreshold)
        {
            ShowLoudSoundMessage(); // Trigger actions for loud sounds
        }
    }

    private float[] GetUpdatedAudio()
    {
        int nowAudioPos = Microphone.GetPosition(null);
        int audioBuffer = m_AudioClip.samples * m_AudioClip.channels;

        if (nowAudioPos == m_LastAudioPos)
            return Array.Empty<float>();

        int audioCount = (nowAudioPos > m_LastAudioPos) ?
            nowAudioPos - m_LastAudioPos :
            (audioBuffer - m_LastAudioPos) + nowAudioPos;

        float[] waveData = new float[audioCount];
        if (nowAudioPos > m_LastAudioPos)
        {
            m_AudioClip.GetData(waveData, m_LastAudioPos);
        }
        else
        {
            float[] wave1 = new float[audioBuffer - m_LastAudioPos];
            m_AudioClip.GetData(wave1, m_LastAudioPos);

            float[] wave2 = new float[nowAudioPos];
            m_AudioClip.GetData(wave2, 0);

            wave1.CopyTo(waveData, 0);
            wave2.CopyTo(waveData, wave1.Length);
        }

        m_LastAudioPos = nowAudioPos;
        return waveData;
    }

    private void ShowLoudSoundMessage()
    {
        if (Time.time - lastTriggerTime < triggerCooldown) return; // Skip if within the cooldown period
        lastTriggerTime = Time.time;

        GameLogger.Log(LogCategory.Audio,"Loud sound detected!");
    }
}
