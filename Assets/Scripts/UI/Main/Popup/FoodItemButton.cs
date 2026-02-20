using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Button))]
public class FoodItemButton : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI itemName;

    private Button _button;
    private FoodItem _foodItem;

    public UnityAction<FoodItem> onClick;

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

    public void Initialize(FoodItem foodItem)
    {
        _foodItem = foodItem;
        image.sprite = foodItem.icon;
        UpdateItemName();
    }
    
    /// <summary>
    /// アイテム名を現在の言語で更新
    /// </summary>
    void UpdateItemName()
    {
        if (_foodItem != null && itemName != null)
        {
            itemName.text = _foodItem.GetLocalizedName();
        }
    }

    void OnClick()
    {
        onClick?.Invoke(_foodItem);
    }
}