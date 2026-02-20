using UnityEngine;

/// <summary>
/// 犬の性格データ（ScriptableObject）
/// 新しい性格を追加する際は、アセットを作成するだけでOK
/// </summary>
[CreateAssetMenu(fileName = "New Dog Personality", menuName = "Dog/Personality")]
public class DogPersonalityData : ScriptableObject
{
    [Header("基本情報")]
    public string personalityName = "人懐っこい";
    [TextArea(3, 5)]
    public string description = "すぐに懐く、初心者向けの性格";

    [Header("Love（愛情度）への影響")]
    [Tooltip("Love上昇の倍率（1.0が標準）")]
    [Range(0.1f, 3.0f)]
    public float loveIncreaseMultiplier = 1.0f;

    [Tooltip("Love減少の倍率（1.0が標準）")]
    [Range(0.1f, 3.0f)]
    public float loveDecreaseMultiplier = 1.0f;

    [Header("Demand（要求度）への影響")]
    [Tooltip("Demand上昇の倍率（1.0が標準）")]
    [Range(0.1f, 3.0f)]
    public float demandIncreaseMultiplier = 1.0f;

    [Tooltip("Demand減少の倍率（1.0が標準）")]
    [Range(0.1f, 3.0f)]
    public float demandDecreaseMultiplier = 1.0f;

    [Header("Hunger（空腹度）への影響")]
    [Tooltip("Hunger進行速度の倍率（1.0が標準）")]
    [Range(0.1f, 3.0f)]
    public float hungerProgressMultiplier = 1.0f;

    [Tooltip("食事によるLove上昇ボーナス（加算値）")]
    [Range(0, 10)]
    public int feedLoveBonus = 0;

    [Header("特殊効果")]
    [Tooltip("この性格の特別な効果の説明")]
    [TextArea(2, 4)]
    public string specialEffect = "";
}