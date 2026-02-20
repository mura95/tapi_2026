using UnityEngine;
using UnityEngine.UI;
using TapHouse.MetaverseWalk.Movement;

namespace TapHouse.MetaverseWalk.UI
{
    /// <summary>
    /// 移動ボタンUI（前進/左/右）
    /// ボタンを押している間、プレイヤーが移動する
    /// </summary>
    public class MovementButtonUI : MonoBehaviour
    {
        [Header("プレイヤーコントローラー")]
        [SerializeField] private MetaversePlayerController playerController;

        [Header("ボタン参照")]
        [SerializeField] private Button forwardButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [Header("ビジュアル設定")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        private void Start()
        {
            Debug.Log($"[MovementButtonUI] forward={forwardButton != null}, left={leftButton != null}, right={rightButton != null}, player={playerController != null}");
            SetupButton(forwardButton, OnForwardPressed, OnForwardReleased);
            SetupButton(leftButton, OnLeftPressed, OnLeftReleased);
            SetupButton(rightButton, OnRightPressed, OnRightReleased);
        }

        private void SetupButton(Button button, System.Action onPressed, System.Action onReleased)
        {
            if (button == null) return;

            var go = button.gameObject;

            // ButtonコンポーネントがPointerイベントを横取りするので無効化
            button.enabled = false;

            // 古いEventTriggerがあれば削除
            var oldTrigger = go.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (oldTrigger != null)
            {
                Destroy(oldTrigger);
            }

            // HoldButtonでPress/Releaseを検知
            HoldButton hold = go.GetComponent<HoldButton>();
            if (hold == null)
            {
                hold = go.AddComponent<HoldButton>();
            }

            hold.OnPressed += () => {
                onPressed?.Invoke();
                SetButtonColor(button, pressedColor);
            };
            hold.OnReleased += () => {
                onReleased?.Invoke();
                SetButtonColor(button, normalColor);
            };
        }

        private void SetButtonColor(Button button, Color color)
        {
            if (button == null) return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        #region Button Callbacks

        private string PlayerPositionLog()
        {
            if (playerController == null) return "player=NULL";
            var p = playerController.transform.position;
            var yaw = playerController.CurrentYaw;
            return $"pos=({p.x:F2}, {p.y:F2}, {p.z:F2}) yaw={yaw:F1}";
        }

        private void OnForwardPressed()
        {
            Debug.Log($"[MovementButtonUI] Forward PRESSED {PlayerPositionLog()}");
            if (playerController != null)
            {
                playerController.StartMoveForward();
            }
        }

        private void OnForwardReleased()
        {
            Debug.Log($"[MovementButtonUI] Forward RELEASED {PlayerPositionLog()}");
            if (playerController != null)
            {
                playerController.StopMoveForward();
            }
        }

        private void OnLeftPressed()
        {
            Debug.Log($"[MovementButtonUI] Left PRESSED {PlayerPositionLog()}");
            if (playerController != null)
            {
                playerController.StartTurnLeft();
            }
        }

        private void OnLeftReleased()
        {
            Debug.Log($"[MovementButtonUI] Left RELEASED {PlayerPositionLog()}");
            if (playerController != null)
            {
                playerController.StopTurn();
            }
        }

        private void OnRightPressed()
        {
            Debug.Log($"[MovementButtonUI] Right PRESSED {PlayerPositionLog()}");
            if (playerController != null)
            {
                playerController.StartTurnRight();
            }
        }

        private void OnRightReleased()
        {
            Debug.Log($"[MovementButtonUI] Right RELEASED {PlayerPositionLog()}");
            if (playerController != null)
            {
                playerController.StopTurn();
            }
        }

        #endregion

        /// <summary>
        /// プレイヤーコントローラーを設定
        /// </summary>
        public void SetPlayerController(MetaversePlayerController controller)
        {
            playerController = controller;
        }
    }
}
