using System.Threading.Tasks;
using UnityEngine;

public class DogBehaviourBase : StateMachineBehaviour
{

    [SerializeField]
    private DogAnimationGroupBase dogAnimationGroup;
    private DogAnimationGroupBase.DogAnimationOption _dogAnimationOption;
    private bool _isTransition;
    private bool _isLoopAnimation;
    private float _idleTime;

    private AudioSource _audioSource;

    private int _transitionAnimationTypeHash;

    // サウンドとアニメーションを同期させる為の遅延時間
    private const double SyncDelay = 0.1f;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Initialize(animator, stateInfo);
    }

    private void Initialize(Animator animator, AnimatorStateInfo stateInfo)
    {
        _isTransition = false;

        _isLoopAnimation = IsLoopAnimation(stateInfo);

        _transitionAnimationTypeHash = Animator.StringToHash(dogAnimationGroup.transitionAnimationTypeName);

        animator.SetInteger(_transitionAnimationTypeHash, 0);

        if (_isLoopAnimation)
        {
            _audioSource = animator.gameObject.GetComponent<AudioSource>();

            _idleTime = dogAnimationGroup.WaitLoopTime;

            var emotionState = GetEmotionState();

            _dogAnimationOption = dogAnimationGroup.GetRandomOneShotAnimationByEmotion(emotionState);
        }
        else
        {
            // 単発アニメーションからの戻りのループモーションの指定
            var loopAnimationOption = dogAnimationGroup.GetRandomLoopAnimation();
            animator.SetInteger("IdleLoopType", loopAnimationOption.animationType);
        }
    }

    bool IsLoopAnimation(AnimatorStateInfo stateInfo)
    {
        foreach (var loopAnimationStateFullPath in dogAnimationGroup.loopAnimationStateFullPaths)
        {
            var loopAnimationHash = Animator.StringToHash(loopAnimationStateFullPath);

            if (loopAnimationHash == stateInfo.fullPathHash)
            {
                return true;
            }
        }

        return false;
    }

    EmotionState GetEmotionState()
    {
        EmotionState emotionState = EmotionState.None;

        if (GlobalVariables.CurrentHungerState == HungerState.Hungry)
        {
            emotionState |= EmotionState.Hungry;
        }

        return emotionState;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!_isLoopAnimation)
        {
            return;
        }

        if (_isTransition)
        {
            return;
        }

        _idleTime -= Time.deltaTime;

        if (_idleTime <= 0.0f)
        {
            var _ = PlayAnimationAndSound(animator);

            _isTransition = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
    }

    private async Task PlayAnimationAndSound(Animator animator)
    {
        var startTime = UnityEngine.AudioSettings.dspTime + SyncDelay;

        PlaySound(_dogAnimationOption.audioClip, startTime);

        await Task.Delay((int)(SyncDelay * 1000));

        PlayAnimation(animator, _dogAnimationOption.animationType);
    }

    private void PlayAnimation(Animator animator, int animationType)
    {
        animator.SetInteger(_transitionAnimationTypeHash, animationType);
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
