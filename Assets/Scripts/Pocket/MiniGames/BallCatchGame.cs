using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

namespace TapHouse.Pocket.MiniGames
{
    /// <summary>
    /// ボールキャッチゲーム
    /// スワイプでボールを投げて、犬がキャッチする
    /// </summary>
    public class BallCatchGame : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject gameUI;
        [SerializeField] private GameObject selectionUI;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text remainingBallsText;
        [SerializeField] private TMP_Text hintText;

        [Header("Ball")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float minThrowForce = 5f;
        [SerializeField] private float maxThrowForce = 15f;

        [Header("Dog")]
        [SerializeField] private Transform dogTransform;
        [SerializeField] private Animator dogAnimator;
        [SerializeField] private float dogMoveSpeed = 5f;

        [Header("Game Settings")]
        [SerializeField] private int maxBalls = 5;
        [SerializeField] private float catchDistance = 1.5f;

        [Header("Scoring")]
        [SerializeField] private int normalCatchScore = 10;
        [SerializeField] private int bonusCatchScore = 20;
        [SerializeField] private int jumpCatchScore = 30;

        // Game State
        private int _remainingBalls;
        private int _score;
        private bool _isPlaying;
        private bool _canThrow;
        private GameObject _currentBall;
        private Vector2 _swipeStart;
        private bool _isSwiping;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<int> OnGameEnded;

        private void Update()
        {
            if (!_isPlaying) return;

            HandleInput();

            if (_currentBall != null)
            {
                UpdateDogChase();
            }
        }

        #region Public Methods

        public void StartGame()
        {
            Debug.Log("[BallCatchGame] Starting game");

            // UI切り替え
            if (selectionUI != null) selectionUI.SetActive(false);
            if (gameUI != null) gameUI.SetActive(true);

            // 初期化
            _remainingBalls = maxBalls;
            _score = 0;
            _isPlaying = true;
            _canThrow = true;

            // ボタン設定
            quitButton?.onClick.RemoveAllListeners();
            quitButton?.onClick.AddListener(QuitGame);

            UpdateUI();
            ShowHint("上にスワイプしてボールを投げよう！");

            // 犬の初期アニメーション
            PlayDogIdleAnimation();
        }

        public void QuitGame()
        {
            _isPlaying = false;

            // ボールがあれば削除
            if (_currentBall != null)
            {
                Destroy(_currentBall);
                _currentBall = null;
            }

            // UI切り替え
            if (gameUI != null) gameUI.SetActive(false);
            if (selectionUI != null) selectionUI.SetActive(true);
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            if (!_canThrow) return;

#if UNITY_EDITOR
            HandleMouseInput();
#else
            HandleTouchInput();
#endif
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _swipeStart = Input.mousePosition;
                _isSwiping = true;
            }
            else if (Input.GetMouseButtonUp(0) && _isSwiping)
            {
                Vector2 swipeEnd = Input.mousePosition;
                ProcessSwipe(_swipeStart, swipeEnd);
                _isSwiping = false;
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _swipeStart = touch.position;
                    _isSwiping = true;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isSwiping)
                    {
                        ProcessSwipe(_swipeStart, touch.position);
                        _isSwiping = false;
                    }
                    break;
            }
        }

        private void ProcessSwipe(Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;

            // 上方向のスワイプのみ有効
            if (delta.y < 50) return; // 最低50pxの上方向スワイプ

            // スワイプの強さを計算（0-1）
            float strength = Mathf.Clamp01(delta.magnitude / 500f);

            // スワイプの方向を計算
            Vector2 direction = delta.normalized;

            ThrowBall(direction, strength);
        }

        #endregion

        #region Ball Throwing

        private void ThrowBall(Vector2 direction, float strength)
        {
            if (_remainingBalls <= 0 || _currentBall != null) return;

            _canThrow = false;
            _remainingBalls--;

            // ボール生成
            Vector3 spawnPos = throwPoint != null ? throwPoint.position : new Vector3(0, 1, -5);
            _currentBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);

            // 投げる力を計算
            float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, strength);
            Vector3 throwDirection = new Vector3(direction.x * 0.5f, direction.y, 1f).normalized;

            // Rigidbodyで投げる
            var rb = _currentBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
            }

            // 犬を追いかけさせる
            PlayDogRunAnimation();
            ShowHint("");

            UpdateUI();
        }

        #endregion

        #region Dog Chase

        private void UpdateDogChase()
        {
            if (_currentBall == null || dogTransform == null) return;

            // 犬をボールに向かって移動
            Vector3 targetPos = _currentBall.transform.position;
            targetPos.y = dogTransform.position.y; // Y座標は維持

            Vector3 direction = (targetPos - dogTransform.position).normalized;
            dogTransform.position += direction * dogMoveSpeed * Time.deltaTime;

            // 犬の向きを調整
            if (direction != Vector3.zero)
            {
                dogTransform.rotation = Quaternion.LookRotation(direction);
            }

            // キャッチ判定
            float distance = Vector3.Distance(dogTransform.position, _currentBall.transform.position);
            if (distance < catchDistance)
            {
                OnBallCaught();
            }

            // ボールが地面に落ちた場合
            if (_currentBall.transform.position.y < -1f)
            {
                OnBallMissed();
            }
        }

        private void OnBallCaught()
        {
            // スコア加算
            int points = normalCatchScore;

            // ジャンプキャッチ判定（ボールが高い位置の場合）
            if (_currentBall.transform.position.y > 2f)
            {
                points = jumpCatchScore;
                ShowHint("ジャンプキャッチ！+30");
            }
            else
            {
                ShowHint("キャッチ！+10");
            }

            _score += points;
            OnScoreChanged?.Invoke(_score);

            // 愛情度アップ
            AddLove(points == jumpCatchScore ? 3 : 1);

            // 犬の喜びアニメーション
            PlayDogHappyAnimation();

            // ボール削除
            Destroy(_currentBall);
            _currentBall = null;

            UpdateUI();
            CheckGameEnd();
        }

        private void OnBallMissed()
        {
            ShowHint("残念...");

            // ボール削除
            Destroy(_currentBall);
            _currentBall = null;

            // 犬の残念アニメーション
            PlayDogSadAnimation();

            UpdateUI();
            CheckGameEnd();
        }

        #endregion

        #region Game Flow

        private void CheckGameEnd()
        {
            if (_remainingBalls <= 0)
            {
                EndGame();
            }
            else
            {
                // 次のボールを投げられるようにする
                ResetForNextThrowAsync().Forget();
            }
        }

        private async UniTaskVoid ResetForNextThrowAsync()
        {
            await UniTask.Delay(1500);

            if (!_isPlaying) return;

            // 犬を初期位置に戻す
            if (dogTransform != null)
            {
                dogTransform.position = new Vector3(0, 0, 0);
            }

            PlayDogIdleAnimation();
            _canThrow = true;
            ShowHint("次のボールを投げよう！");
        }

        private void EndGame()
        {
            _isPlaying = false;

            ShowHint($"ゲーム終了！スコア: {_score}");
            OnGameEnded?.Invoke(_score);

            // 結果表示後に選択画面に戻る
            ReturnToSelectionAsync().Forget();
        }

        private async UniTaskVoid ReturnToSelectionAsync()
        {
            await UniTask.Delay(3000);

            if (gameUI != null) gameUI.SetActive(false);
            if (selectionUI != null) selectionUI.SetActive(true);
        }

        #endregion

        #region Dog Animation

        private void PlayDogIdleAnimation()
        {
            if (dogAnimator != null)
            {
                dogAnimator.SetBool("WalkBool", false);
                dogAnimator.SetTrigger("Idle");
            }
        }

        private void PlayDogRunAnimation()
        {
            if (dogAnimator != null)
            {
                dogAnimator.SetBool("WalkBool", true);
                dogAnimator.SetFloat("walk_y", 1f);
            }
        }

        private void PlayDogHappyAnimation()
        {
            if (dogAnimator != null)
            {
                dogAnimator.SetBool("WalkBool", false);
                dogAnimator.SetTrigger("Happy");
            }
        }

        private void PlayDogSadAnimation()
        {
            if (dogAnimator != null)
            {
                dogAnimator.SetBool("WalkBool", false);
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

            if (remainingBallsText != null)
            {
                remainingBallsText.text = $"残り: {_remainingBalls}";
            }
        }

        private void ShowHint(string message)
        {
            if (hintText != null)
            {
                hintText.text = message;
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
