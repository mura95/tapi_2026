#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// アニメーションクリップのルートrotationキーフレームを確認するエディタツール
/// </summary>
public class AnimationRootRotationChecker : EditorWindow
{
    private Animator targetAnimator;
    private Vector2 scrollPosition;
    private List<ClipRotationInfo> results = new List<ClipRotationInfo>();

    private class ClipRotationInfo
    {
        public string clipName;
        public bool hasRootRotation;
        public List<string> rotationProperties = new List<string>();
    }

    [MenuItem("Tools/Animation Root Rotation Checker")]
    public static void ShowWindow()
    {
        GetWindow<AnimationRootRotationChecker>("Root Rotation Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("アニメーションのルートRotation確認", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetAnimator = (Animator)EditorGUILayout.ObjectField(
            "Animator", targetAnimator, typeof(Animator), true);

        GUILayout.Space(10);

        if (GUILayout.Button("チェック実行", GUILayout.Height(30)))
        {
            CheckAnimations();
        }

        GUILayout.Space(10);

        if (results.Count > 0)
        {
            DisplayResults();
        }
    }

    private void CheckAnimations()
    {
        results.Clear();

        if (targetAnimator == null)
        {
            Debug.LogError("Animatorを設定してください");
            return;
        }

        var controller = targetAnimator.runtimeAnimatorController;
        if (controller == null)
        {
            Debug.LogError("RuntimeAnimatorControllerがありません");
            return;
        }

        var clips = controller.animationClips;
        Debug.Log($"=== {clips.Length}個のアニメーションクリップをチェック ===");

        foreach (var clip in clips)
        {
            var info = AnalyzeClip(clip);
            results.Add(info);

            if (info.hasRootRotation)
            {
                Debug.LogWarning($"[{clip.name}] ルートRotationあり: {string.Join(", ", info.rotationProperties)}");
            }
            else
            {
                Debug.Log($"[{clip.name}] ルートRotationなし");
            }
        }

        Debug.Log("=== チェック完了 ===");
    }

    private ClipRotationInfo AnalyzeClip(AnimationClip clip)
    {
        var info = new ClipRotationInfo
        {
            clipName = clip.name,
            hasRootRotation = false
        };

        var bindings = AnimationUtility.GetCurveBindings(clip);

        foreach (var binding in bindings)
        {
            // ルート（path が空）のrotation関連プロパティをチェック
            bool isRoot = string.IsNullOrEmpty(binding.path);
            bool isRotation = binding.propertyName.Contains("Rotation") ||
                              binding.propertyName.Contains("localEulerAngles") ||
                              binding.propertyName.Contains("m_LocalRotation");

            if (isRoot && isRotation)
            {
                info.hasRootRotation = true;
                info.rotationProperties.Add(binding.propertyName);
            }
        }

        // ObjectReferenceCurveもチェック
        var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var binding in objectBindings)
        {
            bool isRoot = string.IsNullOrEmpty(binding.path);
            bool isRotation = binding.propertyName.Contains("Rotation");

            if (isRoot && isRotation)
            {
                info.hasRootRotation = true;
                info.rotationProperties.Add($"{binding.propertyName} (ObjectRef)");
            }
        }

        return info;
    }

    private void DisplayResults()
    {
        GUILayout.Label("結果:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var info in results)
        {
            EditorGUILayout.BeginHorizontal("box");

            // 状態アイコン
            if (info.hasRootRotation)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("⚠", GUILayout.Width(20));
            }
            else
            {
                GUI.color = Color.green;
                GUILayout.Label("✓", GUILayout.Width(20));
            }
            GUI.color = Color.white;

            // クリップ名
            GUILayout.Label(info.clipName, GUILayout.Width(200));

            // 詳細
            if (info.hasRootRotation)
            {
                GUILayout.Label(string.Join(", ", info.rotationProperties));
            }
            else
            {
                GUILayout.Label("ルートRotationなし");
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif
