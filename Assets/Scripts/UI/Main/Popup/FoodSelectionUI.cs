using UnityEngine;
using UnityEngine.UI;
using TapHouse.Logging;

public class FoodSelectionUI : MonoBehaviour
{
    [Header("MasterData")]
    [SerializeField] private FoodItemListSO foodItemListSO;

    [Header("Scroll")]
    [SerializeField] private Transform content;
    [SerializeField] private FoodItemButton foodItemButtonPrefab;

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backGroundButton;

    [Header("Event")]
    [SerializeField] private FoodItemButtonEvent foodItemButtonEvent;

    void Start()
    {
        closeButton.onClick.AddListener(OnClose);
        backGroundButton.onClick.AddListener(OnClose);

        GenerateButtons();
    }

    void GenerateButtons()
    {
        foreach (var foodItem in foodItemListSO.items)
        {
            var newButton = Instantiate(foodItemButtonPrefab, content);
            newButton.Initialize(foodItem);
            newButton.onClick = OnClickItem;
        }
    }

    void OnClickItem(FoodItem foodItem)
    {
        GameLogger.Log(LogCategory.UI, $"FoodItem OnClick {foodItem.id}");

        foodItemButtonEvent.Raise(foodItem.id);

        Close();
    }

    void OnClose()
    {
        Close();
        GlobalVariables.CurrentState = PetState.idle;
    }

    void Close()
    {
        Destroy(gameObject);
    }
}
