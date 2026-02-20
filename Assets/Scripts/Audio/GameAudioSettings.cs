using UnityEngine;
using TapHouse.Logging;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ゲーム全体の音量設定を管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "GameAudioSettings", menuName = "Settings/Game Audio Settings", order = 1)]
public class GameAudioSettings : ScriptableObject
{
    private static GameAudioSettings _instance;

    public static GameAudioSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameAudioSettings>("GameAudioSettings");

                if (_instance == null)
                {
                    GameLogger.LogError(LogCategory.Audio,"GameAudioSettings.asset が Resources フォルダに見つかりません！");
                }
            }
            return _instance;
        }
    }

    [Header("🔊 音量設定")]
    [Tooltip("鳴き声（Bark）の音量")]
    [SerializeField, Range(0f, 1f)]
    private float barkVolume = 0.5f;

    [Tooltip("アイドルアニメーション中の音の音量")]
    [SerializeField, Range(0f, 1f)]
    private float idleAnimationVolume = 0.5f;

    [Tooltip("座りアニメーション中の音の音量")]
    [SerializeField, Range(0f, 1f)]
    private float sittingAnimationVolume = 0.5f;

    public float BarkVolume => barkVolume;
    public float IdleAnimationVolume => idleAnimationVolume;
    public float SittingAnimationVolume => sittingAnimationVolume;

    public void SetBarkVolume(float volume)
    {
        barkVolume = Mathf.Clamp01(volume);
    }

    public void SetIdleAnimationVolume(float volume)
    {
        idleAnimationVolume = Mathf.Clamp01(volume);
    }

    public void SetSittingAnimationVolume(float volume)
    {
        sittingAnimationVolume = Mathf.Clamp01(volume);
    }

#if UNITY_EDITOR
    /// <summary>
    /// エディタメニューから GameAudioSettings.asset を作成
    /// </summary>
    [MenuItem("Assets/Create/Settings/Game Audio Settings")]
    public static void CreateAudioSettingsAsset()
    {
        GameAudioSettings asset = ScriptableObject.CreateInstance<GameAudioSettings>();

        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(resourcesPath + "/GameAudioSettings.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        GameLogger.Log(LogCategory.Audio,"GameAudioSettings.asset を作成しました: " + assetPath);
    }
#endif
}