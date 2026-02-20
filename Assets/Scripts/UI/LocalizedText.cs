using UnityEngine;
using TMPro;
using UnityEngine.UI;
using TapHouse.Logging;

/// <summary>
/// TextMeshProUGUIやTextに自動的に翻訳テキストを適用するコンポーネント
/// </summary>
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;

    private TextMeshProUGUI tmpText;
    private Text unityText;

    private void Awake()
    {
        // TextMeshProUGUIまたはTextコンポーネントを取得
        tmpText = GetComponent<TextMeshProUGUI>();
        unityText = GetComponent<Text>();
    }

    private void Start()
    {
        // LocalizationManagerの準備ができるまで待つ
        if (LocalizationManager.Instance != null)
        {
            RegisterAndUpdate();
        }
        else
        {
            Invoke(nameof(RegisterAndUpdate), 0.1f);
        }
    }

    private void RegisterAndUpdate()
    {
        if (LocalizationManager.Instance != null)
        {
            // 言語変更イベントに登録
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;

            // 初回のテキスト更新
            UpdateText();
        }
    }

    private void OnDestroy()
    {
        // イベントから登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    /// <summary>
    /// テキストを更新
    /// </summary>
    private void UpdateText()
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            GameLogger.LogWarning(LogCategory.UI, $"[LocalizedText] Localization key is not set on {gameObject.name}");
            return;
        }

        // コンポーネントがまだ取得されていない場合は取得
        if (tmpText == null && unityText == null)
        {
            tmpText = GetComponent<TextMeshProUGUI>();
            unityText = GetComponent<Text>();
        }

        string translatedText = LocalizationManager.Instance.GetText(localizationKey);

        GameLogger.Log(LogCategory.UI, $"[LocalizedText] Updating text on '{gameObject.name}' - Key: '{localizationKey}' -> Text: '{translatedText}'");

        // TextMeshProUGUIまたはTextに適用
        if (tmpText != null)
        {
            tmpText.text = translatedText;
            GameLogger.Log(LogCategory.UI, $"[LocalizedText] Updated TextMeshProUGUI on '{gameObject.name}'");
        }
        else if (unityText != null)
        {
            unityText.text = translatedText;
            GameLogger.Log(LogCategory.UI, $"[LocalizedText] Updated Text on '{gameObject.name}'");
        }
        else
        {
            GameLogger.LogError(LogCategory.UI, $"[LocalizedText] No Text or TextMeshProUGUI component found on '{gameObject.name}'");
        }
    }

    /// <summary>
    /// ローカライゼーションキーを設定
    /// </summary>
    public void SetLocalizationKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }

    /// <summary>
    /// 現在のローカライゼーションキーを取得
    /// </summary>
    public string GetLocalizationKey()
    {
        return localizationKey;
    }

#if UNITY_EDITOR
    // エディタでキーを変更したときに即座に反映
    private void OnValidate()
    {
        if (Application.isPlaying && LocalizationManager.Instance != null)
        {
            UpdateText();
        }
    }
#endif
}