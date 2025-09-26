# UxPlay Launcher for Windows

Windows用のUxPlayランチャーアプリケーションです。UxPlayのビルドから実行までを統合的に管理できます。

## 機能

### 🚀 主要機能
- **統合ビルド**: UxPlayのソースコードから自動ビルド
- **最新版対応**: 最新のUxPlayソースコードを取得してビルド
- **豊富な設定**: UxPlayの全フラグをGUIで設定可能
- **リアルタイムログ**: ビルドと実行のログをリアルタイム表示
- **自動パス検出**: uxplay.exeの自動検出とパス設定

### 🎛️ 設定オプション

#### 基本設定
- **解像度**: 1280x720, 1920x1080, 2560x1440, 3840x2160
- **FPS**: 30fps, 60fps
- **デバイス名**: AirPlayデバイスとして表示される名前

#### オプション設定
- **-hls**: YouTube等のAirPlay動画向け
- **-async**: 音質優先/遅延大
- **-vsync no**: 低遅延寄り
- **音声OFF**: 音声を無効化
- **-d**: デバッグモード
- **-v**: 詳細ログ
- **-m**: ミラーリングモード
- **-a2**: AirPlay 2対応
- **-r**: RAOP対応

#### セキュリティ設定
- **パスワード**: 接続時のパスワード保護
- **基底ポート**: 使用するポート番号

#### 高度な設定
- **VideoSink**: 動画出力設定
- **AudioSink**: 音声出力設定
- **音声遅延**: 音声遅延の調整
- **カスタム引数**: 追加のコマンドライン引数

## システム要件

### 必須要件
- **Windows 10/11** (x64)
- **.NET 8.0 Runtime**
- **MSYS2** (C:/msys64 にインストール)

### MSYS2 パッケージ
以下のパッケージが自動的にインストールされます：
- mingw-w64-x86_64-gcc
- mingw-w64-x86_64-cmake
- mingw-w64-x86_64-pkgconf
- mingw-w64-x86_64-openssl
- mingw-w64-x86_64-libplist
- mingw-w64-x86_64-gstreamer
- mingw-w64-x86_64-gst-plugins-base
- mingw-w64-x86_64-gst-plugins-good
- mingw-w64-x86_64-gst-plugins-bad
- mingw-w64-x86_64-gst-libav

## インストール

1. **MSYS2のインストール**
   ```bash
   # MSYS2を https://www.msys2.org/ からダウンロードしてインストール
   # C:/msys64 にインストールすることを推奨
   ```

2. **アプリケーションの実行**
   ```bash
   # リリース版をダウンロードして実行
   UxplayLauncher.exe
   ```

## 使用方法

### 初回セットアップ
1. アプリケーションを起動
2. 「自動ビルドを有効にする」にチェック
3. 「UxPlay をビルド」ボタンをクリック
4. ビルド完了後、「🚀 起動」ボタンでUxPlayを開始

### 日常的な使用
1. 必要な設定を調整
2. 「🚀 起動」ボタンでUxPlayを開始
3. iPhone/iPadからAirPlayで接続
4. 「⏹ 停止」ボタンでUxPlayを停止

### 最新版への更新
1. 「最新版に更新してビルド」にチェック
2. 「最新版をビルド」ボタンをクリック
3. 最新のUxPlayソースコードでビルド

## トラブルシューティング

### ビルドエラー
- MSYS2が正しくインストールされているか確認
- インターネット接続を確認（依存パッケージのダウンロード）
- ウイルス対策ソフトがビルドプロセスをブロックしていないか確認

### 実行エラー
- uxplay.exeのパスが正しく設定されているか確認
- GStreamerプラグインが正しくインストールされているか確認
- ファイアウォールがUxPlayの通信をブロックしていないか確認

### 接続できない
- 同じネットワークに接続されているか確認
- ポートが他のアプリケーションで使用されていないか確認
- パスワード設定を確認

## 開発者向け情報

### プロジェクト構造
```
UxplayLauncher/
├── UxplayLauncher/
│   ├── MainWindow.xaml          # メインUI
│   ├── MainWindow.xaml.cs       # メインロジック
│   ├── Models/
│   │   └── AppSettings.cs       # 設定モデル
│   ├── Services/
│   │   ├── UxplayProcess.cs     # プロセス管理
│   │   └── UxplayBuildService.cs # ビルドサービス
│   └── scripts/
│       ├── build-uxplay-mingw64.sh    # ビルドスクリプト
│       └── invoke-msys2-build.ps1     # PowerShellラッパー
└── third_party/
    └── UxPlay/                  # UxPlayソースコード
```

### ビルド方法
```bash
# 開発環境でのビルド
dotnet build

# リリース版のビルド
dotnet publish -c Release
```

## ライセンス

このプロジェクトはMITライセンスの下で公開されています。

## 貢献

バグ報告や機能要望は、GitHubのIssuesでお知らせください。

## 更新履歴

### v1.0.0
- 初回リリース
- UxPlayの統合ビルド機能
- 豊富な設定オプション
- リアルタイムログ表示
- 自動パス検出機能
