using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HungerGauge
{
    [Header("Localization Key (翻訳キー)")]
    [Tooltip("LocalizationManagerで定義されたキーを入力")]
    public string nameKey;           // 例: "hunger_full"
    public string descriptionKey;    // 例: "hunger_full_desc"

    [Header("Visual")]
    public Sprite icon;
    public int activeBoneCount;

    /// <summary>
    /// 現在の言語で翻訳された名前を取得
    /// </summary>
    public string GetLocalizedName()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(nameKey))
        {
            return LocalizationManager.Instance.GetText(nameKey);
        }
        return nameKey; // フォールバック
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
        return descriptionKey; // フォールバック
    }
}

[CreateAssetMenu(fileName = "HungerGaugeListSO", menuName = "PetData/HungerGaugeList")]
public class HungerGaugeListSO : ScriptableObject
{
    public List<HungerGauge> gauges;
    public Sprite boneOnImage;
    public Sprite boneOffImage;

    public HungerGauge GetItem(int index)
    {
        return gauges[index];
    }
}