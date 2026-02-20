using UnityEngine;

/// <summary>
/// プレイヤーのアクション（なでる、餌、おやつなど）の効果を定義
/// これを使えば、新しいアクションも簡単に追加可能
/// </summary>
[System.Serializable]
public class ActionEffect
{
    [Header("Love（愛情度）への影響")]
    public int loveChange = 0;  // プラスで増加、マイナスで減少

    [Header("Demand（要求度）への影響")]
    public int demandChange = 0;  // プラスで増加、マイナスで減少

    [Header("Hunger（空腹度）への影響")]
    public bool feedsHunger = false;  // trueなら空腹度を回復

    [Header("特殊効果")]
    public bool useIdealTimingBonus = false;  // 理想的な時間ボーナスを使うか
    public bool usePersonalityFeedBonus = false;  // 性格の食事ボーナスを使うか
}

/// <summary>
/// プレイヤーのアクション定義（ScriptableObject）
/// 新しいアクションを追加する際は、アセットを作成するだけでOK
/// </summary>
[CreateAssetMenu(fileName = "New Action", menuName = "Dog/Action")]
public class DogActionData : ScriptableObject
{
    [Header("基本情報")]
    public string actionName = "なでる";
    [TextArea(2, 4)]
    public string description = "犬をなでる。愛情が少し上がり、要求度が下がる。";

    [Header("基本効果")]
    public ActionEffect baseEffect;

    [Header("性格による効果倍率の適用")]
    public bool applyPersonalityToLove = true;
    public bool applyPersonalityToDemand = true;

    [Header("愛情レベルによる効果変動")]
    [Tooltip("愛情レベルが高いほど効果が変わるか")]
    public bool scaleWithLoveLevel = false;

    [Tooltip("Love Lowの時の倍率")]
    [Range(0.1f, 2.0f)]
    public float loveLowMultiplier = 1.0f;

    [Tooltip("Love Mediumの時の倍率")]
    [Range(0.1f, 2.0f)]
    public float loveMediumMultiplier = 1.0f;

    [Tooltip("Love Highの時の倍率")]
    [Range(0.1f, 3.0f)]
    public float loveHighMultiplier = 1.0f;

    [Header("UI・演出")]
    public Sprite icon;
    public string soundEffectName;
    public string animationTrigger;
}