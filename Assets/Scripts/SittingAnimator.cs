using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SittingAnimator : StateMachineBehaviour
{
    [SerializeField]
    private float _timeUntilBored;

    [SerializeField]
    private int _numberOfBoredAnimations;

    private bool _isBored;
    private float _idleTime;
    private int _boredAnimation;
    private bool _soundPlayed = false;
    private AudioSource audioSource;
    private Dictionary<int, AudioClip> soundMap;

    [SerializeField]
    private AudioClip DogSounds267;

    [SerializeField]
    private AudioClip DogSounds273;

    [SerializeField]
    private AudioClip DogSounds275;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ResetIdle();
        audioSource = animator.gameObject.GetComponent<AudioSource>();
        InitializeSoundMap();
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_isBored == false)
        {
            _idleTime += Time.deltaTime;

            if (_idleTime > _timeUntilBored && stateInfo.normalizedTime % 1 < 0.02f)
            {
                _isBored = true;
                _boredAnimation = Random.Range(1, _numberOfBoredAnimations + 1);
                _boredAnimation = _boredAnimation * 2 - 1;
                animator.SetFloat("SitAnimation", _boredAnimation - 1);
            }
            if (!_soundPlayed && (soundMap.ContainsKey(_boredAnimation)))
            {
                PlaySound(_boredAnimation);
                _soundPlayed = true;
            }
        }
        else if (stateInfo.normalizedTime % 1 > 0.98)
        {
            ResetIdle();
        }
        animator.SetFloat("SitAnimation", _boredAnimation, 0.2f, Time.deltaTime);
        if (Mathf.Abs(animator.GetFloat("SitAnimation") - _boredAnimation) < 0.01f)
        {
            animator.SetFloat("SitAnimation", _boredAnimation);
        }
    }

    private void ResetIdle()
    {
        if (_isBored)
        {
            _boredAnimation--;
        }

        _isBored = false;
        _idleTime = 0;
        _soundPlayed = false;
    }
    private void PlaySound(int soundNumber)
    {
        if (audioSource != null && soundMap.ContainsKey(soundNumber))
        {
            audioSource.clip = soundMap[soundNumber];
            audioSource.volume = GameAudioSettings.Instance.SittingAnimationVolume;
            audioSource.Play();
        }
    }
    private void InitializeSoundMap()
    {
        soundMap = new Dictionary<int, AudioClip>
        {
            { 3, DogSounds267 },
            { 9, DogSounds273 },
            { 10, DogSounds275 },
            { 2, DogSounds275 },
            { 6, DogSounds275 },
        };
    }
}
