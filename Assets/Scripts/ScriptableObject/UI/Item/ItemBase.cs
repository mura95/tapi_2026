using UnityEngine;

[System.Serializable]
public abstract class ItemBase
{
    public int id;

    [Header("Localization Keys (翻訳キー)")]
    [Tooltip("LocalizationManagerで定義されたアイテム名のキー")]
    public string itemNameKey;           // 例: "item_ball", "food_chicken"

    [Tooltip("LocalizationManagerで定義された説明のキー")]
    public string descriptionKey;        // 例: "item_ball_desc"

    [Header("Visual")]
    public Sprite icon;

    /// <summary>
    /// 現在の言語で翻訳されたアイテム名を取得
    /// </summary>
    public string GetLocalizedName()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(itemNameKey))
        {
            return LocalizationManager.Instance.GetText(itemNameKey);
        }
        return itemNameKey;
    }

    /// <summary>
    /// 現在の言語で翻訳された説明を取得
    /// </summary>
    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(descriptionKey))
        {
            return LocalizationManager.Instance.GetText(descriptionKey);
        }
        return descriptionKey;
    }
}