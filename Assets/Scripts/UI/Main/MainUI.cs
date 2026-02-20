using UnityEngine;
using UnityEngine.UI;
using TapHouse.Logging;
using TapHouse.MetaverseWalk.Core;

public class MainUI : MonoBehaviour
{
    private MainUIButtons _mainUIButtons;
    private MainUIFullness _mainUIFullness;
    [SerializeField] private EatAnimationController _Eating;
    [SerializeField] private TurnAndMoveHandler _turnAndMoveHandler;
    [SerializeField] private DogController _dogController;
    [SerializeField] private FirebaseManager _firebaseManager;

    [Header("Position Check Settings")]
    [SerializeField] private float positionTolerance = 0.5f;
    private Vector3 targetPosition = Vector3.zero;

    private string _petName = "わんちゃん";

    void Start()
    {
        _mainUIButtons = GetComponentInChildren<MainUIButtons>();
        _mainUIFullness = GetComponentInChildren<MainUIFullness>();
        _Eating = FindObjectOfType<EatAnimationController>();
        _turnAndMoveHandler = FindObjectOfType<TurnAndMoveHandler>();
        _dogController = FindObjectOfType<DogController>();
        if (_firebaseManager == null)
            _firebaseManager = FindObjectOfType<FirebaseManager>();

        _mainUIButtons.onFood = OnFood;
        _mainUIButtons.onPlay = OnPlay;
        _mainUIButtons.onSnack = OnSnack;
        _mainUIButtons.onWalk = OnWalk;

        InitPetName();
    }

    private async void InitPetName()
    {
        // まずPlayerPrefsから取得
        string cached = PlayerPrefs.GetString(PrefsKeys.PetName, "");
        if (!string.IsNullOrEmpty(cached))
        {
            _petName = cached;
        }

        // Firebaseから最新を取得してキャッシュ
        if (_firebaseManager != null)
        {
            try
            {
                string name = await _firebaseManager.GetPetNameAsync();
                _petName = name;
                PlayerPrefs.SetString(PrefsKeys.PetName, name);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                GameLogger.LogWarning(LogCategory.UI, $"Failed to get pet name: {e.Message}");
            }
        }
    }

    void OnFood()
    {
        if (IsDogAtTargetPosition())
        {
            _Eating.StartEatingAnimation();
            //OpenPopup("UI/FoodSelectionUI");
        }
        else
        {
            ShowReturnAlert();
        }
    }

    void OnPlay()
    {
        _dogController.UpdateTransitionState(0);
        _turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 2.0f);
        if (IsDogAtTargetPosition())
        {
            OpenPopup("UI/PlaySelectionUI");
            GlobalVariables.CurrentState = PetState.ready;
        }
        else
        {
            ShowReturnAlert();
        }
    }

    void OnWalk()
    {
        WalkScheduler.Instance?.StartWalk();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToMetaverse();
        }
        else
        {
            GameLogger.LogError(LogCategory.UI, "[MainUI] SceneTransitionManager not found");
        }
    }

    void OnSnack()
    {
        _dogController.UpdateTransitionState(0);
        _turnAndMoveHandler.StartTurnAndMove(new Vector3(0, 0, 0), 2.0f);
        if (IsDogAtTargetPosition())
        {
            OpenPopup("UI/SnackSelectionUI");
            GlobalVariables.CurrentState = PetState.ready;
        }
        else
        {
            ShowReturnAlert();
        }
    }

    /// <summary>
    /// 犬が目標位置(0,0,0)にいるかを確認
    /// </summary>
    private bool IsDogAtTargetPosition()
    {
        if (_dogController == null)
        {
            GameLogger.LogWarning(LogCategory.UI, "DogController is null.");
            return false;
        }

        Vector3 dogPosition = _dogController.transform.position;
        float distance = Vector3.Distance(dogPosition, targetPosition);

        GameLogger.Log(LogCategory.UI, $"Dog position: {dogPosition}, Distance from target: {distance:F2}");

        return distance <= positionTolerance;
    }

    /// <summary>
    /// 犬が戻ってくるまで待つよう促すアラートを表示
    /// </summary>
    private void ShowReturnAlert()
    {
        ShowLightAlert($"{_petName}が帰ってきたら\nボタンを押せます！");
    }

    private void ShowLightAlert(string message)
    {
        try
        {
            var alertPrefab = Resources.Load<GameObject>("UI/AlertUI");

            if (alertPrefab != null)
            {
                // 親を指定せずに生成
                var alertInstance = Instantiate(alertPrefab);

                // Canvas設定を強制的に修正
                Canvas alertCanvas = alertInstance.GetComponent<Canvas>();
                if (alertCanvas != null)
                {
                    alertCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    alertCanvas.sortingOrder = 1000;
                    alertCanvas.worldCamera = null;
                }
                else
                {
                    // Canvasがない場合は追加
                    alertCanvas = alertInstance.AddComponent<Canvas>();
                    alertCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    alertCanvas.sortingOrder = 1000;
                }

                GameLogger.Log(LogCategory.UI, $"Alert created - Parent: {(alertInstance.transform.parent != null ? alertInstance.transform.parent.name : "None")}");
                GameLogger.Log(LogCategory.UI, $"Canvas Sort Order: {alertCanvas.sortingOrder}");

                // メッセージ設定
                var controller = alertInstance.GetComponent<LightAlertController>();
                if (controller != null)
                {
                    controller.SetMessage(message);
                }
            }
        }
        catch (System.Exception e)
        {
            GameLogger.LogError(LogCategory.UI, $"Failed to show alert: {e.Message}");
        }
    }

    void OpenPopup(string prefabName)
    {
        var prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null)
        {
            GameLogger.LogError(LogCategory.UI, $"Not Found Prefab '{prefabName}");
            return;
        }
        Instantiate(prefab, transform);
    }
}