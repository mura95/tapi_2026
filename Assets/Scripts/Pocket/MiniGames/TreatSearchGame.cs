using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace TapHouse.Pocket.MiniGames
{
    /// <summary>
    /// おやつ探しゲーム（シェルゲーム/カップゲーム）
    /// 3つのカップの中に隠れたおやつを当てる
    /// </summary>
    public class TreatSearchGame : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject gameUI;
        [SerializeField] private GameObject selectionUI;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text hintText;

        [Header("Cups")]
        [SerializeField] private Button[] cupButtons;
        [SerializeField] private Image[] cupImages;
        [SerializeField] private GameObject treatPrefab;

        [Header("Dog")]
        [SerializeField] private Transform dogTransform;
        [SerializeField] private Animator dogAnimator;
        [SerializeField] private TMP_Text dogMessageText;

        [Header("Game Settings")]
        [SerializeField] private int totalRounds = 5;
        [SerializeField] private int shuffleCount = 3;
        [SerializeField] private float shuffleSpeed = 0.3f;

        [Header("Scoring")]
        [SerializeField] private int correctScore = 20;

        // Game State
        private int _currentRound;
        private int _score;
        private int _correctCupIndex;
        private bool _isPlaying;
        private bool _canSelect;
        private bool _isShuffling;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<int> OnGameEnded;

        #region Public Methods

        public void StartGame()
        {
            Debug.Log("[TreatSearchGame] Starting game");

            // UI切り替え
            if (selectionUI != null) selectionUI.SetActive(false);
            if (gameUI != null) gameUI.SetActive(true);

            // 初期化
            _currentRound = 0;
            _score = 0;
            _isPlaying = true;
            _canSelect = false;

            // ボタン設定
            quitButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.AddListener(QuitGame);

            SetupCupButtons();
            UpdateUI();

            // 最初のラウンド開始
            StartRoundAsync().Forget();
        }

        public void QuitGame()
        {
            _isPlaying = false;

            // UI切り替え
            if (gameUI != null) gameUI.SetActive(false);
            if (selectionUI != null) selectionUI.SetActive(true);
        }

        #endregion

        #region Setup

        private void SetupCupButtons()
        {
            for (int i = 0; i < cupButtons.Length; i++)
            {
                int index = i; // クロージャ用
                cupButtons[i]?.onClick.RemoveAllListeners();
                cupButtons[i]?.onClick.AddListener(() => OnCupSelected(index));
            }
        }

        #endregion

        #region Game Flow

        private async UniTaskVoid StartRoundAsync()
        {
            _currentRound++;
            _canSelect = false;

            UpdateUI();
            ShowDogMessage("見ててね...");
            ShowHint("おやつを隠すよ...");

            // カップをリセット
            ResetCups();

            await UniTask.Delay(500);

            // おやつを隠す
            _correctCupIndex = UnityEngine.Random.Range(0, cupButtons.Length);
            await ShowTreatHiding(_correctCupIndex);

            // シャッフル
            ShowHint("シャッフル！");
            _isShuffling = true;
            await ShuffleCups();
            _isShuffling = false;

            // 犬のヒント
            PlayDogHintAnimation(_correctCupIndex);
            ShowDogMessage("クンクン...");
            ShowHint("どこかな？タップして選んでね！");

            _canSelect = true;
        }

        private async UniTask ShowTreatHiding(int cupIndex)
        {
            // おやつを表示してからカップで隠す演出
            if (cupImages != null && cupIndex < cupImages.Length)
            {
                // カップを持ち上げる
                var cup = cupImages[cupIndex];
                Vector3 originalPos = cup.transform.localPosition;
                cup.transform.localPosition = originalPos + Vector3.up * 50;

                await UniTask.Delay(500);

                // カップを戻す
                cup.transform.localPosition = originalPos;
            }

            await UniTask.Delay(300);
        }

        private async UniTask ShuffleCups()
        {
            if (cupImages == null || cupImages.Length < 2) return;

            for (int i = 0; i < shuffleCount; i++)
            {
                // ランダムな2つのカップを選んで入れ替え
                int cup1 = UnityEngine.Random.Range(0, cupImages.Length);
                int cup2 = (cup1 + UnityEngine.Random.Range(1, cupImages.Length)) % cupImages.Length;

                await SwapCups(cup1, cup2);
            }
        }

        private async UniTask SwapCups(int index1, int index2)
        {
            if (cupImages == null) return;
            if (index1 >= cupImages.Length || index2 >= cupImages.Length) return;

            var cup1 = cupImages[index1];
            var cup2 = cupImages[index2];

            Vector3 pos1 = cup1.transform.localPosition;
            Vector3 pos2 = cup2.transform.localPosition;

            // アニメーションで入れ替え
            float elapsed = 0f;
            while (elapsed < shuffleSpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shuffleSpeed;

                // 半円を描くように移動
                float arc = Mathf.Sin(t * Mathf.PI) * 30f;

                cup1.transform.localPosition = Vector3.Lerp(pos1, pos2, t) + Vector3.up * arc;
                cup2.transform.localPosition = Vector3.Lerp(pos2, pos1, t) + Vector3.up * arc;

                await UniTask.Yield();
            }

            cup1.transform.localPosition = pos2;
            cup2.transform.localPosition = pos1;

            // 正解位置も追跡
            if (_correctCupIndex == index1)
            {
                _correctCupIndex = index2;
            }
            else if (_correctCupIndex == index2)
            {
                _correctCupIndex = index1;
            }
        }

        private void OnCupSelected(int index)
        {
            if (!_canSelect || !_isPlaying) return;

            _canSelect = false;

            bool isCorrect = index == _correctCupIndex;

            // すべてのカップを開ける
            RevealAllCups(index, isCorrect);

            if (isCorrect)
            {
                OnCorrectAnswer();
            }
            else
            {
                OnWrongAnswer();
            }
        }

        private void OnCorrectAnswer()
        {
            _score++;
            OnScoreChanged?.Invoke(_score);

            ShowDogMessage("やったー！");
            ShowHint($"正解！ +{correctScore}点");
            PlayDogHappyAnimation();

            // 愛情度アップ
            AddLove(2);

            UpdateUI();
            ContinueOrEndAsync().Forget();
        }

        private void OnWrongAnswer()
        {
            ShowDogMessage("残念...");
            ShowHint("はずれ...");
            PlayDogSadAnimation();

            UpdateUI();
            ContinueOrEndAsync().Forget();
        }

        private async UniTaskVoid ContinueOrEndAsync()
        {
            await UniTask.Delay(2000);

            if (!_isPlaying) return;

            if (_currentRound >= totalRounds)
            {
                EndGame();
            }
            else
            {
                StartRoundAsync().Forget();
            }
        }

        private void EndGame()
        {
            _isPlaying = false;

            ShowDogMessage("お疲れ様！");
            ShowHint($"ゲーム終了！ {_score}/{totalRounds} 正解！");
            OnGameEnded?.Invoke(_score);

            ReturnToSelectionAsync().Forget();
        }

        private async UniTaskVoid ReturnToSelectionAsync()
        {
            await UniTask.Delay(3000);

            if (gameUI != null) gameUI.SetActive(false);
            if (selectionUI != null) selectionUI.SetActive(true);
        }

        #endregion

        #region Cup Display

        private void ResetCups()
        {
            // カップの表示をリセット
            if (cupImages != null)
            {
                foreach (var cup in cupImages)
                {
                    if (cup != null)
                    {
                        cup.color = Color.white;
                    }
                }
            }
        }

        private void RevealAllCups(int selectedIndex, bool isCorrect)
        {
            if (cupImages == null) return;

            for (int i = 0; i < cupImages.Length; i++)
            {
                if (cupImages[i] == null) continue;

                if (i == _correctCupIndex)
                {
                    // 正解のカップ
                    cupImages[i].color = Color.green;
                }
                else if (i == selectedIndex && !isCorrect)
                {
                    // 選んだカップ（不正解）
                    cupImages[i].color = Color.red;
                }
            }
        }

        #endregion

        #region Dog Animation

        private void PlayDogHintAnimation(int correctIndex)
        {
            if (dogAnimator == null || dogTransform == null) return;

            // 犬が正解のカップを見る
            if (cupImages != null && correctIndex < cupImages.Length)
            {
                Vector3 targetPos = cupImages[correctIndex].transform.position;
                Vector3 direction = (targetPos - dogTransform.position).normalized;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    dogTransform.rotation = Quaternion.LookRotation(direction);
                }
            }

            dogAnimator.SetTrigger("Sniff");
        }

        private void PlayDogHappyAnimation()
        {
            if (dogAnimator != null)
            {
                dogAnimator.SetTrigger("Happy");
            }
        }

        private void PlayDogSadAnimation()
        {
            if (dogAnimator != null)
            {
                dogAnimator.SetTrigger("Sad");
            }
        }

        #endregion

        #region UI

        private void UpdateUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"スコア: {_score}";
            }

            if (roundText != null)
            {
                roundText.text = $"ラウンド: {_currentRound}/{totalRounds}";
            }
        }

        private void ShowHint(string message)
        {
            if (hintText != null)
            {
                hintText.text = message;
            }
        }

        private void ShowDogMessage(string message)
        {
            if (dogMessageText != null)
            {
                dogMessageText.text = message;
            }
        }

        #endregion

        #region Love Manager

        private void AddLove(int amount)
        {
            var dogStateController = FindObjectOfType<DogStateController>();
            if (dogStateController != null && dogStateController.LoveManager != null)
            {
                dogStateController.LoveManager.Increase(amount);
            }
        }

        #endregion
    }
}
