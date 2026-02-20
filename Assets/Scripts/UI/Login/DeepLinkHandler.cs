using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deep Linkを受信してトークン等のパラメータを処理
/// taphouse://auth?token=xxx 形式のURLを解析
/// </summary>
public class DeepLinkHandler : MonoBehaviour
{
    private const string SCHEME = "taphouse";
    private const string HOST = "auth";

    /// <summary>トークン受信時</summary>
    public event Action<string> OnTokenReceived;

    /// <summary>キャンセル時</summary>
    public event Action OnCancelled;

    /// <summary>エラー時</summary>
    public event Action<string> OnError;

    private static DeepLinkHandler _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static DeepLinkHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("DeepLinkHandler");
                _instance = go.AddComponent<DeepLinkHandler>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Application.deepLinkActivated += OnDeepLinkActivated;

        // アプリ起動時のDeep Linkをチェック
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            Application.deepLinkActivated -= OnDeepLinkActivated;
            _instance = null;
        }
    }

    /// <summary>
    /// Deep Link受信時の処理
    /// </summary>
    private void OnDeepLinkActivated(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        try
        {
            var uri = new Uri(url);

            // スキームとホストを検証
            if (!string.Equals(uri.Scheme, SCHEME, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.Equals(uri.Host, HOST, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var query = ParseQuery(uri.Query);

            if (query.TryGetValue("token", out var token) && !string.IsNullOrEmpty(token))
            {
                OnTokenReceived?.Invoke(token);
            }
            else if (query.TryGetValue("cancelled", out _))
            {
                OnCancelled?.Invoke();
            }
            else if (query.TryGetValue("error", out var error))
            {
                Debug.LogWarning($"[DeepLinkHandler] Error: {error}");
                OnError?.Invoke(error);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DeepLinkHandler] Failed to parse URL: {ex.Message}");
            OnError?.Invoke("parse_error");
        }
    }

    /// <summary>
    /// クエリ文字列をパースしてDictionaryに変換
    /// </summary>
    private Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        // 先頭の ? を除去
        if (query.StartsWith("?"))
        {
            query = query.Substring(1);
        }

        var pairs = query.Split('&');
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split(new[] { '=' }, 2);
            if (keyValue.Length == 2)
            {
                var key = Uri.UnescapeDataString(keyValue[0]);
                var value = Uri.UnescapeDataString(keyValue[1]);
                result[key] = value;
            }
            else if (keyValue.Length == 1)
            {
                result[Uri.UnescapeDataString(keyValue[0])] = string.Empty;
            }
        }

        return result;
    }

    /// <summary>
    /// イベントリスナーをクリア
    /// </summary>
    public void ClearListeners()
    {
        OnTokenReceived = null;
        OnCancelled = null;
        OnError = null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// [Editor専用] Deep Linkをシミュレート（トークン受信）
    /// </summary>
    public void SimulateTokenReceived(string token)
    {
        OnTokenReceived?.Invoke(token);
    }

    /// <summary>
    /// [Editor専用] Deep Linkをシミュレート（キャンセル）
    /// </summary>
    public void SimulateCancelled()
    {
        OnCancelled?.Invoke();
    }

    /// <summary>
    /// [Editor専用] Deep Linkをシミュレート（エラー）
    /// </summary>
    public void SimulateError(string error)
    {
        OnError?.Invoke(error);
    }
#endif
}
