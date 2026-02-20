using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using TapHouse.Logging;

/// <summary>
/// 暗号化されたPlayerPrefsラッパー
/// パスワードなどの機密情報を安全に保存するために使用
///
/// 使用方法:
///   SecurePlayerPrefs.SetString("Password", "secret123");
///   string password = SecurePlayerPrefs.GetString("Password");
///   SecurePlayerPrefs.DeleteKey("Password");
/// </summary>
public static class SecurePlayerPrefs
{
    // 暗号化キーのプレフィックス（デバイス固有IDと組み合わせる）
    private const string KEY_PREFIX = "TapHouse_Secure_";

    // PlayerPrefsに保存する際のキーサフィックス
    private const string ENCRYPTED_SUFFIX = "_encrypted";

    // AES暗号化設定
    private const int KEY_SIZE = 256;
    private const int BLOCK_SIZE = 128;

    /// <summary>
    /// 暗号化された文字列を保存
    /// </summary>
    public static void SetString(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            GameLogger.LogError(LogCategory.General,"[SecurePlayerPrefs] Key cannot be null or empty");
            return;
        }

        if (string.IsNullOrEmpty(value))
        {
            DeleteKey(key);
            return;
        }

        try
        {
            string encrypted = Encrypt(value);
            string storageKey = GetStorageKey(key);
            PlayerPrefs.SetString(storageKey, encrypted);
            PlayerPrefs.Save();
            GameLogger.Log(LogCategory.General,$"[SecurePlayerPrefs] Saved encrypted value for key: {key}");
        }
        catch (Exception e)
        {
            GameLogger.LogError(LogCategory.General,$"[SecurePlayerPrefs] Failed to encrypt and save: {e.Message}");
        }
    }

    /// <summary>
    /// 暗号化された文字列を取得
    /// </summary>
    public static string GetString(string key, string defaultValue = "")
    {
        if (string.IsNullOrEmpty(key))
        {
            GameLogger.LogError(LogCategory.General,"[SecurePlayerPrefs] Key cannot be null or empty");
            return defaultValue;
        }

        string storageKey = GetStorageKey(key);
        if (!PlayerPrefs.HasKey(storageKey))
        {
            return defaultValue;
        }

        try
        {
            string encrypted = PlayerPrefs.GetString(storageKey);
            if (string.IsNullOrEmpty(encrypted))
            {
                return defaultValue;
            }

            return Decrypt(encrypted);
        }
        catch (Exception e)
        {
            GameLogger.LogError(LogCategory.General,$"[SecurePlayerPrefs] Failed to decrypt: {e.Message}");
            // 復号に失敗した場合はデータを削除（破損の可能性）
            DeleteKey(key);
            return defaultValue;
        }
    }

    /// <summary>
    /// キーが存在するかチェック
    /// </summary>
    public static bool HasKey(string key)
    {
        string storageKey = GetStorageKey(key);
        return PlayerPrefs.HasKey(storageKey);
    }

    /// <summary>
    /// キーを削除
    /// </summary>
    public static void DeleteKey(string key)
    {
        string storageKey = GetStorageKey(key);
        PlayerPrefs.DeleteKey(storageKey);
        PlayerPrefs.Save();
        GameLogger.Log(LogCategory.General,$"[SecurePlayerPrefs] Deleted key: {key}");
    }

    /// <summary>
    /// 古い平文パスワードを暗号化形式に移行
    /// アプリ起動時に一度だけ呼び出す
    /// </summary>
    public static void MigratePlaintextPassword()
    {
        const string OLD_PASSWORD_KEY = "Password";

        // 古い平文パスワードが存在するかチェック
        if (!PlayerPrefs.HasKey(OLD_PASSWORD_KEY))
        {
            return;
        }

        // 既に暗号化済みかチェック
        if (HasKey(OLD_PASSWORD_KEY))
        {
            // 暗号化済みなら古いキーを削除
            PlayerPrefs.DeleteKey(OLD_PASSWORD_KEY);
            PlayerPrefs.Save();
            GameLogger.Log(LogCategory.General,"[SecurePlayerPrefs] Old plaintext password key removed (already migrated)");
            return;
        }

        // 平文パスワードを取得
        string plaintextPassword = PlayerPrefs.GetString(OLD_PASSWORD_KEY);
        if (string.IsNullOrEmpty(plaintextPassword))
        {
            PlayerPrefs.DeleteKey(OLD_PASSWORD_KEY);
            PlayerPrefs.Save();
            return;
        }

        // 暗号化して保存
        SetString(OLD_PASSWORD_KEY, plaintextPassword);

        // 古い平文キーを削除
        PlayerPrefs.DeleteKey(OLD_PASSWORD_KEY);
        PlayerPrefs.Save();

        GameLogger.Log(LogCategory.General,"[SecurePlayerPrefs] Successfully migrated plaintext password to encrypted storage");
    }

    /// <summary>
    /// ストレージ用のキーを生成
    /// </summary>
    private static string GetStorageKey(string key)
    {
        return key + ENCRYPTED_SUFFIX;
    }

    /// <summary>
    /// 暗号化キーを生成（デバイス固有）
    /// </summary>
    private static byte[] GetEncryptionKey()
    {
        // デバイス固有の識別子を使用
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        string keySource = KEY_PREFIX + deviceId;

        // SHA256でハッシュ化して32バイト（256ビット）のキーを生成
        using (SHA256 sha256 = SHA256.Create())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
        }
    }

    /// <summary>
    /// 初期化ベクトル（IV）を生成
    /// </summary>
    private static byte[] GenerateIV()
    {
        byte[] iv = new byte[BLOCK_SIZE / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }
        return iv;
    }

    /// <summary>
    /// 文字列を暗号化
    /// </summary>
    private static string Encrypt(string plainText)
    {
        byte[] key = GetEncryptionKey();
        byte[] iv = GenerateIV();

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // IVと暗号文を結合（IV + EncryptedData）
            byte[] result = new byte[iv.Length + encryptedBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }
    }

    /// <summary>
    /// 文字列を復号
    /// </summary>
    private static string Decrypt(string encryptedText)
    {
        byte[] key = GetEncryptionKey();
        byte[] fullCipher = Convert.FromBase64String(encryptedText);

        // IVと暗号文を分離
        byte[] iv = new byte[BLOCK_SIZE / 8];
        byte[] cipher = new byte[fullCipher.Length - iv.Length];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
