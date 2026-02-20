using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace TapHouse.MetaverseWalk.UI
{
    /// <summary>
    /// シーン遷移時のフェードイン/アウト演出
    /// CanvasGroupのalphaを制御してフェード効果を実現する
    ///
    /// 使い方:
    ///   1. 空のGameObjectにこのスクリプトとCanvas/CanvasGroupを付ける
    ///   2. 子にフルスクリーンの黒Imageを配置
    ///   3. SceneTransitionManager等からFadeOut/FadeInを呼ぶ
    ///
    /// テスト段階: シーン上にこのコンポーネントが無い場合、遷移は即座に行われる（既存動作を維持）
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SceneFadeOverlay : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private float defaultFadeDuration = 0.5f;

        private CanvasGroup canvasGroup;
        private Canvas canvas;

        public static SceneFadeOverlay Instance { get; private set; }

        /// <summary>
        /// フェード中かどうか
        /// </summary>
        public bool IsFading { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            canvas = GetComponent<Canvas>();

            // 初期状態: 透明、操作を通す
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // 最前面に表示
            if (canvas != null)
            {
                canvas.sortingOrder = 9999;
            }
        }

        /// <summary>
        /// フェードアウト（画面を暗くする）
        /// </summary>
        public async UniTask FadeOut(float? duration = null, CancellationToken ct = default)
        {
            float fadeDuration = duration ?? defaultFadeDuration;
            IsFading = true;

            // フェード中はタッチを無効化
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            canvasGroup.alpha = 1f;
            IsFading = false;
        }

        /// <summary>
        /// フェードイン（画面を明るくする）
        /// </summary>
        public async UniTask FadeIn(float? duration = null, CancellationToken ct = default)
        {
            float fadeDuration = duration ?? defaultFadeDuration;
            IsFading = true;
            canvasGroup.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            IsFading = false;
        }

        /// <summary>
        /// 即座に黒画面にする（フェードなし）
        /// </summary>
        public void SetBlack()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// 即座に透明にする（フェードなし）
        /// </summary>
        public void SetClear()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
