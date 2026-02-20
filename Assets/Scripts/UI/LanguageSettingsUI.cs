using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TapHouse.Logging;

/// <summary>
/// 設定画面での言語切り替えUI（トグルのみ版・デバッグ強化版）
/// トグルで言語を選択: OFF(false) = 日本語, ON(true) = English
/// </summary>
public class LanguageSettingsUI : MonoBehaviour
{
    [Header("Language Control")]
    [SerializeField] private Toggle languageToggle;

    private bool isInitialized = false;

    private void Awake()
    {
        GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] Awake called");

        // Toggleの参照確認
        if (languageToggle == null)
        {
            GameLogger.LogError(LogCategory.UI, "[LanguageSettingsUI] languageToggle is NULL! Please assign Toggle in Inspector.");
        }
        else
        {
            GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] Toggle found: {languageToggle.name}");
        }
    }

    private void Start()
    {
        GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] Start called");
        InitializeLanguageUI();
    }

    private void OnEnable()
    {
        GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] OnEnable called");
        // パネルが開かれたときにも初期化（2回目以降は設定を反映するため）
        if (isInitialized && LocalizationManager.Instance != null)
        {
            UpdateToggleFromLanguage();
        }
    }

    private void OnDestroy()
    {
        GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] OnDestroy called");
        // トグルのイベントを解除
        if (languageToggle != null)
        {
            languageToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }

    /// <summary>
    /// 言語UIの初期化
    /// </summary>
    private void InitializeLanguageUI()
    {
        GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] InitializeLanguageUI called");

        if (LocalizationManager.Instance == null)
        {
            GameLogger.LogError(LogCategory.UI, "[LanguageSettingsUI] LocalizationManager.Instance is NULL! Make sure LocalizationManager exists in the scene.");
            return;
        }

        GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] LocalizationManager found");

        if (languageToggle != null)
        {
            GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] Setting up language toggle");

            // 現在の言語に応じてトグルを設定
            UpdateToggleFromLanguage();

            // イベントを登録
            languageToggle.onValueChanged.AddListener(OnToggleChanged);
            GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] Toggle listener added");
        }
        else
        {
            GameLogger.LogError(LogCategory.UI, "[LanguageSettingsUI] Cannot initialize: languageToggle is NULL");
        }

        isInitialized = true;
        GameLogger.Log(LogCategory.UI, "[LanguageSettingsUI] Initialization complete");
    }

    /// <summary>
    /// 現在の言語設定からトグルの状態を更新
    /// </summary>
    private void UpdateToggleFromLanguage()
    {
        if (languageToggle == null || LocalizationManager.Instance == null) return;

        var currentLang = LocalizationManager.Instance.GetCurrentLanguage();
        bool shouldBeOn = (currentLang == LocalizationManager.Language.English);

        GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] Updating toggle - Current Language: {currentLang}, Toggle should be: {shouldBeOn}");

        // イベントを一時的に解除してから更新（無限ループ防止）
        languageToggle.onValueChanged.RemoveListener(OnToggleChanged);
        languageToggle.isOn = shouldBeOn;
        languageToggle.onValueChanged.AddListener(OnToggleChanged);

        GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] Toggle updated to: {languageToggle.isOn}");
    }

    /// <summary>
    /// トグル変更時の処理
    /// false = 日本語, true = English
    /// </summary>
    private void OnToggleChanged(bool isEnglish)
    {
        GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] ===== Toggle Changed! Value: {isEnglish} =====");

        if (LocalizationManager.Instance == null)
        {
            GameLogger.LogError(LogCategory.UI, "[LanguageSettingsUI] LocalizationManager.Instance is NULL in OnToggleChanged!");
            return;
        }

        var newLang = isEnglish ? LocalizationManager.Language.English : LocalizationManager.Language.Japanese;
        GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] Attempting to change language to: {newLang}");

        LocalizationManager.Instance.SetLanguage(newLang);

        GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] Language changed successfully to: {LocalizationManager.Instance.GetCurrentLanguage()}");
    }

    /// <summary>
    /// コードから直接言語を設定（外部から呼ぶ用）
    /// </summary>
    public void SetLanguageToggle(bool isEnglish)
    {
        GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] SetLanguageToggle called with: {isEnglish}");
        if (languageToggle != null)
        {
            languageToggle.isOn = isEnglish;
        }
        else
        {
            GameLogger.LogError(LogCategory.UI, "[LanguageSettingsUI] Cannot set language toggle: languageToggle is NULL");
        }
    }

    /// <summary>
    /// 現在のトグル状態を取得（外部から呼ぶ用）
    /// </summary>
    public bool GetLanguageToggle()
    {
        bool result = languageToggle != null && languageToggle.isOn;
        GameLogger.Log(LogCategory.UI,$"[LanguageSettingsUI] GetLanguageToggle returning: {result}");
        return result;
    }
}