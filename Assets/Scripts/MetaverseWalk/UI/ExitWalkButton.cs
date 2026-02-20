using UnityEngine;
using UnityEngine.UI;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.UI
{
    /// <summary>
    /// 散歩終了ボタン
    /// 確認ダイアログを表示し、承認後にメインシーンへ戻る
    /// </summary>
    public class ExitWalkButton : MonoBehaviour
    {
        [Header("ボタン")]
        [SerializeField] private Button exitButton;

        [Header("確認ダイアログ")]
        [SerializeField] private GameObject confirmDialog;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        [Header("設定")]
        [SerializeField] private bool showConfirmDialog = true;

        private void Start()
        {
            SetupButtons();
            HideConfirmDialog();
        }

        private void SetupButtons()
        {
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitButtonClicked);
            }

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            }

            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.AddListener(OnConfirmNo);
            }
        }

        private void OnExitButtonClicked()
        {
            if (showConfirmDialog && confirmDialog != null)
            {
                ShowConfirmDialog();
            }
            else
            {
                ExitWalk();
            }
        }

        private void ShowConfirmDialog()
        {
            if (confirmDialog != null)
            {
                confirmDialog.SetActive(true);
            }
        }

        private void HideConfirmDialog()
        {
            if (confirmDialog != null)
            {
                confirmDialog.SetActive(false);
            }
        }

        private void OnConfirmYes()
        {
            HideConfirmDialog();
            ExitWalk();
        }

        private void OnConfirmNo()
        {
            HideConfirmDialog();
        }

        private void ExitWalk()
        {
            if (MetaverseManager.Instance != null)
            {
                MetaverseManager.Instance.ExitToMainScene();
            }
            else
            {
                Debug.LogWarning("[ExitWalkButton] MetaverseManager not found");
            }
        }

        private void OnDestroy()
        {
            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(OnExitButtonClicked);
            }

            if (confirmYesButton != null)
            {
                confirmYesButton.onClick.RemoveListener(OnConfirmYes);
            }

            if (confirmNoButton != null)
            {
                confirmNoButton.onClick.RemoveListener(OnConfirmNo);
            }
        }
    }
}
