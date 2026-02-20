using UnityEngine;
using TapHouse.MetaverseWalk.Core;

namespace TapHouse.MetaverseWalk.UI
{
    /// <summary>
    /// メインシーンに配置する散歩トリガーUI
    /// 散歩時間になるとポップアップを表示し、確認後メタバースシーンへ遷移
    /// </summary>
    public class WalkTriggerUI : MonoBehaviour
    {
        [Header("ポップアップ")]
        [SerializeField] private GameObject walkPopupPrefab;

        [Header("メインUIボタン参照")]
        [SerializeField] private MainUIButtons mainUIButtons;

        [Header("犬の演出")]
        [SerializeField] private Animator dogAnimator;
        [SerializeField] private string walkRequestTrigger = "WalkRequest";

        [Header("音声")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip barkClip;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        private WalkScheduler walkScheduler;
        private GameObject currentPopup;

        private void Start()
        {
            walkScheduler = WalkScheduler.Instance;
            if (walkScheduler == null)
            {
                walkScheduler = FindObjectOfType<WalkScheduler>();
            }

            if (walkScheduler != null)
            {
                walkScheduler.OnStateChanged += OnWalkStateChanged;
                OnWalkStateChanged(walkScheduler.CurrentState);
            }
            else
            {
                Debug.LogWarning("[WalkTriggerUI] WalkScheduler not found");
            }
        }

        private void OnDestroy()
        {
            if (walkScheduler != null)
            {
                walkScheduler.OnStateChanged -= OnWalkStateChanged;
            }
        }

        private void OnWalkStateChanged(WalkRequestState state)
        {
            if (debugMode)
            {
                Debug.Log($"[WalkTriggerUI] State changed to: {state}");
            }

            switch (state)
            {
                case WalkRequestState.Active:
                    PlayWalkRequestAnimation();
                    ShowWalkPopup();
                    break;

                case WalkRequestState.Walking:
                case WalkRequestState.Completed:
                case WalkRequestState.Inactive:
                default:
                    HideWalkPopup();
                    if (mainUIButtons != null)
                    {
                        mainUIButtons.HideWalkButton();
                    }
                    break;
            }
        }

        private void PlayWalkRequestAnimation()
        {
            if (dogAnimator != null)
            {
                dogAnimator.SetTrigger(walkRequestTrigger);
            }

            if (audioSource != null && barkClip != null)
            {
                audioSource.PlayOneShot(barkClip);
            }
        }

        private void ShowWalkPopup()
        {
            // 既にポップアップが表示中なら何もしない
            if (currentPopup != null) return;

            // prefabをロード（SerializeFieldが未設定ならResourcesから）
            GameObject prefab = walkPopupPrefab;
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("UI/walkPopupPrefab");
            }

            if (prefab == null)
            {
                Debug.LogError("[WalkTriggerUI] WalkConfirmPopup prefab not found");
                return;
            }

            // 親なしでInstantiate（prefab自体がCanvasを持つ）
            currentPopup = Instantiate(prefab);

            // Canvas設定を確認・修正
            Canvas popupCanvas = currentPopup.GetComponent<Canvas>();
            if (popupCanvas != null)
            {
                popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                popupCanvas.sortingOrder = 999;
            }

            // コールバック設定
            var popup = currentPopup.GetComponent<WalkConfirmPopup>();
            if (popup != null)
            {
                popup.Initialize(OnWalkConfirmed, OnWalkDeclined);
            }
        }

        private void HideWalkPopup()
        {
            if (currentPopup != null)
            {
                Destroy(currentPopup);
                currentPopup = null;
            }
        }

        private void OnWalkConfirmed()
        {
            if (debugMode)
            {
                Debug.Log("[WalkTriggerUI] Walk confirmed");
            }

            currentPopup = null;

            // 散歩開始
            if (walkScheduler != null)
            {
                walkScheduler.StartWalk();
            }

            // シーン遷移
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToMetaverse();
            }
            else
            {
                Debug.LogError("[WalkTriggerUI] SceneTransitionManager not found");
            }
        }

        private void OnWalkDeclined()
        {
            if (debugMode)
            {
                Debug.Log("[WalkTriggerUI] Walk declined");
            }

            currentPopup = null;

            // メインUIに散歩ボタンを表示
            if (mainUIButtons != null)
            {
                mainUIButtons.ShowWalkButton();
            }
        }

        /// <summary>
        /// 外部から散歩ポップアップを表示（デバッグ用）
        /// </summary>
        public void ForceShowPopup()
        {
            ShowWalkPopup();
        }
    }
}
