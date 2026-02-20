using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System;
using TapHouse.MetaverseWalk.UI;

namespace TapHouse.MetaverseWalk.Core
{
    /// <summary>
    /// メインシーンとメタバースシーン間の遷移を管理
    /// SceneFadeOverlayが存在する場合はフェード演出付き、なければ即遷移（既存動作互換）
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("シーン設定")]
        [SerializeField] private string metaverseSceneName = "Metaverse";

        [Header("遷移設定")]
        [SerializeField] private float transitionDuration = 0.5f;

        [Header("デバッグ")]
        [SerializeField] private bool debugMode = false;

        public static SceneTransitionManager Instance { get; private set; }

        public bool IsTransitioning { get; private set; }

        public event Action OnTransitionStarted;
        public event Action OnTransitionCompleted;

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
            }
        }

        /// <summary>
        /// メタバースシーンへ遷移
        /// </summary>
        public void TransitionToMetaverse()
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[SceneTransition] Already transitioning");
                return;
            }

            TransitionToMetaverseAsync().Forget();
        }

        private async UniTaskVoid TransitionToMetaverseAsync()
        {
            IsTransitioning = true;
            OnTransitionStarted?.Invoke();

            Debug.Log("[SceneTransition] Starting transition to Metaverse");

            try
            {
                // PetStateを散歩中に変更
                GlobalVariables.CurrentState = PetState.walk;

                // フェードアウト（SceneFadeOverlayがあれば使用）
                var fade = SceneFadeOverlay.Instance;
                if (fade != null)
                {
                    await fade.FadeOut(transitionDuration);
                }
                else
                {
                    // フォールバック: 単純な待機
                    await UniTask.Delay(TimeSpan.FromSeconds(transitionDuration));
                }

                // シーン遷移
                await SceneManager.LoadSceneAsync(metaverseSceneName);

                // フェードイン
                if (fade != null)
                {
                    await fade.FadeIn(transitionDuration);
                }

                Debug.Log("[SceneTransition] Transition completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneTransition] Failed: {e.Message}");
                // フェードが中途半端になった場合はクリア
                SceneFadeOverlay.Instance?.SetClear();
            }
            finally
            {
                IsTransitioning = false;
                OnTransitionCompleted?.Invoke();
            }
        }

        /// <summary>
        /// シーンが存在するかチェック
        /// </summary>
        public bool IsMetaverseSceneValid()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneName == metaverseSceneName)
                {
                    return true;
                }
            }
            return false;
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
