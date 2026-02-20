using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Cysharp.Threading.Tasks;

namespace TapHouse.Pocket.MiniGames
{
    /// <summary>
    /// ミニゲーム種類
    /// </summary>
    public enum MiniGameType
    {
        BallCatch,      // ボールキャッチ
        TreatSearch,    // おやつ探し
        DanceMimic      // まねっこダンス
    }

    /// <summary>
    /// ミニゲーム選択画面の管理
    /// </summary>
    public class MiniGameManager : MonoBehaviour
    {
        [Header("Game Selection Buttons")]
        [SerializeField] private Button ballCatchButton;
        [SerializeField] private Button treatSearchButton;
        [SerializeField] private Button danceMimicButton;

        [Header("Back Button")]
        [SerializeField] private Button backButton;

        [Header("Dog Display")]
        [SerializeField] private Animator dogAnimator;
        [SerializeField] private TMP_Text dogMessageText;

        [Header("Scene Names")]
        [SerializeField] private string pocketMainSceneName = "PocketMain";

        [Header("Dog Messages")]
        [SerializeField] private string[] waitingMessages = new string[]
        {
            "何して遊ぶ？",
            "ワクワク！",
            "一緒に遊ぼう！"
        };

        private void Start()
        {
            SetupButtons();
            ShowRandomDogMessage();
            PlayDogWaitingAnimation();
        }

        #region Setup

        private void SetupButtons()
        {
            // ボールキャッチ
            ballCatchButton?.onClick.AddListener(() =>
            {
                StartGame(MiniGameType.BallCatch);
            });

            // おやつ探し
            treatSearchButton?.onClick.AddListener(() =>
            {
                StartGame(MiniGameType.TreatSearch);
            });

            // まねっこダンス
            danceMimicButton?.onClick.AddListener(() =>
            {
                StartGame(MiniGameType.DanceMimic);
            });

            // 戻るボタン
            backButton?.onClick.AddListener(OnBackButtonClicked);
        }

        #endregion

        #region Game Start

        private void StartGame(MiniGameType gameType)
        {
            Debug.Log($"[MiniGameManager] Starting game: {gameType}");

            switch (gameType)
            {
                case MiniGameType.BallCatch:
                    StartBallCatchGame();
                    break;
                case MiniGameType.TreatSearch:
                    StartTreatSearchGame();
                    break;
                case MiniGameType.DanceMimic:
                    StartDanceMimicGame();
                    break;
            }
        }

        private void StartBallCatchGame()
        {
            // ボールキャッチゲームを開始
            // 別シーンに遷移するか、UIを切り替える
            var ballCatchUI = FindObjectOfType<BallCatchGame>();
            if (ballCatchUI != null)
            {
                ballCatchUI.StartGame();
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] BallCatchGame not found, showing placeholder");
                ShowGamePlaceholder("ボールキャッチ");
            }
        }

        private void StartTreatSearchGame()
        {
            var treatSearchUI = FindObjectOfType<TreatSearchGame>();
            if (treatSearchUI != null)
            {
                treatSearchUI.StartGame();
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] TreatSearchGame not found, showing placeholder");
                ShowGamePlaceholder("おやつ探し");
            }
        }

        private void StartDanceMimicGame()
        {
            var danceMimicUI = FindObjectOfType<DanceMimicGame>();
            if (danceMimicUI != null)
            {
                danceMimicUI.StartGame();
            }
            else
            {
                Debug.LogWarning("[MiniGameManager] DanceMimicGame not found, showing placeholder");
                ShowGamePlaceholder("まねっこダンス");
            }
        }

        private void ShowGamePlaceholder(string gameName)
        {
            if (dogMessageText != null)
            {
                dogMessageText.text = $"{gameName}は準備中だよ！";
            }
        }

        #endregion

        #region Dog Display

        private void ShowRandomDogMessage()
        {
            if (dogMessageText != null && waitingMessages.Length > 0)
            {
                int index = Random.Range(0, waitingMessages.Length);
                dogMessageText.text = waitingMessages[index];
            }
        }

        private void PlayDogWaitingAnimation()
        {
            if (dogAnimator != null)
            {
                // 待機アニメーションを再生
                dogAnimator.SetTrigger("Excited");
            }
        }

        #endregion

        #region Navigation

        private void OnBackButtonClicked()
        {
            SceneManager.LoadScene(pocketMainSceneName);
        }

        #endregion
    }
}
