using UnityEngine;
using UnityEngine.UI;
using TapHouse.Logging;

public class PlaySelectionUI : MonoBehaviour
{
    [Header("MasterData")]
    [SerializeField] private PlayItemListSO playItemListSO;

    [Header("Scroll")]
    [SerializeField] private Transform content;
    [SerializeField] private PlayItemButton playItemButtonPrefab;

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backGroundButton;

    [Header("Event")]
    [SerializeField] private PlayItemButtonEvent playItemButtonEvent;

    void Start()
    {
        closeButton.onClick.AddListener(OnClose);
        backGroundButton.onClick.AddListener(OnClose);

        GenerateButtons();
    }

    void GenerateButtons()
    {
        foreach (var playItem in playItemListSO.items)
        {
            var newButton = Instantiate(playItemButtonPrefab, content);
            newButton.Initialize(playItem);
            newButton.onClick = OnClickItem;
        }
    }

    void OnClickItem(PlayItem playItem)
    {
        GameLogger.Log(LogCategory.UI, $"PlayItem OnClick {playItem.id}");
        playItemButtonEvent.Raise(playItem.id);
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
