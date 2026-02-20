using UnityEngine;
using TapHouse.Logging;

public class LayerBarkSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip barkSound;
    [SerializeField] private Animator animator;

    private bool hasPlayedBarkSound = false;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                GameLogger.LogError(LogCategory.Audio,"AudioSource が見つかりません！");
            }
        }
    }

    void Update()
    {
        if (animator.GetCurrentAnimatorStateInfo(1).IsName("050_Expression_Bark2"))
        {
            if (!hasPlayedBarkSound)
            {
                PlayBarkSound();
                hasPlayedBarkSound = true;
            }
        }
        else
        {
            hasPlayedBarkSound = false;
        }
    }

    public void PlayBarkSound()
    {
        if (audioSource != null && barkSound != null)
        {
            audioSource.PlayOneShot(barkSound);
            GameLogger.Log(LogCategory.Audio,"BarkSound を再生しました。");
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Audio,"AudioSource または BarkSound が設定されていません。");
        }
    }
}
