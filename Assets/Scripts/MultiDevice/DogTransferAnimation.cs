using UnityEngine;
using System.Collections;
using TapHouse.Logging;

namespace TapHouse.MultiDevice
{
    /// <summary>
    /// 犬のデバイス間転送時のアニメーション制御
    /// 右への退場と左からの登場を担当
    /// コルーチンベースのシンプルな実装
    /// </summary>
    public class DogTransferAnimation : MonoBehaviour
    {
        #region Inspector Fields
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private DogController _dogController;
        [SerializeField] private TurnAndMoveHandler _turnAndMoveHandler;

        [Header("Animation Settings")]
        [SerializeField] private float exitSpeed = 3f;
        [SerializeField] private float enterSpeed = 2.5f;

        [Header("Positions")]
        [SerializeField] private Vector3 centerPosition = Vector3.zero;
        [SerializeField] private Vector3 exitPosition = new Vector3(3f, 0f, -3.5f);
        [SerializeField] private Vector3 enterStartPosition = new Vector3(-3f, 0f, -3.5f);

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;
        #endregion

        #region Private Fields
        private bool _isAnimating;
        private Coroutine _currentCoroutine;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_dogController == null)
                _dogController = GetComponent<DogController>();
            if (_turnAndMoveHandler == null)
                _turnAndMoveHandler = GetComponent<TurnAndMoveHandler>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 右側へ退場するアニメーション開始
        /// </summary>
        public void ExitToRight()
        {
            if (_isAnimating)
            {
                Log("Animation already in progress");
                return;
            }

            _currentCoroutine = StartCoroutine(ExitToRightCoroutine());
        }

        /// <summary>
        /// 左側から登場するアニメーション開始
        /// </summary>
        public void EnterFromLeft()
        {
            if (_isAnimating)
            {
                Log("Animation already in progress");
                return;
            }

            // 位置を画面外に設定してからアクティブにする
            Vector3 startPos = new Vector3(enterStartPosition.x, centerPosition.y, centerPosition.z);
            transform.position = startPos;
            // GameObjectが非アクティブの場合、先にアクティブにする（コルーチン開始に必要）
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            _currentCoroutine = StartCoroutine(EnterFromLeftCoroutine());
        }

        /// <summary>
        /// アニメーション中かどうか
        /// </summary>
        public bool IsAnimating => _isAnimating;

        /// <summary>
        /// アニメーションを強制停止
        /// </summary>
        public void ForceStop()
        {
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
                _currentCoroutine = null;
            }
            ResetAnimationFlags();
            _isAnimating = false;
        }
        #endregion

        #region Coroutines
        private IEnumerator ExitToRightCoroutine()
        {
            _isAnimating = true;
            Log("Starting exit animation...");

            // TurnAndMoveHandlerで退場位置へ移動
            Vector3 targetPos = new Vector3(exitPosition.x, transform.position.y, exitPosition.z);

            if (_turnAndMoveHandler != null)
            {
                _turnAndMoveHandler.StartTurnAndMove(targetPos, exitSpeed);
                Log("Moving to exit position...");
                // 移動完了を待つ（stateがidleになるまで）
                while (GlobalVariables.CurrentState == PetState.moving)
                {
                    yield return null;
                }
            }
            else
            {
                // フォールバック
                GlobalVariables.CurrentState = PetState.moving;
                Log("Moving to exit position (fallback)...");
                SetWalkingAnimation(true);
                yield return StartCoroutine(MoveToPositionCoroutine(targetPos, exitSpeed));
                SetWalkingAnimation(false);
                GlobalVariables.CurrentState = PetState.idle;
            }

            // 非表示
            if (_dogController != null)
            {
                _dogController.SetVisible(false);
            }
            else
            {
                transform.gameObject.SetActive(false);
            }

            Log("Exit animation completed");
            _isAnimating = false;
            _currentCoroutine = null;
        }

        private IEnumerator EnterFromLeftCoroutine()
        {
            _isAnimating = true;
            Log("Starting enter animation...");

            // 表示
            if (_dogController != null)
            {
                _dogController.SetVisible(true);
            }

            // TurnAndMoveHandlerで中央へ移動
            if (_turnAndMoveHandler != null)
            {
                _turnAndMoveHandler.StartTurnAndMove(centerPosition, enterSpeed);

                // 移動完了を待つ（stateがidleになるまで）
                while (GlobalVariables.CurrentState == PetState.moving)
                {
                    yield return null;
                }
            }
            else
            {
                // フォールバック
                GlobalVariables.CurrentState = PetState.moving;
                SetWalkingAnimation(true);
                yield return StartCoroutine(MoveToPositionCoroutine(centerPosition, enterSpeed));
                SetWalkingAnimation(false);
                GlobalVariables.CurrentState = PetState.idle;
            }

            Log("Enter animation completed");
            _isAnimating = false;
            _currentCoroutine = null;
        }

        /// <summary>
        /// シンプルな移動コルーチン
        /// </summary>
        private IEnumerator MoveToPositionCoroutine(Vector3 targetPosition, float speed)
        {
            const float arrivalDistance = 0.3f;

            while (Vector3.Distance(transform.position, targetPosition) > arrivalDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );
                yield return null;
            }

            transform.position = targetPosition;
            Log($"Arrived at target position");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 歩行アニメーションを設定
        /// </summary>
        private void SetWalkingAnimation(bool isWalking)
        {
            if (_animator == null) return;

            _animator.SetBool("WalkBool", isWalking);
            _animator.SetFloat("walk_x", 0f);
            _animator.SetFloat("walk_y", isWalking ? 1f : 0f);
        }

        /// <summary>
        /// アニメーションフラグをリセット
        /// </summary>
        private void ResetAnimationFlags()
        {
            if (_animator == null) return;

            _animator.SetBool("WalkBool", false);
            _animator.SetFloat("walk_x", 0f);
            _animator.SetFloat("walk_y", 0f);
            _animator.SetInteger("TransitionNo", 0);
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                GameLogger.Log(LogCategory.Dog, $"[DogTransferAnimation] {message}");
            }
        }
        #endregion
    }
}
