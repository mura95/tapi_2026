using UnityEngine;
using UnityEngine.UI;

public class SnackSelectionUI : MonoBehaviour
{
    [Header("MasterData")]
    [SerializeField] private SnackItemListSO snackItemListSO;

    [Header("Scroll")]
    [SerializeField] private Transform content;
    [SerializeField] private SnackItemButton snackItemButtonPrefab;

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backGroundButton;

    [Header("Event")]
    [SerializeField] private SnackItemButtonEvent snackItemButtonEvent;

    void Start()
    {
        closeButton.onClick.AddListener(OnClose);
        backGroundButton.onClick.AddListener(OnClose);

        GenerateButtons();
    }

    void GenerateButtons()
    {
        foreach (var foodItem in snackItemListSO.items)
        {
            var newButton = Instantiate(snackItemButtonPrefab, content);
            newButton.Initialize(foodItem);
            newButton.onClick = OnClickItem;
        }
    }

    void OnClickItem(SnackItem snackItem)
    {
        snackItemButtonEvent.Raise(snackItem.id);
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
