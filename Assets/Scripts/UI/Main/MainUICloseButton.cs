using System;
using BehaviorDesigner.Runtime.Tasks.Unity.Timeline;
using UnityEngine;
using UnityEngine.UI;
using TapHouse.Logging;
public class MainUICloseButton : MonoBehaviour
{
    [SerializeField] private GameObject closeButton;
    [SerializeField] private MainUIButtons _mainUIButtons;
    [SerializeField] private DogController _dogController;
    [SerializeField] private SnackManager _snackManager;
    [SerializeField] private PlayManager _playManager;
    [SerializeField] private GetBall _getBall;
    [SerializeField] private GetToy _getToy;
    public static event Action OnCloseButtonClicked;
    // 初期状態
    void Start()
    {
        closeButton.GetComponent<Button>().onClick.AddListener(OnCloseButtonPressed);
    }

    // UI を表示する
    public void ShowUI()
    {
        SetActiveState(true);
        _mainUIButtons.UpdateButtonVisibility(false);
    }

    // クローズボタンが押された瞬間にトリガー
    public async void OnCloseButtonPressed()
    {
        SetActiveState(false);
        _dogController.UpdateTransitionState(0);
        if (GlobalVariables.CurrentState == PetState.snack)
        {
            _snackManager.TriggerSnackCompletion();
            GameLogger.Log(LogCategory.UI, "Snack completion triggered.");
        }
        else if (GlobalVariables.CurrentState == PetState.ball)
        {
            _getBall.EndPlay();
        }
        else if (GlobalVariables.CurrentState == PetState.toy)
        {
            _getToy.EndPlay();
        }
        else if (GlobalVariables.CurrentState == PetState.ready)
        {
            _playManager.CancelToyAction();
        }
        GlobalVariables.CurrentState = PetState.idle;
        OnCloseButtonClicked?.Invoke();
    }

    // UI のアクティブ状態を設定するヘルパー関数
    public void SetActiveState(bool isActive)
    {
        closeButton.SetActive(isActive);
        _mainUIButtons.UpdateButtonVisibility(!isActive);
    }
}
