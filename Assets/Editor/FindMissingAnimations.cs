using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class FindMissingAnimations : EditorWindow
{
    [MenuItem("Tools/Find Missing Animations")]
    public static void FindMissingClips()
    {
        // AnimatorControllerを検索
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.motion == null) // Motion が Missing の場合
                    {
                        Debug.LogError($"Missing AnimationClip found in {controller.name} at state {state.state.name}");
                    }
                }
            }
        }
    }
}
