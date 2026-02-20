using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace TapHouse.Pocket.MiniGames
{
    /// <summary>
    /// ポーズの種類
    /// </summary>
    public enum PoseType
    {
        Ote,        // お手（左手）
        Okawari,    // おかわり（右手）
        Fuse,       // ふせ（両方同時）
        Jump        // ジャンプ（上スワイプ）
    }

    /// <summary>
    /// まねっこダンスゲーム
    /// 犬のポーズに合わせてタップするリズムゲーム
    /// </summary>
    public class DanceMimicGame : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject gameUI;
        [SerializeField] private GameObject selectionUI;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text poseNameText;

        [Header("Input Buttons")]
        [SerializeField] private Button leftPawButton;   // 左手（お手）
        [SerializeField] private Button rightPawButton;  // 右手（おかわり）

        [Header("Pose Indicators")]
        [SerializeField] private Image[] poseIndicators;
        [SerializeField] private Image currentPoseIndicator;

        [Header("Dog")]
        [SerializeField] private Animator dogAnimator;
        [SerializeField] private TMP_Text dogMessageText;

        [Header("Game Settings")]
        [SerializeField] private int totalPoses = 10;
        [SerializeField] private float poseDisplayTime = 2f;
        [SerializeField] private float inputWindowTime = 1.5f;

        [Header("Scoring")]
        [SerializeField] private int perfectScore = 30;
        [SerializeField] private int goodScore = 20;
        [SerializeField] private int missScore = 0;
        [SerializeField] private int comboBonus = 5;

        // Game State
        private int _currentPoseIndex;
        private int _score;
        private int _combo;
        private int _maxCombo;
        private bool _isPlaying;
        private bool _canInput;
        private PoseType _currentPose;
        private float _inputStartTime;
        private bool _leftPressed;
        private bool _rightPressed;
        private List<PoseType> _poseSequence;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<int> OnGameEnded;

        private void Update()
        {
            if (!_isPlaying || !_canInput) return;

            // スワイプ検出（ジャンプ用）
            DetectSwipe();
        }

        #region Public Methods

        public void StartGame()
        {
            Debug.Log("[DanceMimicGame] Starting game");

            // UI切り替え
            if (selectionUI != null) selectionUI.SetActive(false);
            if (gameUI != null) gameUI.SetActive(true);

            // 初期化
            _currentPoseIndex = 0;
            _score = 0;
            _combo = 0;
            _maxCombo = 0;
            _isPlaying = true;
            _canInput = false;

            // ポーズシーケンス生成
            GeneratePoseSequence();

            // ボタン設定
            SetupButtons();
            UpdateUI();

            // ゲーム開始
            ShowDogMessage("まねっこしよう！");
            StartGameLoopAsync().Forget();
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

        private void SetupButtons()
        {
            quitButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.AddListener(QuitGame);

            leftPawButton?.onClick.RemoveAllListeners();
            leftPawButton?.onClick.AddListener(OnLeftPawPressed);

            rightPawButton?.onClick.RemoveAllListeners();
            rightPawButton?.onClick.AddListener(OnRightPawPressed);
        }

        private void GeneratePoseSequence()
        {
            _poseSequence = new List<PoseType>();

            // ジャンプは少なめに
            PoseType[] availablePoses = { PoseType.Ote, PoseType.Okawari, PoseType.Fuse, PoseType.Jump };
            float[] weights = { 0.3f, 0.3f, 0.25f, 0.15f };

            for (int i = 0; i < totalPoses; i++)
            {
                float rand = UnityEngine.Random.value;
                float cumulative = 0f;
                PoseType selectedPose = PoseType.Ote;

                for (int j = 0; j < availablePoses.Length; j++)
                {
                    cumulative += weights[j];
                    if (rand <= cumulative)
                    {
                        selectedPose = availablePoses[j];
                        break;
                    }
                }

                _poseSequence.Add(selectedPose);
            }
        }

        #endregion

        #region Game Loop

        private async UniTaskVoid StartGameLoopAsync()
        {
            await UniTask.Delay(1000);

            ShowHint("犬のポーズをまねしよう！");

            while (_isPlaying && _currentPoseIndex < _poseSequence.Count)
            {
                await PlayPoseRound();
                _currentPoseIndex++;
            }

            if (_isPlaying)
            {
                EndGame();
            }
        }

        private async UniTask PlayPoseRound()
        {
            _currentPose = _poseSequence[_currentPoseIndex];
            _canInput = false;
            _leftPressed = false;
            _rightPressed = false;

            // ポーズ表示
            ShowPose(_currentPose);
            PlayDogPoseAnimation(_currentPose);

            await UniTask.Delay((int)(poseDisplayTime * 1000));

            // 入力受付開始
            _canInput = true;
            _inputStartTime = Time.time;
            ShowHint(GetPoseHint(_currentPose));

            // 入力待ち
            bool inputReceived = false;
            while (Time.time - _inputStartTime < inputWindowTime && !inputReceived)
            {
                inputReceived = CheckInput();
                await UniTask.Yield();
            }

            _canInput = false;

            if (!inputReceived)
            {
                OnMiss();
            }

            await UniTask.Delay(500);
        }

        #endregion

        #region Input Handling

        private void OnLeftPawPressed()
        {
            if (!_canInput) return;
            _leftPressed = true;
        }

        private void OnRightPawPressed()
        {
            if (!_canInput) return;
            _rightPressed = true;
        }

        private bool CheckInput()
        {
            switch (_currentPose)
            {
                case PoseType.Ote:
                    if (_leftPressed && !_rightPressed)
                    {
                        JudgeInput();
                        return true;
                    }
                    break;

                case PoseType.Okawari:
                    if (_rightPressed && !_leftPressed)
                    {
                        JudgeInput();
                        return true;
                    }
                    break;

                case PoseType.Fuse:
                    if (_leftPressed && _rightPressed)
                    {
                        JudgeInput();
                        return true;
                    }
                    break;

                case PoseType.Jump:
                    // スワイプで処理
                    break;
            }

            return false;
        }

        private Vector2 _swipeStart;
        private bool _isSwiping;

        private void DetectSwipe()
        {
            if (_currentPose != PoseType.Jump) return;

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                _swipeStart = Input.mousePosition;
                _isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && _isSwiping)
            {
                Vector2 swipeEnd = Input.mousePosition;
                if (swipeEnd.y - _swipeStart.y > 100) // 上方向100px以上
                {
                    JudgeInput();
                }
                _isSwiping = false;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    _swipeStart = touch.position;
                    _isSwiping = true;
                }
                else if (touch.phase == TouchPhase.Ended && _isSwiping)
                {
                    if (touch.position.y - _swipeStart.y > 100)
                    {
                        JudgeInput();
                    }
                    _isSwiping = false;
                }
            }
#endif
        }

        private void JudgeInput()
        {
            _canInput = false;

            float elapsed = Time.time - _inputStartTime;
            float ratio = elapsed / inputWindowTime;

            if (ratio < 0.3f)
            {
                OnPerfect();
            }
            else if (ratio < 0.7f)
            {
                OnGood();
            }
            else
            {
                OnGood(); // 遅くてもGood判定
            }
        }

        #endregion

        #region Scoring

        private void OnPerfect()
        {
            _combo++;
            if (_combo > _maxCombo) _maxCombo = _combo;

            int points = perfectScore + (_combo * comboBonus);
            _score += points;

            ShowHint("Perfect!");
            ShowDogMessage("すごい！");
            PlayDogHappyAnimation();

            AddLove(3);
            UpdateUI();
        }

        private void OnGood()
        {
            _combo++;
            if (_combo > _maxCombo) _maxCombo = _combo;

            int points = goodScore + (_combo * comboBonus);
            _score += points;

            ShowHint("Good!");
            ShowDogMessage("いいね！");
            PlayDogHappyAnimation();

            AddLove(1);
            UpdateUI();
        }

        private void OnMiss()
        {
            _combo = 0;

            ShowHint("Miss...");
            ShowDogMessage("残念...");
            PlayDogSadAnimation();

            UpdateUI();
        }

        #endregion

        #region Game End

        private void EndGame()
        {
            _isPlaying = false;

            ShowDogMessage("お疲れ様！");
            ShowHint($"ゲーム終了！ スコア: {_score} 最大コンボ: {_maxCombo}");
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

        #region Display

        private void ShowPose(PoseType pose)
        {
            if (poseNameText != null)
            {
                poseNameText.text = GetPoseName(pose);
            }
        }

        private string GetPoseName(PoseType pose)
        {
            return pose switch
            {
                PoseType.Ote => "お手！",
                PoseType.Okawari => "おかわり！",
                PoseType.Fuse => "ふせ！",
                PoseType.Jump => "ジャンプ！",
                _ => ""
            };
        }

        private string GetPoseHint(PoseType pose)
        {
            return pose switch
            {
                PoseType.Ote => "左手ボタンをタップ！",
                PoseType.Okawari => "右手ボタンをタップ！",
                PoseType.Fuse => "両方同時にタップ！",
                PoseType.Jump => "上にスワイプ！",
                _ => ""
            };
        }

        #endregion

        #region Dog Animation

        private void PlayDogPoseAnimation(PoseType pose)
        {
            if (dogAnimator == null) return;

            string triggerName = pose switch
            {
                PoseType.Ote => "Ote",
                PoseType.Okawari => "Okawari",
                PoseType.Fuse => "Fuse",
                PoseType.Jump => "Jump",
                _ => "Idle"
            };

            dogAnimator.SetTrigger(triggerName);
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

            if (comboText != null)
            {
                comboText.text = _combo > 0 ? $"コンボ: {_combo}" : "";
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
