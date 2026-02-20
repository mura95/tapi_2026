using UnityEngine;
using TapHouse.Logging;

public class MaskLayerSoundManager : StateMachineBehaviour
{
    [SerializeField] private AudioClip barkSound; // 吠える音
    [SerializeField] private AudioClip loopSound; // ループ音

    private AudioSource audioSource;
    private AudioSource loopAudioSource;
    private bool isLoopPlaying = false; // ループ音が再生中か判定

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // `AudioSource` の取得（barkSound 用）
        if (audioSource == null)
        {
            audioSource = animator.gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                GameLogger.LogError(LogCategory.Audio,"AudioSource が " + animator.gameObject.name + " にアタッチされていません！");
                return;
            }
        }

        // `loopSound` 用の `AudioSource` を取得（無ければ新しく追加）
        if (loopAudioSource == null)
        {
            loopAudioSource = animator.gameObject.AddComponent<AudioSource>();
            loopAudioSource.loop = true;
            loopAudioSource.playOnAwake = false;
        }

        // `Weight` が `1` のとき `barkSound` を再生
        float layerWeight = animator.GetLayerWeight(layerIndex);
        if (layerWeight >= 1f && stateInfo.IsName("050_Expression_Bark2") && barkSound != null)
        {
            audioSource.PlayOneShot(barkSound);
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float layerWeight = animator.GetLayerWeight(layerIndex);

        // `Weight == 1` のとき `loopSound` を再生、すでに再生されていればスキップ
        if (layerWeight >= 1f)
        {
            if (!isLoopPlaying && loopSound != null)
            {
                loopAudioSource.clip = loopSound;
                loopAudioSource.Play();
                isLoopPlaying = true;
            }
        }
        else
        {
            // `Weight < 1` ならループ音を停止
            if (isLoopPlaying)
            {
                loopAudioSource.Stop();
                isLoopPlaying = false;
            }
        }
    }
}
