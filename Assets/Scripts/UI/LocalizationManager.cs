using UnityEngine;
using System.Collections.Generic;
using TapHouse.Logging;

/// <summary>
/// 多言語対応マネージャー
/// 英語と日本語の切り替えを管理します
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public enum Language
    {
        Japanese,
        English
    }

    [SerializeField] private Language currentLanguage = Language.Japanese;
    private const string LANGUAGE_PREF_KEY = "SelectedLanguage";

    // 言語変更時のイベント
    public System.Action OnLanguageChanged;

    // 翻訳データ
    private Dictionary<string, Dictionary<Language, string>> translations = new Dictionary<string, Dictionary<Language, string>>();

    private void Awake()
    {
        // シングルトンパターン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTranslations();
            LoadLanguagePreference();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 翻訳データの初期化
    /// </summary>
    private void InitializeTranslations()
    {
        // UIボタン関連
        AddTranslation("play_button", "遊び", "Play");
        AddTranslation("feed_button", "ご飯", "Feed");
        AddTranslation("snack_button", "おかし", "Snack");
        AddTranslation("call_button", "呼ぶ", "Call");
        AddTranslation("walk_button", "さんぽ", "Walk");

        AddTranslation("close_button", "とじる", "Close");

        AddTranslation("hunger_full", "満腹", "Full");
        AddTranslation("hunger_full_desc", "満腹", "Full");
        AddTranslation("hunger_medium_high", "ちょうど良い", "Just Right");
        AddTranslation("hunger_medium_high_desc", "ちょうど良い", "Just Right");
        AddTranslation("hunger_medium_low", "少し減っている", "A Little Hungry");
        AddTranslation("hunger_medium_low_desc", "少し減っている", "A Little Hungry");
        AddTranslation("hunger_hungry", "空腹", "Hungry");
        AddTranslation("hunger_hungry_desc", "空腹", "Hungry");

        AddTranslation("Alert_Message", "わんちゃんが帰ってきたら ボタンを押せます！", "When your dog comes home, you can press the button!");

        // アイテム名 - 遊び
        AddTranslation("item_ball", "ボール", "Ball");
        AddTranslation("item_rope", "ロープ", "Rope");

        // アイテム名 - おやつ
        AddTranslation("snack_bone", "ほね", "Bone");
        AddTranslation("snack_stick", "ペロペロスティック", "Lollipop Stick");

        AddTranslation("play_title", "何して遊ぶ？", "What do you want to play?");
        AddTranslation("snack_title", "どんなおかしをあげる？", "What snack do you want to give?");

        AddTranslation("stop_txt", "やめる", "Stop");
    }

    /// <summary>
    /// 翻訳を追加
    /// </summary>
    private void AddTranslation(string key, string japanese, string english)
    {
        translations[key] = new Dictionary<Language, string>
        {
            { Language.Japanese, japanese },
            { Language.English, english }
        };
    }

    /// <summary>
    /// 翻訳テキストを取得
    /// </summary>
    public string GetText(string key)
    {
        if (translations.ContainsKey(key) && translations[key].ContainsKey(currentLanguage))
        {
            return translations[key][currentLanguage];
        }

        GameLogger.LogWarning(LogCategory.UI, $"Translation not found for key: {key}");
        return key;
    }

    /// <summary>
    /// 現在の言語を取得
    /// </summary>
    public Language GetCurrentLanguage()
    {
        return currentLanguage;
    }

    /// <summary>
    /// 言語を設定
    /// </summary>
    public void SetLanguage(Language language)
    {
        GameLogger.Log(LogCategory.UI, $"[LocalizationManager] SetLanguage called: {currentLanguage} -> {language}");

        if (currentLanguage != language)
        {
            currentLanguage = language;
            SaveLanguagePreference();

            GameLogger.Log(LogCategory.UI, $"[LocalizationManager] Language changed! Invoking OnLanguageChanged event...");
            OnLanguageChanged?.Invoke();

            // イベントリスナーの数を確認
            int listenerCount = OnLanguageChanged?.GetInvocationList().Length ?? 0;
            GameLogger.Log(LogCategory.UI, $"[LocalizationManager] OnLanguageChanged has {listenerCount} listeners");
        }
        else
        {
            GameLogger.Log(LogCategory.UI, $"[LocalizationManager] Language is already {language}, no change needed");
        }
    }

    /// <summary>
    /// 言語を切り替え（トグル）
    /// </summary>
    public void ToggleLanguage()
    {
        Language newLanguage = currentLanguage == Language.Japanese ? Language.English : Language.Japanese;
        SetLanguage(newLanguage);
    }

    /// <summary>
    /// 言語設定を保存
    /// </summary>
    private void SaveLanguagePreference()
    {
        PlayerPrefs.SetInt(LANGUAGE_PREF_KEY, (int)currentLanguage);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 言語設定を読み込み
    /// </summary>
    private void LoadLanguagePreference()
    {
        if (PlayerPrefs.HasKey(LANGUAGE_PREF_KEY))
        {
            currentLanguage = (Language)PlayerPrefs.GetInt(LANGUAGE_PREF_KEY);
        }
        else
        {
            // デフォルトはシステム言語に基づいて設定
            if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                currentLanguage = Language.Japanese;
            }
            else
            {
                currentLanguage = Language.English;
            }
        }
    }
}