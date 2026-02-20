using UnityEngine;
using System;

/// <summary>
/// アプリの動作モード
/// </summary>
public enum AppMode
{
    /// <summary>たっぷハウス（タブレット用）</summary>
    TapHouse,
    /// <summary>たっぷポケット（スマホ用）</summary>
    TapPocket
}

/// <summary>
/// アプリモードの保存・読み込みを管理
/// </summary>
public static class AppModeManager
{
    /// <summary>
    /// モードを保存
    /// </summary>
    public static void Save(AppMode mode)
    {
        PlayerPrefs.SetString(PrefsKeys.AppMode, mode.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// モードを読み込み（デフォルト: TapHouse）
    /// </summary>
    public static AppMode Load()
    {
        var modeStr = PlayerPrefs.GetString(PrefsKeys.AppMode, AppMode.TapHouse.ToString());
        if (Enum.TryParse<AppMode>(modeStr, out var mode))
        {
            return mode;
        }
        return AppMode.TapHouse;
    }

    /// <summary>
    /// モードが保存されているか確認
    /// </summary>
    public static bool HasSavedMode()
    {
        return PlayerPrefs.HasKey(PrefsKeys.AppMode);
    }

    /// <summary>
    /// 保存されたモードをクリア
    /// </summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(PrefsKeys.AppMode);
        PlayerPrefs.Save();
    }
}
