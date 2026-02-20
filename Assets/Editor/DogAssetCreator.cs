using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Unityエディタのメニューに「Create > Dog > Personality/Action」を追加
/// Assets/Editor/ フォルダに配置すること
/// </summary>
#if UNITY_EDITOR
public class DogAssetCreator
{
    /// <summary>
    /// 性格データを作成
    /// </summary>
    [MenuItem("Assets/Create/Dog/Personality", false, 1)]
    public static void CreatePersonality()
    {
        CreateAsset<DogPersonalityData>("New_Personality");
    }
    
    /// <summary>
    /// アクションデータを作成
    /// </summary>
    [MenuItem("Assets/Create/Dog/Action", false, 2)]
    public static void CreateAction()
    {
        CreateAsset<DogActionData>("New_Action");
    }
    
    /// <summary>
    /// すべての基本性格を一括作成
    /// </summary>
    [MenuItem("Assets/Create/Dog/Create All Personalities", false, 20)]
    public static void CreateAllPersonalities()
    {
        string folder = GetSelectedPathOrFallback();
        
        // Friendly
        CreatePersonalityAsset(folder, "Friendly", "人懐っこい", 
            "すぐに懐く、初心者向けの性格", 1.5f, 1.0f, 1.0f, 1.0f, 1.0f, 0);
        
        // Shy
        CreatePersonalityAsset(folder, "Shy", "恥ずかしがり", 
            "時間をかけて信頼を得る必要がある", 0.5f, 1.5f, 1.3f, 0.8f, 1.0f, 0);
        
        // Active
        CreatePersonalityAsset(folder, "Active", "活発", 
            "よく遊びたがる、エネルギッシュな性格", 1.0f, 1.0f, 1.4f, 1.2f, 1.2f, 0);
        
        // Calm
        CreatePersonalityAsset(folder, "Calm", "穏やか", 
            "手がかからない、落ち着いた性格", 1.0f, 0.8f, 0.7f, 1.0f, 0.9f, 0);
        
        // Gluttonous
        CreatePersonalityAsset(folder, "Gluttonous", "食いしん坊", 
            "食べ物が大好き、食事で大喜び", 1.0f, 1.0f, 1.0f, 1.0f, 1.5f, 3);
        
        // Independent
        CreatePersonalityAsset(folder, "Independent", "独立心強い", 
            "ツンデレ、一人でも平気", 0.8f, 0.7f, 0.6f, 1.0f, 1.0f, 0);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ 6種類の性格を作成しました！");
    }
    
    /// <summary>
    /// すべての基本アクションを一括作成
    /// </summary>
    [MenuItem("Assets/Create/Dog/Create All Actions", false, 21)]
    public static void CreateAllActions()
    {
        string folder = GetSelectedPathOrFallback();
        
        // Pet (なでる)
        CreateActionAsset(folder, "Action_Pet", "なでる", 
            "犬をなでる。愛情が少し上がり、要求度が下がる。",
            1, -15, false, false, false,
            true, true, true, 0.5f, 1.0f, 2.0f);
        
        // Feed (ごはん)
        CreateActionAsset(folder, "Action_Feed", "ごはん", 
            "ごはんを与える。空腹度が回復し、愛情と要求度が上がる。",
            2, -20, true, true, true,
            true, true, false, 1.0f, 1.0f, 1.0f);
        
        // Snack (おやつ)
        CreateActionAsset(folder, "Action_Snack", "おやつ", 
            "おやつを与える。愛情と要求度が上がる。",
            2, -15, false, false, true,
            true, true, true, 0.7f, 1.0f, 1.5f);
        
        // Play (遊ぶ)
        CreateActionAsset(folder, "Action_Play", "遊ぶ", 
            "犬と遊ぶ。愛情が上がり、要求度が大きく下がる。",
            3, -30, false, false, false,
            true, true, true, 0.3f, 1.0f, 1.5f);
        
        // Walk (散歩)
        CreateActionAsset(folder, "Action_Walk", "散歩", 
            "犬と散歩に行く。愛情が上がり、要求度が大きく下がる。",
            4, -40, false, false, false,
            true, true, true, 0.5f, 1.0f, 1.3f);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ 5種類のアクションを作成しました！");
    }
    
    /// <summary>
    /// ScriptableObjectアセットを作成（汎用）
    /// </summary>
    private static void CreateAsset<T>(string defaultName) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        
        string path = GetSelectedPathOrFallback();
        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + defaultName + ".asset");
        
        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
    
    /// <summary>
    /// 性格アセットを作成
    /// </summary>
    private static void CreatePersonalityAsset(string folder, string fileName, string name, string desc,
        float loveInc, float loveDec, float demandInc, float demandDec, float hunger, int feedBonus)
    {
        var asset = ScriptableObject.CreateInstance<DogPersonalityData>();
        asset.personalityName = name;
        asset.description = desc;
        asset.loveIncreaseMultiplier = loveInc;
        asset.loveDecreaseMultiplier = loveDec;
        asset.demandIncreaseMultiplier = demandInc;
        asset.demandDecreaseMultiplier = demandDec;
        asset.hungerProgressMultiplier = hunger;
        asset.feedLoveBonus = feedBonus;
        
        string path = folder + "/" + fileName + ".asset";
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
    }
    
    /// <summary>
    /// アクションアセットを作成
    /// </summary>
    private static void CreateActionAsset(string folder, string fileName, string name, string desc,
        int loveChange, int demandChange, bool feedsHunger, bool idealTiming, bool personalityBonus,
        bool applyLove, bool applyDemand, bool scaleWithLove,
        float lowMult, float medMult, float highMult)
    {
        var asset = ScriptableObject.CreateInstance<DogActionData>();
        asset.actionName = name;
        asset.description = desc;
        
        asset.baseEffect = new ActionEffect
        {
            loveChange = loveChange,
            demandChange = demandChange,
            feedsHunger = feedsHunger,
            useIdealTimingBonus = idealTiming,
            usePersonalityFeedBonus = personalityBonus
        };
        
        asset.applyPersonalityToLove = applyLove;
        asset.applyPersonalityToDemand = applyDemand;
        asset.scaleWithLoveLevel = scaleWithLove;
        asset.loveLowMultiplier = lowMult;
        asset.loveMediumMultiplier = medMult;
        asset.loveHighMultiplier = highMult;
        
        string path = folder + "/" + fileName + ".asset";
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
    }
    
    /// <summary>
    /// 選択中のフォルダパスを取得
    /// </summary>
    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
        
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                path = System.IO.Path.GetDirectoryName(path);
                break;
            }
        }
        
        return path;
    }
}
#endif