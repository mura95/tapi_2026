using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace TapHouse.MetaverseWalk.UI
{
    /// <summary>
    /// 散歩確認ポップアップの制御
    /// 足跡アイコン + メッセージ + はい/行かないボタン
    /// </summary>
    public class WalkConfirmPopup : MonoBehaviour
    {
        [SerializeField] private Image footprintIcon;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action onConfirm;
        private Action onDecline;

        /// <summary>
        /// コールバックを設定して初期化
        /// </summary>
        public void Initialize(Action onConfirm, Action onDecline)
        {
            this.onConfirm = onConfirm;
            this.onDecline = onDecline;

            if (yesButton != null)
                yesButton.onClick.AddListener(OnYesClicked);
            if (noButton != null)
                noButton.onClick.AddListener(OnNoClicked);

            // ペット名を取得してメッセージを設定
            string petName = PlayerPrefs.GetString(PrefsKeys.PetName, "わんちゃん");
            if (messageText != null)
            {
                messageText.text = $"{petName}が散歩に\n行きたがっています！\n散歩に行きますか？";
            }
        }

        private void OnYesClicked()
        {
            onConfirm?.Invoke();
            Destroy(gameObject);
        }

        private void OnNoClicked()
        {
            onDecline?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (yesButton != null)
                yesButton.onClick.RemoveListener(OnYesClicked);
            if (noButton != null)
                noButton.onClick.RemoveListener(OnNoClicked);
        }
    }
}
