using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Unity Editor のコンソール出力を Logs/unity_console.log にリアルタイム書き出しする。
/// Claude Code から tail -f で監視してデバッグに活用する。
/// </summary>
[InitializeOnLoad]
public static class UnityConsoleLogger
{
    private const string LogFileName = "Logs/unity_console.log";
    private static string _logFilePath;
    private static StreamWriter _writer;

    static UnityConsoleLogger()
    {
        _logFilePath = Path.Combine(
            Path.GetDirectoryName(Application.dataPath),
            LogFileName);

        // ログディレクトリが無ければ作成
        var dir = Path.GetDirectoryName(_logFilePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        OpenWriter(append: true);

        Application.logMessageReceived += OnLogMessageReceived;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OpenWriter(bool append)
    {
        CloseWriter();
        _writer = new StreamWriter(_logFilePath, append, System.Text.Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    private static void CloseWriter()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Close();
            _writer.Dispose();
            _writer = null;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Play モード開始時にログファイルをクリア
            OpenWriter(append: false);
            _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] === Play Mode Started ===");
        }
    }

    private static void OnLogMessageReceived(string message, string stackTrace, LogType type)
    {
        if (_writer == null)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var label = type switch
        {
            LogType.Error     => "ERR",
            LogType.Exception => "ERR",
            LogType.Warning   => "WARN",
            LogType.Assert    => "ERR",
            _                 => "LOG",
        };

        try
        {
            // スタックトレースから発信元（クラス名:メソッド名）を抽出
            var source = ExtractSource(stackTrace);
            var sourceTag = string.IsNullOrEmpty(source) ? "" : $" ({source})";

            _writer.WriteLine($"[{timestamp}] [{label}]{sourceTag} {message}");

            // エラー・例外の場合はスタックトレースも出力
            if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                && !string.IsNullOrEmpty(stackTrace))
            {
                _writer.WriteLine(stackTrace);
            }
        }
        catch (ObjectDisposedException)
        {
            // Editor 終了時などで writer が破棄済みの場合は無視
        }
    }

    // スタックトレース例: "DogStateController:StateCheck () (at Assets/Scripts/DogStateController.cs:42)"
    // → "DogStateController:StateCheck" を返す
    private static readonly Regex SourceRegex = new Regex(
        @"^(\w[\w.]*:\w+)",
        RegexOptions.Compiled);

    private static string ExtractSource(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
            return null;

        // 最初の行を取得
        var firstLine = stackTrace;
        var newlineIdx = stackTrace.IndexOf('\n');
        if (newlineIdx > 0)
            firstLine = stackTrace.Substring(0, newlineIdx);

        // UnityEngine/UnityEditor 内部のフレームはスキップして、ユーザーコードを探す
        foreach (var line in stackTrace.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;
            // Unity 内部フレームをスキップ
            if (trimmed.StartsWith("UnityEngine.") || trimmed.StartsWith("UnityEditor."))
                continue;

            var match = SourceRegex.Match(trimmed);
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }
}
