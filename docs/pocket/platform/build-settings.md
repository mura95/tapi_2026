# Android/iOS ビルド設定

## 1. 概要

たっぷポケットのAndroid/iOSビルド設定を定義する。同一Unityプロジェクトからプラットフォームを切り替えてビルドする。

---

## 2. 共通設定

### 2.1 Player Settings

| 設定 | 値 | 備考 |
|------|-----|------|
| Company Name | TapHouse | 会社名 |
| Product Name | たっぷポケット | アプリ名 |
| Version | 1.0.0 | セマンティックバージョニング |
| Default Icon | pocket_icon.png | アプリアイコン |

### 2.2 Define Symbols

```
TAPHOUSE_POCKET
```

---

## 3. Android設定

### 3.1 Player Settings (Android)

| 設定 | 値 | 備考 |
|------|-----|------|
| Package Name | com.taphouse.pocket | 一意のパッケージ名 |
| Minimum API Level | 26 (Android 8.0) | 歩数計API要件 |
| Target API Level | 34 (Android 14) | 最新APIレベル |
| Install Location | Automatic | 自動選択 |
| Internet Access | Require | Firebase必須 |
| Write Permission | External (SDCard) | ストレージアクセス |

### 3.2 Scripting Backend

| 設定 | 値 |
|------|-----|
| Scripting Backend | IL2CPP |
| Api Compatibility Level | .NET Standard 2.1 |
| Target Architectures | ARM64 + ARMv7 |

### 3.3 Keystore設定

```
Keystore:
├── Path: user.keystore
├── Password: ********
├── Alias: taphouse_pocket
└── Alias Password: ********
```

**注意:** keystoreファイルはGitにコミットしない

### 3.4 AndroidManifest.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <!-- 基本権限 -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <!-- 歩数計（Google Fit） -->
    <uses-permission android:name="android.permission.ACTIVITY_RECOGNITION" />
    <uses-permission android:name="com.google.android.gms.permission.ACTIVITY_RECOGNITION" />

    <!-- マイク（音声通話） -->
    <uses-permission android:name="android.permission.RECORD_AUDIO" />

    <!-- 加速度センサー -->
    <uses-feature android:name="android.hardware.sensor.accelerometer" android:required="false" />

    <application
        android:allowBackup="true"
        android:icon="@mipmap/app_icon"
        android:label="@string/app_name"
        android:usesCleartextTraffic="true">

        <!-- Google Sign-In -->
        <meta-data
            android:name="com.google.android.gms.version"
            android:value="@integer/google_play_services_version" />

        <!-- Firebase -->
        <!-- google-services.jsonから自動生成 -->

    </application>

</manifest>
```

### 3.5 Google Fit設定

1. **Google Cloud Console**
   - OAuth 2.0 クライアントID作成
   - Fitness API有効化

2. **google-services.json**
   - Firebase ConsoleからダウンロードAssets/に配置

3. **SHA1フィンガープリント**
   ```bash
   keytool -list -v -keystore user.keystore -alias taphouse_pocket
   ```
   - Firebase Console → Project Settings → Your apps → Add fingerprint

---

## 4. iOS設定

### 4.1 Player Settings (iOS)

| 設定 | 値 | 備考 |
|------|-----|------|
| Bundle Identifier | com.taphouse.pocket | 一意のバンドルID |
| Target minimum iOS Version | 14.0 | HealthKit要件 |
| Target SDK | Device SDK | 実機向け |
| Scripting Backend | IL2CPP | 必須 |
| Architecture | ARM64 | 64bit必須 |

### 4.2 Capability設定

| Capability | 用途 |
|------------|------|
| HealthKit | 歩数計 |
| Push Notifications | プッシュ通知 |
| Sign in with Apple | Apple Sign-In（オプション） |
| Background Modes | バックグラウンド処理 |

### 4.3 Info.plist

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- HealthKit -->
    <key>NSHealthShareUsageDescription</key>
    <string>歩数を表示するために、ヘルスケアの歩数データにアクセスします。</string>
    <key>NSHealthUpdateUsageDescription</key>
    <string>歩数を記録するために、ヘルスケアの歩数データにアクセスします。</string>

    <!-- マイク -->
    <key>NSMicrophoneUsageDescription</key>
    <string>メタバース内で他のユーザーと通話するためにマイクを使用します。</string>

    <!-- カメラ（将来用） -->
    <key>NSCameraUsageDescription</key>
    <string>プロフィール写真を撮影するためにカメラを使用します。</string>

    <!-- 位置情報（オプション） -->
    <key>NSLocationWhenInUseUsageDescription</key>
    <string>近くのユーザーを表示するために位置情報を使用します。</string>

    <!-- Background Modes -->
    <key>UIBackgroundModes</key>
    <array>
        <string>fetch</string>
        <string>remote-notification</string>
    </array>

    <!-- HealthKit Capability -->
    <key>com.apple.developer.healthkit</key>
    <true/>
    <key>com.apple.developer.healthkit.access</key>
    <array>
        <string>health-records</string>
    </array>
</dict>
</plist>
```

### 4.4 Xcodeプロジェクト設定

ビルド後のXcodeプロジェクトで追加設定が必要：

1. **Signing & Capabilities**
   - Team: 開発者チーム選択
   - Bundle Identifier確認
   - Provisioning Profile選択

2. **HealthKit追加**
   - + Capability → HealthKit
   - Clinical Health Records: OFF

3. **Push Notifications追加**
   - + Capability → Push Notifications

---

## 5. Firebase設定

### 5.1 共通設定

両プラットフォームで同じFirebaseプロジェクトを使用。

### 5.2 Android

```
Assets/
├── google-services.json          # Firebase設定ファイル
└── Plugins/Android/
    └── FirebaseApp.androidlib/   # Firebase SDK
```

### 5.3 iOS

```
Assets/
└── GoogleService-Info.plist      # Firebase設定ファイル

# ビルド後、XcodeプロジェクトのルートにGoogleService-Info.plistをコピー
```

---

## 6. ビルド手順

### 6.1 Android

```
1. Edit → Project Settings → Player → Android
2. Package Name確認: com.taphouse.pocket
3. Keystore設定確認
4. Edit → Project Settings → Player → Other Settings
   → Scripting Define Symbols: TAPHOUSE_POCKET
5. File → Build Settings
6. Platform: Android選択
7. Switch Platform（必要な場合）
8. Build Settings:
   - Build App Bundle: ON（Google Play向け）
   - Split APKs by target architecture: ON
9. Build または Build And Run
```

### 6.2 iOS

```
1. Edit → Project Settings → Player → iOS
2. Bundle Identifier確認: com.taphouse.pocket
3. Edit → Project Settings → Player → Other Settings
   → Scripting Define Symbols: TAPHOUSE_POCKET
4. File → Build Settings
5. Platform: iOS選択
6. Switch Platform（必要な場合）
7. Build
8. Xcodeでプロジェクトを開く
9. Signing & Capabilities設定
10. Archive → Distribute App
```

---

## 7. ビルドスクリプト

### 7.1 エディタスクリプト

```csharp
using UnityEditor;
using UnityEditor.Build;
using System.IO;

public static class PocketBuildScript
{
    private const string POCKET_SYMBOL = "TAPHOUSE_POCKET";

    [MenuItem("Build/Build Pocket Android")]
    public static void BuildPocketAndroid()
    {
        // Define Symbolsを設定
        PlayerSettings.SetScriptingDefineSymbols(
            NamedBuildTarget.Android,
            POCKET_SYMBOL
        );

        // ビルド設定
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetPocketScenes(),
            locationPathName = "Builds/TapHousePocket.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        // ビルド実行
        BuildPipeline.BuildPlayer(options);
    }

    [MenuItem("Build/Build Pocket iOS")]
    public static void BuildPocketIOS()
    {
        // Define Symbolsを設定
        PlayerSettings.SetScriptingDefineSymbols(
            NamedBuildTarget.iOS,
            POCKET_SYMBOL
        );

        // ビルド設定
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = GetPocketScenes(),
            locationPathName = "Builds/TapHousePocket_iOS",
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        // ビルド実行
        BuildPipeline.BuildPlayer(options);
    }

    private static string[] GetPocketScenes()
    {
        return new string[]
        {
            "Assets/Scenes/Common/Splash.unity",
            "Assets/Scenes/Pocket/PocketMain.unity",
            "Assets/Scenes/Pocket/DressUp.unity",
            "Assets/Scenes/Pocket/Pedometer.unity",
            "Assets/Scenes/Pocket/BrainTraining.unity",
            "Assets/Scenes/Metaverse/Metaverse.unity"
        };
    }
}
```

---

## 8. CI/CD設定

### 8.1 GitHub Actions（Android）

```yaml
name: Build Android

on:
  push:
    branches: [main]
    paths:
      - 'Assets/**'
      - 'ProjectSettings/**'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Unity
        uses: game-ci/unity-builder@v2
        with:
          targetPlatform: Android
          buildMethod: PocketBuildScript.BuildPocketAndroid

      - name: Upload APK
        uses: actions/upload-artifact@v3
        with:
          name: TapHousePocket.apk
          path: Builds/TapHousePocket.apk
```

### 8.2 GitHub Actions（iOS）

```yaml
name: Build iOS

on:
  push:
    branches: [main]
    paths:
      - 'Assets/**'
      - 'ProjectSettings/**'

jobs:
  build:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup Unity
        uses: game-ci/unity-builder@v2
        with:
          targetPlatform: iOS
          buildMethod: PocketBuildScript.BuildPocketIOS

      - name: Build Xcode Project
        run: |
          xcodebuild -project Builds/TapHousePocket_iOS/Unity-iPhone.xcodeproj \
                     -scheme Unity-iPhone \
                     -archivePath Builds/TapHousePocket.xcarchive \
                     archive
```

---

## 9. トラブルシューティング

### 9.1 Android

| 問題 | 解決策 |
|------|--------|
| ビルドエラー: keystore | keystoreパスとパスワードを確認 |
| Google Fit接続失敗 | SHA1フィンガープリントを確認 |
| Firebase初期化エラー | google-services.jsonを確認 |
| 64bit要件エラー | ARM64アーキテクチャを有効化 |

### 9.2 iOS

| 問題 | 解決策 |
|------|--------|
| Signing失敗 | Provisioning Profileを確認 |
| HealthKitエラー | Capabilityを追加 |
| Firebase初期化エラー | GoogleService-Info.plistを確認 |
| bitcode エラー | Build Settings → Enable Bitcode: NO |

---

## 10. リリースチェックリスト

### Android

- [ ] Package Name確認
- [ ] Version Code/Name更新
- [ ] Keystoreファイル確認
- [ ] Proguard設定（難読化）
- [ ] google-services.json最新版
- [ ] SHA1フィンガープリント登録
- [ ] App Bundle形式でビルド
- [ ] Google Play Console設定

### iOS

- [ ] Bundle Identifier確認
- [ ] Version/Build更新
- [ ] Provisioning Profile有効期限
- [ ] Capabilities設定
- [ ] GoogleService-Info.plist最新版
- [ ] App Store Connect設定
- [ ] TestFlight配布テスト

---

## 11. 関連ドキュメント

| ドキュメント | 説明 |
|-------------|------|
| [../build-configuration.md](../build-configuration.md) | Define Symbolsによるビルド分け |
| [../pocket-overview.md](../pocket-overview.md) | たっぷポケット全体概要 |
