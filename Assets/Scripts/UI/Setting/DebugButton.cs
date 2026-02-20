using UnityEngine;
using UnityEngine.UI;
using TapHouse.Logging;

/// <summary>
/// 管理画面（設定画面）にあるDebugButtonを管理
/// ボタンをクリックするとDebugCanvasを表示/非表示する
/// </summary>
public class DebugButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DebugCanvasManager debugCanvasManager;
    [SerializeField] private Toggle debugToggle;

    private void Awake()
    {
        debugToggle = GetComponent<Toggle>();

        if (debugToggle != null)
        {
            debugToggle.onValueChanged.AddListener(OnDebugToggleChanged);
        }
        else
        {
            GameLogger.LogError(LogCategory.UI, "[DebugButton] Toggle component not found!");
        }
    }

    private void Start()
    {
        // debugCanvasManagerが未設定の場合、シーンから探す
        if (debugCanvasManager == null)
        {
            debugCanvasManager = FindObjectOfType<DebugCanvasManager>();

            if (debugCanvasManager == null)
            {
                GameLogger.LogWarning(LogCategory.UI, "[DebugButton] DebugCanvasManager が見つかりません。" + "Inspector から DebugCanvasManager を設定してください。");
            }
        }
    }

    /// <summary>
    /// デバッグボタンがクリックされた時の処理
    /// </summary>
    private void OnDebugToggleChanged(bool isOn)
    {
        if (debugCanvasManager != null)
        {
            if (isOn)
            {
                debugCanvasManager.ShowDebugCanvas();
            }
            else
            {
                debugCanvasManager.HideDebugCanvas();
            }
            GameLogger.Log(LogCategory.UI, "[DebugButton] Debug Canvas をトグルしました");
        }
        else
        {
            GameLogger.LogWarning(LogCategory.UI, "[DebugButton] DebugCanvasManager が設定されていません！");
        }
    }

    private void OnDestroy()
    {
        if (debugToggle != null)
        {
            debugToggle.onValueChanged.RemoveListener(OnDebugToggleChanged);
        }
    }
}