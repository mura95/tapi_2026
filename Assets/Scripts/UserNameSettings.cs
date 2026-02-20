using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System;
using Firebase.Auth;
using TapHouse.Logging;
using TapHouse.MultiDevice;

public class FirebaseUIManager : MonoBehaviour
{
    [SerializeField] private string defaultUserName = "adminGuest";
    [SerializeField] private SleepController _sleepController;
    [SerializeField] private FirebaseManager _firebaseManager;
    [SerializeField] private DogController _dogController;
    [SerializeField] private Toggle resetToggle;

    [SerializeField] private TimeZoneService _timeZoneService;

    public GameObject inputPanel;
    public TMP_Text userNameText;

    public TextMeshProUGUI sliderSleepText;
    public TextMeshProUGUI sliderWakeText;
    public TextMeshProUGUI sliderNapSleepText;
    public TextMeshProUGUI sliderNapWakeText;

    public Slider sliderSleep;
    public Slider sliderWake;
    public Slider sliderNapSleep;
    public Slider sliderNapWake;

    public TMP_Dropdown timezoneDropdown;
    public Toggle tabletModeToggle;
    public Toggle subDeviceToggle;  // サブ機設定トグル
    public Button saveButton;
    public Button logoutButton;

    private FirebaseAuth firebaseAuth;

    // 指の本数（例: 5本タップ）
    [SerializeField] int requiredFingers = 5;
    // その5本タップを何回繰り返すか（例: 3回）
    [SerializeField] int requiredTapCount = 5;
    // 連続判定の時間窓（秒）
    [SerializeField] float tapWindow = 1.2f;

    int tapCount = 0;
    float lastTapTime = -999f;

    // ---- ここから：DogMaterial 設定UI（DogMaterialSwitcher を操作するだけ）----
    [Header("対象（DogMaterialSwitcher を割り当て）")]
    [SerializeField] private DogMaterialSwitcher switcher;

    [Header("ラジオボタン風トグル")]
    [SerializeField] private ToggleGroup group;
    [SerializeField] private Toggle toggleBrown;
    [SerializeField] private Toggle toggleBlack;
    [SerializeField] private Toggle toggleWhite;
    // ---- ここまで ----

    // ★ タイムゾーンリストを保持（setDropdownValue で受け取る）
    private List<TimeZoneInfo> _timeZones;

    private void Awake()
    {
        // --- DogColor UI 初期化 ---
        if (group) group.allowSwitchOff = false;
        if (toggleBrown) toggleBrown.group = group;
        if (toggleBlack) toggleBlack.group = group;
        if (toggleWhite) toggleWhite.group = group;

        if (toggleBrown) toggleBrown.onValueChanged.AddListener(on =>
        {
            if (on && switcher) switcher.SetCoat(DogCoat.Brown, applyNow: true);
        });
        if (toggleBlack) toggleBlack.onValueChanged.AddListener(on =>
        {
            if (on && switcher) switcher.SetCoat(DogCoat.Black, applyNow: true);
        });
        if (toggleWhite) toggleWhite.onValueChanged.AddListener(on =>
        {
            if (on && switcher) switcher.SetCoat(DogCoat.White, applyNow: true);
        });

        // --- スライダーはイベントで文言更新（毎フレーム更新を回避）---
        if (sliderSleep) sliderSleep.onValueChanged.AddListener(v => { if (sliderSleepText) sliderSleepText.text = ((int)v).ToString(); });
        if (sliderWake) sliderWake.onValueChanged.AddListener(v => { if (sliderWakeText) sliderWakeText.text = ((int)v).ToString(); });
        if (sliderNapSleep) sliderNapSleep.onValueChanged.AddListener(v => { if (sliderNapSleepText) sliderNapSleepText.text = ((int)v).ToString(); });
        if (sliderNapWake) sliderNapWake.onValueChanged.AddListener(v => { if (sliderNapWakeText) sliderNapWakeText.text = ((int)v).ToString(); });
    }

    private void OnEnable()
    {
        SyncDogColorUI();
    }

    private void Start()
    {
        LoadSettings();

        if (inputPanel) inputPanel.SetActive(false);

        firebaseAuth = FirebaseAuth.DefaultInstance;

        if (saveButton) saveButton.onClick.AddListener(SaveSettings);
        if (logoutButton) logoutButton.onClick.AddListener(Logout);
        if (tabletModeToggle) tabletModeToggle.onValueChanged.AddListener(SetTabletMode);

        // DogMaterialSwitcherのStart()が終わっているであろうタイミングで再同期しておく
        SyncDogColorUI();
    }

    private void SyncDogColorUI()
    {
        if (!switcher) return;

        switch (switcher.Current)
        {
            case DogCoat.Brown: if (toggleBrown) toggleBrown.isOn = true; break;
            case DogCoat.Black: if (toggleBlack) toggleBlack.isOn = true; break;
            case DogCoat.White: if (toggleWhite) toggleWhite.isOn = true; break;
        }

        // どれもONでなければBrownへフォールバック
        bool anyOn = (toggleBrown && toggleBrown.isOn) ||
                     (toggleBlack && toggleBlack.isOn) ||
                     (toggleWhite && toggleWhite.isOn);
        if (!anyOn && toggleBrown) toggleBrown.isOn = true;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0))
        {
            GameLogger.Log(LogCategory.General,"Left Shift + Left Mouse Button Clicked");
            ShowInputPanel();
        }
#endif

        // すでに開いていれば判定しない
        if (inputPanel != null && inputPanel.activeSelf) return;

        // このフレームで新規に「Began」になった指の本数を数える
        int beganThisFrame = 0;
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began) beganThisFrame++;
        }

        // 5本以上の同時タップ（Began）が発生したら1カウント
        if (beganThisFrame >= requiredFingers)
        {
            RegisterMultiFingerTap();
        }
    }

    void RegisterMultiFingerTap()
    {
        // 時間窓外ならリセット
        if (Time.unscaledTime - lastTapTime > tapWindow)
            tapCount = 0;

        tapCount++;
        lastTapTime = Time.unscaledTime;

        if (tapCount >= requiredTapCount)
        {
            tapCount = 0;
            ShowInputPanel();
        }
    }

    private async void LoadSettings()
    {
        // DisplayName
        if (PlayerPrefs.HasKey(PrefsKeys.DisplayName))
        {
            string savedDisplayName = PlayerPrefs.GetString(PrefsKeys.DisplayName, defaultUserName);
            if (userNameText) userNameText.text = savedDisplayName;
        }
        else
        {
            string fetchedDisplayName = await _firebaseManager.GetDisplayNameAsync();
            if (!string.IsNullOrEmpty(fetchedDisplayName))
            {
                PlayerPrefs.SetString(PrefsKeys.DisplayName, fetchedDisplayName);
                PlayerPrefs.Save();
                if (userNameText) userNameText.text = fetchedDisplayName;
                GameLogger.Log(LogCategory.General,$"Fetched and saved display name: {fetchedDisplayName}");
            }
            else
            {
                GameLogger.LogWarning(LogCategory.General,"Failed to fetch display name or display name is empty.");
            }
        }

        // 時刻系
        if (PlayerPrefs.HasKey(PrefsKeys.SleepHour))
        {
            int v = PlayerPrefs.GetInt(PrefsKeys.SleepHour);
            if (sliderSleep) sliderSleep.value = v;
            if (sliderSleepText) sliderSleepText.text = v.ToString();
        }
        if (PlayerPrefs.HasKey(PrefsKeys.WakeHour))
        {
            int v = PlayerPrefs.GetInt(PrefsKeys.WakeHour);
            if (sliderWake) sliderWake.value = v;
            if (sliderWakeText) sliderWakeText.text = v.ToString();
        }
        if (PlayerPrefs.HasKey(PrefsKeys.NapSleepHour))
        {
            int v = PlayerPrefs.GetInt(PrefsKeys.NapSleepHour);
            if (sliderNapSleep) sliderNapSleep.value = v;
            if (sliderNapSleepText) sliderNapSleepText.text = v.ToString();
        }
        if (PlayerPrefs.HasKey(PrefsKeys.NapWakeHour))
        {
            int v = PlayerPrefs.GetInt(PrefsKeys.NapWakeHour);
            if (sliderNapWake) sliderNapWake.value = v;
            if (sliderNapWakeText) sliderNapWakeText.text = v.ToString();
        }

        // TabletMode
        if (PlayerPrefs.HasKey(PrefsKeys.TabletMode) && tabletModeToggle)
        {
            bool isTabletMode = PlayerPrefs.GetInt(PrefsKeys.TabletMode) == 1;
            tabletModeToggle.isOn = isTabletMode;
        }

        // SubDevice（サブ機設定）
        if (subDeviceToggle != null)
        {
            bool isSubDevice = PlayerPrefs.GetInt(PrefsKeys.IsSubDevice, 0) == 1;
            subDeviceToggle.isOn = isSubDevice;
        }

        // Timezone（options が別途 setDropdownValue で設定される想定でも、value だけは合わせておく）
        if (PlayerPrefs.HasKey(PrefsKeys.TimezoneIndex))
        {
            int tzIndex = PlayerPrefs.GetInt(PrefsKeys.TimezoneIndex, 0);
            if (timezoneDropdown)
            {
                timezoneDropdown.value = Mathf.Clamp(tzIndex, 0, Mathf.Max(0, timezoneDropdown.options.Count - 1));
                timezoneDropdown.RefreshShownValue();
            }
        }

        if (resetToggle) resetToggle.isOn = false;
    }

    // Saveボタン
    public void SaveSettings()
    {
        GameLogger.Log(LogCategory.General,"[FirebaseUIManager] SaveSettings called");

        // ---- 変更前の値を取得 ----
        int previousSleepHour = PlayerPrefs.GetInt(PrefsKeys.SleepHour, 22);
        int previousWakeHour = PlayerPrefs.GetInt(PrefsKeys.WakeHour, 6);
        int previousTimeZoneIndex = PlayerPrefs.GetInt(PrefsKeys.TimezoneIndex, 0);

        // ---- 新しい値を保存 ----
        int newSleepHour = sliderSleep ? (int)sliderSleep.value : 22;
        int newWakeHour = sliderWake ? (int)sliderWake.value : 6;
        int newTimeZoneIndex = timezoneDropdown ? timezoneDropdown.value : 0;

        PlayerPrefs.SetInt(PrefsKeys.SleepHour, newSleepHour);
        PlayerPrefs.SetInt(PrefsKeys.WakeHour, newWakeHour);

        if (sliderNapSleep) PlayerPrefs.SetInt(PrefsKeys.NapSleepHour, (int)sliderNapSleep.value);
        if (sliderNapWake) PlayerPrefs.SetInt(PrefsKeys.NapWakeHour, (int)sliderNapWake.value);

        PlayerPrefs.SetInt(PrefsKeys.TimezoneIndex, newTimeZoneIndex);

        if (tabletModeToggle) PlayerPrefs.SetInt(PrefsKeys.TabletMode, tabletModeToggle.isOn ? 1 : 0);

        // サブ機設定を保存
        if (subDeviceToggle != null)
        {
            bool isSubDevice = subDeviceToggle.isOn;
            PlayerPrefs.SetInt(PrefsKeys.IsSubDevice, isSubDevice ? 1 : 0);

            // DogLocationSyncにも反映
            if (DogLocationSync.Instance != null)
            {
                DogLocationSync.Instance.SetDeviceRole(isSubDevice ? DeviceRole.Sub : DeviceRole.Main);
            }
        }

        PlayerPrefs.Save();

        GameLogger.Log(LogCategory.General,$"[SaveSettings] 保存完了: Sleep={newSleepHour}時, Wake={newWakeHour}時, TZ_Index={newTimeZoneIndex}");

        // ---- 変更検知 ----
        bool sleepHourChanged = (previousSleepHour != newSleepHour);
        bool wakeHourChanged = (previousWakeHour != newWakeHour);
        bool timeZoneChanged = (previousTimeZoneIndex != newTimeZoneIndex);

        if (sleepHourChanged)
        {
            GameLogger.Log(LogCategory.General,$"[SaveSettings] 就寝時刻が変更されました: {previousSleepHour}時 → {newSleepHour}時");
        }
        if (wakeHourChanged)
        {
            GameLogger.Log(LogCategory.General,$"[SaveSettings] 起床時刻が変更されました: {previousWakeHour}時 → {newWakeHour}時");
        }
        if (timeZoneChanged)
        {
            GameLogger.Log(LogCategory.General,$"[SaveSettings] タイムゾーンが変更されました: index {previousTimeZoneIndex} → {newTimeZoneIndex}");
        }

        // ---- スリープスケジュールの更新 ----
        // ★ 問題点1: タイムゾーンの取得方法が間違っている
        // ★ 問題点2: 時刻が変更されてもスケジュールが再設定されない

        if (sleepHourChanged || wakeHourChanged || timeZoneChanged)
        {
            // TimeZoneService を使ってタイムゾーン変更を通知
            if (_timeZoneService != null)
            {
                GameLogger.Log(LogCategory.General,"[SaveSettings] TimeZoneService.UpdateTimeZone() を呼び出します");
                _timeZoneService.UpdateTimeZone(newTimeZoneIndex);
            }
            else
            {
                // TimeZoneService がない場合は直接 SleepController を呼ぶ
                GameLogger.LogWarning(LogCategory.General,"[SaveSettings] TimeZoneService が設定されていません。直接 SleepController を呼び出します。");

                TimeZoneInfo selectedTz = GetTimeZoneFromIndex(newTimeZoneIndex);

                if (_sleepController != null)
                {
                    // ★ 既存のスケジュールをキャンセル
                    _sleepController.CancelSleepSchedule();

                    // ★ 新しいスケジュールを設定
                    _sleepController.ManageSleepCycle(selectedTz);

                    GameLogger.Log(LogCategory.General,$"[SaveSettings] スリープサイクルを再設定しました (TZ: {selectedTz.DisplayName})");
                }
                else
                {
                    GameLogger.LogError(LogCategory.General,"[SaveSettings] SleepController が null です！");
                }
            }
        }
        else
        {
            GameLogger.Log(LogCategory.General,"[SaveSettings] 時刻・タイムゾーンに変更はありません。スケジュールは更新しません。");
        }

        // 犬の位置リセット
        if (resetToggle && resetToggle.isOn)
        {
            _dogController.ResetDogPosition();
            GameLogger.Log(LogCategory.General,"犬の位置をリセットしました。");
        }

        // Dog の色設定を永続化
        if (switcher) switcher.Save();

        // パネル閉じ
        if (inputPanel) inputPanel.SetActive(false);
        GlobalVariables.IsInputUserName = false;
        GlobalVariables.CurrentState = PetState.idle;
    }

    /// <summary>
    /// タイムゾーンインデックスから TimeZoneInfo を取得
    /// </summary>
    private TimeZoneInfo GetTimeZoneFromIndex(int index)
    {
        // _timeZones がある場合はそれを使用
        if (_timeZones != null && _timeZones.Count > 0 && index >= 0 && index < _timeZones.Count)
        {
            return _timeZones[index];
        }

        // なければシステムのタイムゾーンリストから取得
        var systemTimeZones = TimeZoneInfo.GetSystemTimeZones();
        if (index >= 0 && index < systemTimeZones.Count)
        {
            return systemTimeZones[index];
        }

        // フォールバック
        GameLogger.LogWarning(LogCategory.General,$"[GetTimeZoneFromIndex] インデックス {index} が範囲外です。TimeZone.Local を返します。");
        return TimeZoneInfo.Local;
    }

    // Logout
    public void Logout()
    {
        GameLogger.Log(LogCategory.General,"ログアウトしました。");

        if (firebaseAuth != null)
        {
            GameLogger.Log(LogCategory.General,"FirebaseAuth からログアウトします。");
            firebaseAuth.SignOut();
        }

        PlayerPrefs.DeleteKey(PrefsKeys.UserId);
        PlayerPrefs.DeleteKey(PrefsKeys.Email);
        SecurePlayerPrefs.DeleteKey(PrefsKeys.Password);
        PlayerPrefs.DeleteKey(PrefsKeys.LastLoginTime);
        PlayerPrefs.Save();

        SceneManager.LoadScene("login");
    }

    public void ShowInputPanel()
    {
        if (inputPanel) inputPanel.SetActive(true);
        GlobalVariables.IsInputUserName = true;
        GlobalVariables.CurrentState = PetState.ready;
    }

    private void SetTabletMode(bool isOn)
    {
        PlayerPrefs.SetInt(PrefsKeys.TabletMode, isOn ? 1 : 0);
        PlayerPrefs.Save();
        GameLogger.Log(LogCategory.General,$"タブレットモードが {(isOn ? "有効" : "無効")} になりました。");
    }

    public void setDropdownValue(int savedTimeZoneIndex, List<TimeZoneInfo> timeZones)
    {
        // ★ タイムゾーンリストを保持
        _timeZones = timeZones;

        if (!timezoneDropdown) return;
        timezoneDropdown.ClearOptions();
        timezoneDropdown.AddOptions(timeZones.Select(tz => tz.DisplayName).ToList());
        timezoneDropdown.value = Mathf.Clamp(savedTimeZoneIndex, 0, Mathf.Max(0, timezoneDropdown.options.Count - 1));
        timezoneDropdown.RefreshShownValue();
    }
}