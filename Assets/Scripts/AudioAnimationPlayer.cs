using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class AudioAnimationPlayer : MonoBehaviour
{
    private AudioSource _audioSource;
    private Animator _animator;

    [SerializeField]
    private double syncDelay = 0.1f;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponent<Animator>();
    }

    public void Play(AudioClip audioClip, List<IAnimatorParameter> parameters)
    {
        var _ = PlayAsync(audioClip, parameters);
    }

    public async Task PlayAsync(AudioClip audioClip, List<IAnimatorParameter> parameters)
    {
        var startTime = UnityEngine.AudioSettings.dspTime + syncDelay;

        PlaySound(audioClip, startTime);

        await Task.Delay((int)(syncDelay * 1000));

        PlayAnimation(parameters);
    }

    private void PlayAnimation(List<IAnimatorParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            parameter.Apply(_animator);
        }
    }

    private void PlaySound(AudioClip clip, double startTime)
    {
        if (clip == null)
        {
            return;
        }

        if (_audioSource == null)
        {
            return;
        }

        StopSound();

        _audioSource.clip = clip;
        _audioSource.volume = GameAudioSettings.Instance.IdleAnimationVolume;
        _audioSource.PlayScheduled(startTime);
    }

    private void StopSound()
    {
        if (_audioSource == null)
        {
            return;
        }

        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }


}
