using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TapHouse.Logging;

namespace TapHouse.Reminder
{
    /// <summary>
    /// リマインダー通知UIコントローラー
    /// 画面中央にメッセージと完了ボタンを表示
    /// </summary>
    public class ReminderNotificationUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _completeButton;
        [SerializeField] private Button _backgroundButton;

        [Header("Type Icons (Optional)")]
        [SerializeField] private Sprite _medicationIcon;
        [SerializeField] private Sprite _mealIcon;
        [SerializeField] private Sprite _hydrationIcon;
        [SerializeField] private Sprite _exerciseIcon;
        [SerializeField] private Sprite _restIcon;
        [SerializeField] private Sprite _appointmentIcon;

        private ReminderManager _manager;
        private ReminderData _reminder;

        void Start()
        {
            // Canvasを最前面に設定
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
            }

            // ボタンイベント設定
            if (_completeButton != null)
            {
                _completeButton.onClick.AddListener(OnCompleteClicked);
            }

            if (_backgroundButton != null)
            {
                _backgroundButton.onClick.AddListener(OnCompleteClicked);
            }
        }

        /// <summary>
        /// 初期化（ReminderManagerから呼ばれる）
        /// </summary>
        public void Initialize(ReminderManager manager, ReminderData reminder)
        {
            _manager = manager;
            _reminder = reminder;

            // ユーザー名を取得
            string displayName = PlayerPrefs.GetString(PrefsKeys.DisplayName, "");

            // メッセージを設定
            string message = GetMessageForType(reminder.GetReminderType(), displayName);
            if (_messageText != null)
            {
                _messageText.text = message;
            }

            // タイプラベルを設定
            if (_typeText != null)
            {
                _typeText.text = GetTypeLabelJapanese(reminder.GetReminderType());
            }

            // アイコンを設定
            if (_iconImage != null)
            {
                var icon = GetIconForType(reminder.GetReminderType());
                if (icon != null)
                {
                    _iconImage.sprite = icon;
                }
            }

            GameLogger.Log(LogCategory.UI, $"[ReminderNotificationUI] Initialized: {message}");
        }

        /// <summary>
        /// リマインダータイプに応じたメッセージを生成
        /// </summary>
        private string GetMessageForType(ReminderType type, string userName)
        {
            string prefix = string.IsNullOrEmpty(userName) ? "" : $"{userName}さん、";

            return type switch
            {
                ReminderType.Medication => $"{prefix}お薬を服用してください",
                ReminderType.Meal => $"{prefix}お食事の時間です",
                ReminderType.Hydration => $"{prefix}お水を飲んでください",
                ReminderType.Exercise => $"{prefix}体操の時間です",
                ReminderType.Rest => $"{prefix}休憩してください",
                ReminderType.Appointment => $"{prefix}予定があります",
                _ => $"{prefix}リマインダーです"
            };
        }

        /// <summary>
        /// リマインダータイプの日本語ラベル
        /// </summary>
        private string GetTypeLabelJapanese(ReminderType type)
        {
            return type switch
            {
                ReminderType.Medication => "服薬",
                ReminderType.Meal => "食事",
                ReminderType.Hydration => "水分補給",
                ReminderType.Exercise => "運動",
                ReminderType.Rest => "休憩",
                ReminderType.Appointment => "予定",
                _ => "お知らせ"
            };
        }

        /// <summary>
        /// リマインダータイプに応じたアイコンを取得
        /// </summary>
        private Sprite GetIconForType(ReminderType type)
        {
            return type switch
            {
                ReminderType.Medication => _medicationIcon,
                ReminderType.Meal => _mealIcon,
                ReminderType.Hydration => _hydrationIcon,
                ReminderType.Exercise => _exerciseIcon,
                ReminderType.Rest => _restIcon,
                ReminderType.Appointment => _appointmentIcon,
                _ => _medicationIcon
            };
        }

        /// <summary>
        /// 完了ボタン押下時の処理
        /// </summary>
        private void OnCompleteClicked()
        {
            GameLogger.Log(LogCategory.UI, "[ReminderNotificationUI] Complete button clicked");

            _manager?.OnReminderCompleted();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_completeButton != null)
            {
                _completeButton.onClick.RemoveAllListeners();
            }

            if (_backgroundButton != null)
            {
                _backgroundButton.onClick.RemoveAllListeners();
            }
        }
    }
}
