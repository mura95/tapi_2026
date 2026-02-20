using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HungerStatusChangedEventListener))]
public class MainUIFullness : MonoBehaviour
{
    [SerializeField] private HungerGaugeListSO hungerGaugeList;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image statusImage;
    [SerializeField] private List<Image> boneImages;

    private HungerStatusChangedEventListener _hungerStatusChangedEventListener;
    private int currentHungerIndex = 0;

    void Awake()
    {
        _hungerStatusChangedEventListener = GetComponent<HungerStatusChangedEventListener>();
    }

    void Start()
    {
        _hungerStatusChangedEventListener.response = OnHungerStatusChanged;
        var hungerStateIndex = GlobalVariables.CurrentHungerStateIndex;
        ApplyStatus(hungerStateIndex);

        // 言語変更イベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    void OnDestroy()
    {
        // 言語変更イベントから登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 言語が変更されたときに現在の状態を再適用
    /// </summary>
    void OnLanguageChanged()
    {
        ApplyStatus(currentHungerIndex);
    }

    void ApplyStatus(int groupIndex)
    {
        currentHungerIndex = groupIndex; // 現在のインデックスを保存
        var hungerGauge = hungerGaugeList.GetItem(groupIndex);

        // 多言語対応：翻訳されたテキストを取得
        statusText.text = hungerGauge.GetLocalizedDescription();
        statusImage.sprite = hungerGauge.icon;

        for (var i = 0; i < boneImages.Count; i++)
        {
            if (i < hungerGauge.activeBoneCount)
            {
                boneImages[i].sprite = hungerGaugeList.boneOnImage;
            }
            else
            {
                boneImages[i].sprite = hungerGaugeList.boneOffImage;
            }
        }
    }

    void OnHungerStatusChanged(int status)
    {
        ApplyStatus(status);
    }
}