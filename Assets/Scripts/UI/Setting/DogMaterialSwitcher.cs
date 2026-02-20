using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

public enum DogCoat { Brown = 0, Black = 1, White = 2 }

public class DogMaterialSwitcher : MonoBehaviour
{
    [Header("差し替える対象")]
    public Renderer targetRenderer;   // SkinnedMeshRenderer でも OK
    [Tooltip("差し替えたいマテリアルスロット index（通常 0）")]
    public int materialIndex = 0;

    [Header("用意済みのマテリアル")]
    public Material brownMat;
    public Material blackMat;
    public Material whiteMat;

    const string PREF_KEY = "Tap.DogCoat";

    public DogCoat Current { get; private set; } = DogCoat.Brown;

    void Start()
    {
        var saved = (DogCoat)PlayerPrefs.GetInt(PREF_KEY, (int)DogCoat.Brown);
        SetCoat(saved, applyNow: true);
    }

    public void SetCoat(DogCoat coat, bool applyNow = true)
    {
        Current = coat;
        if (applyNow) Apply();
    }

    public void Apply()
    {
        if (!targetRenderer) return;

        // 共有参照を維持したいなら sharedMaterials、個体ごとに独立させたいなら materials
        var mats = targetRenderer.sharedMaterials;

        if (materialIndex < 0 || materialIndex >= mats.Length)
        {
            GameLogger.LogWarning(LogCategory.UI, $"[DogMat] materialIndex {materialIndex} out of range on {targetRenderer.name}");
            return;
        }

        mats[materialIndex] = GetMaterial(Current);
        targetRenderer.sharedMaterials = mats;
    }

    public void Save()
    {
        PlayerPrefs.SetInt(PREF_KEY, (int)Current);
        PlayerPrefs.Save();
        GameLogger.Log(LogCategory.UI, $"[DogMat] Saved {Current}");
    }

    Material GetMaterial(DogCoat c)
    {
        switch (c)
        {
            case DogCoat.Brown: return brownMat;
            case DogCoat.Black: return blackMat;
            case DogCoat.White: return whiteMat;
        }
        return null;
    }
}
