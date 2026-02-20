using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapHouse.MetaverseWalk.UI
{
    /// <summary>
    /// 長押し対応ボタン
    /// IPointerDownHandler/IPointerUpHandlerで確実にPress/Releaseを検知
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event System.Action OnPressed;
        public event System.Action OnReleased;

        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            OnPressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
            OnReleased?.Invoke();
        }
    }
}
