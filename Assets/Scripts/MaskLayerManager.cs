using System.Collections;
using UnityEngine;
using TapHouse.Logging;

public class MaskLayerManager : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private DogController _dogController;

    public void SetLayerWeight(string layerName, float weight, float duration = 0f)
    {
        if (_dogController.GetLieOnBackBool()) return;
        if (layerName == "Bark")
        {
            BackStart();
            layerName = "face";
        }
        // Animator からレイヤーインデックスを取得
        int layerIndex = animator.GetLayerIndex(layerName);
        if (layerIndex < 0)
        {
            GameLogger.LogError(LogCategory.General,$"Layer '{layerName}' not found in Animator.");
            return;
        }
        // レイヤーのウェイトを設定
        animator.SetLayerWeight(layerIndex, weight);
        // 指定された時間後にリセットする場合
        if (duration > 0f)
        {
            StartCoroutine(ResetLayerWeightAfterTime(layerIndex, duration));
        }
    }
    private IEnumerator ResetLayerWeightAfterTime(int layerIndex, float duration)
    {
        yield return new WaitForSeconds(duration);
        animator.SetLayerWeight(layerIndex, 0);
    }
    private void BackStart()
    {
        _dogController.LayerBarkTrigger();
    }
}
