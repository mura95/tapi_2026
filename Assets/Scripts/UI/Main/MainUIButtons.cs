using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TapHouse.Logging;
using TapHouse.MultiDevice;

public class MainUIButtons : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField] private Button foodButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button snackButton;
    [SerializeField] private Button walkButton;
    [SerializeField] private Button callDogButton;

    [Header("Top Left UI (Hunger Gauge)")]
    [Tooltip("犬がいないときは非表示にするTopLeftUI")]
    [SerializeField] private GameObject topLeftUI;

    public UnityAction onFood;
    public UnityAction onPlay;
    public UnityAction onSnack;
    public UnityAction onCallDog;
    public UnityAction onWalk;

    [SerializeField] private Toggle tabletModeToggle;

    private bool _subscribedToDogLocationSync = false;
    private bool _walkButtonVisible = false;

    void Start()
    {
        // ボタンにリスナーを追加
        foodButton.onClick.AddListener(OnFood);
        playButton.onClick.AddListener(OnPlay);
        snackButton.onClick.AddListener(OnSnack);

        // walkButtonが未アサインの場合、子オブジェクトから自動検索
        if (walkButton == null)
        {
            Transform walkBtnTransform = transform.Find("WalkButton");
            if (walkBtnTransform != null)
            {
                walkButton = walkBtnTransform.GetComponent<Button>();
                GameLogger.Log(LogCategory.UI, "WalkButton auto-detected from children");
            }
        }

        // 散歩ボタンのリスナー
        if (walkButton != null)
        {
            walkButton.onClick.AddListener(OnWalk);
            walkButton.gameObject.SetActive(false);
        }

        // 呼ぶボタンのリスナー
        if (callDogButton != null)
        {
            callDogButton.onClick.AddListener(OnCallDog);
            callDogButton.gameObject.SetActive(false);
        }

        // Toggleのリスナーを追加して、状態を監視
        tabletModeToggle.onValueChanged.AddListener(OnTabletModeChanged);

        // DogLocationSyncのイベントを購読（遅延バインディング対応）
        TrySubscribeToDogLocationSync();

        // 初期状態の設定
        //UpdateButtonVisibility(tabletModeToggle.isOn);
    }

    void Update()
    {
        // DogLocationSyncが後から初期化された場合に対応
        if (!_subscribedToDogLocationSync)
        {
            TrySubscribeToDogLocationSync();
        }
    }

    private void TrySubscribeToDogLocationSync()
    {
        if (DogLocationSync.Instance != null && !_subscribedToDogLocationSync)
        {
            DogLocationSync.Instance.OnDogPresenceChanged += OnDogPresenceChanged;
            _subscribedToDogLocationSync = true;
            // 初期状態を設定
            OnDogPresenceChanged(DogLocationSync.Instance.HasDog);
            GameLogger.Log(LogCategory.UI, "Subscribed to DogLocationSync events");
        }
    }

    void OnDestroy()
    {
        // イベント購読解除
        if (DogLocationSync.Instance != null)
        {
            DogLocationSync.Instance.OnDogPresenceChanged -= OnDogPresenceChanged;
        }
    }

    void OnFood()
    {
        onFood?.Invoke();
    }

    void OnPlay()
    {
        onPlay?.Invoke();
    }

    void OnSnack()
    {
        onSnack?.Invoke();
    }

    void OnWalk()
    {
        GameLogger.Log(LogCategory.UI, "OnWalk button pressed");
        // タップしたら即座にボタンを非表示
        HideWalkButton();
        onWalk?.Invoke();
    }

    void OnCallDog()
    {
        GameLogger.Log(LogCategory.UI, "OnCallDog button pressed");

        // カスタムコールバックがあれば呼ぶ
        onCallDog?.Invoke();

        // DogLocationSyncで犬を呼ぶ（常に実行）
        if (DogLocationSync.Instance != null)
        {
            _ = DogLocationSync.Instance.RequestCallDog();
        }
        else
        {
            GameLogger.LogError(LogCategory.UI, "DogLocationSync.Instance is null - cannot call dog");
        }
    }

    /// <summary>
    /// 散歩ボタンを表示（ポップアップで「行かない」を選んだ後に呼ばれる）
    /// </summary>
    public void ShowWalkButton()
    {
        _walkButtonVisible = true;
        if (walkButton != null)
        {
            bool isTabletMode = tabletModeToggle != null && tabletModeToggle.isOn;
            walkButton.gameObject.SetActive(isTabletMode);
        }
    }

    /// <summary>
    /// 散歩ボタンを非表示
    /// </summary>
    public void HideWalkButton()
    {
        _walkButtonVisible = false;
        if (walkButton != null)
        {
            walkButton.gameObject.SetActive(false);
        }
    }

    // タブレットモードが変更されたときに呼ばれる
    void OnTabletModeChanged(bool isTabletMode)
    {
        UpdateButtonVisibility(isTabletMode);
    }

    /// <summary>
    /// 犬の存在状態が変わったときに呼ばれる
    /// </summary>
    private void OnDogPresenceChanged(bool hasDog)
    {
        GameLogger.Log(LogCategory.UI, $"OnDogPresenceChanged: hasDog={hasDog}");
        UpdateButtonsForDogPresence(hasDog);
    }

    /// <summary>
    /// 犬の有無に応じてボタン表示を切り替え
    /// </summary>
    private void UpdateButtonsForDogPresence(bool hasDog)
    {
        bool isTabletMode = tabletModeToggle != null && tabletModeToggle.isOn;

        if (hasDog)
        {
            // 犬がいる: ご飯・遊ぶ・おやつを表示、呼ぶを非表示
            foodButton.gameObject.SetActive(isTabletMode);
            playButton.gameObject.SetActive(isTabletMode);
            snackButton.gameObject.SetActive(isTabletMode);
            if (walkButton != null)
            {
                walkButton.gameObject.SetActive(isTabletMode && _walkButtonVisible);
            }
            if (callDogButton != null)
            {
                callDogButton.gameObject.SetActive(false);
            }
            // TopLeftUI（空腹ゲージ）を表示
            if (topLeftUI != null)
            {
                topLeftUI.SetActive(true);
            }
        }
        else
        {
            // 犬がいない: 呼ぶを表示、ご飯・遊ぶ・おやつを非表示
            foodButton.gameObject.SetActive(false);
            playButton.gameObject.SetActive(false);
            snackButton.gameObject.SetActive(false);
            if (walkButton != null)
            {
                walkButton.gameObject.SetActive(false);
            }
            if (callDogButton != null)
            {
                callDogButton.gameObject.SetActive(isTabletMode);
            }
            // TopLeftUI（空腹ゲージ）を非表示
            if (topLeftUI != null)
            {
                topLeftUI.SetActive(false);
            }
        }
    }

    // タブレットモードの状態に応じてボタンを非表示または非アクティブにする
    public void UpdateButtonVisibility(bool isTabletMode)
    {
        GameLogger.Log(LogCategory.UI, "UpdateButtonVisibility: " + isTabletMode);

        // isTabletMode=falseなら強制的に全ボタン非表示（アクション中など）
        if (!isTabletMode)
        {
            foodButton.gameObject.SetActive(false);
            playButton.gameObject.SetActive(false);
            snackButton.gameObject.SetActive(false);
            if (walkButton != null)
            {
                walkButton.gameObject.SetActive(false);
            }
            if (callDogButton != null)
            {
                callDogButton.gameObject.SetActive(false);
            }
            return;
        }

        // isTabletMode=trueの場合のみ、犬の有無で判断
        if (DogLocationSync.Instance != null)
        {
            UpdateButtonsForDogPresence(DogLocationSync.Instance.HasDog);
        }
        else
        {
            // フォールバック: 従来の動作
            foodButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(true);
            snackButton.gameObject.SetActive(true);
            if (walkButton != null)
            {
                walkButton.gameObject.SetActive(_walkButtonVisible);
            }
            if (callDogButton != null)
            {
                callDogButton.gameObject.SetActive(false);
            }
        }
    }
}
