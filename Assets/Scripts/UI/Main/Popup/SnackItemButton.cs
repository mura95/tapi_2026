using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Button))]
public class SnackItemButton : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI textName;

    private Button _button;
    private SnackItem _snackItem;

    public UnityAction<SnackItem> onClick;

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

    public void Initialize(SnackItem snackItem)
    {
        _snackItem = snackItem;
        image.sprite = snackItem.icon;
        UpdateItemName();
    }

    /// <summary>
    /// アイテム名を現在の言語で更新
    /// </summary>
    void UpdateItemName()
    {
        if (_snackItem != null && textName != null)
        {
            textName.text = _snackItem.GetLocalizedName();
        }
    }

    void OnClick()
    {
        onClick?.Invoke(_snackItem);
    }
}