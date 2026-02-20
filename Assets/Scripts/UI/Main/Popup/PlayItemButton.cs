using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Button))]
public class PlayItemButton : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI itemName;

    private Button _button;
    private PlayItem _playItem;

    public UnityAction<PlayItem> onClick;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    void Start()
    {
        _button.onClick.AddListener(OnClick);

        // 言語変更イベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateItemName;
        }
    }

    void OnDestroy()
    {
        // 言語変更イベントから登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateItemName;
        }
    }

    public void Initialize(PlayItem playItem)
    {
        _playItem = playItem;
        image.sprite = playItem.icon;
        UpdateItemName();
    }

    /// <summary>
    /// アイテム名を現在の言語で更新
    /// </summary>
    void UpdateItemName()
    {
        if (_playItem != null && itemName != null)
        {
            itemName.text = _playItem.GetLocalizedName();
        }
    }

    void OnClick()
    {
        onClick?.Invoke(_playItem);
    }
}