# リポジトリ構成

```
UxplayLauncher.WPF/
├─ src/
│  └─ UxplayLauncher/
│     ├─ UxplayLauncher.csproj
│     ├─ App.xaml
│     ├─ App.xaml.cs
│     ├─ MainWindow.xaml
│     ├─ MainWindow.xaml.cs
│     ├─ Models/
│     │  └─ AppSettings.cs
│     ├─ Services/
│     │  └─ UxplayProcess.cs
│     ├─ Properties/
│     │  ├─ Resources.resx
│     │  └─ Settings.settings
│     ├─ app.manifest
│     └─ README.local.md
├─ .editorconfig
├─ .gitattributes
├─ .gitignore
├─ LICENSE (後でGitHub上で GPL-3.0-only を選択して本文を自動生成)
└─ .github/
   └─ workflows/
      └─ build.yml
```

---

## src/UxplayLauncher/UxplayLauncher.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>UxplayLauncher</AssemblyName>
    <RootNamespace>UxplayLauncher</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <ApplicationIcon></ApplicationIcon>
    <Platforms>x64</Platforms>
    <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
    <!-- 自己完結配布用設定（CIで上書き可能） -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <DebugType>portable</DebugType>
  </PropertyGroup>
</Project>
```

---

## src/UxplayLauncher/App.xaml
```xml
<Application x:Class="UxplayLauncher.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
  <Application.Resources>
  </Application.Resources>
</Application>
```

## src/UxplayLauncher/App.xaml.cs
```csharp
using System.Windows;

namespace UxplayLauncher;

public partial class App : Application { }
```

---

## src/UxplayLauncher/MainWindow.xaml
```xml
<Window x:Class="UxplayLauncher.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Uxplay Launcher" Height="520" Width="780">
  <Grid Margin="12" ColumnDefinitions="Auto,*" RowDefinitions="* ,Auto">
    <StackPanel Width="290" Margin="0,0,12,0">
      <TextBlock Text="uxplay.exe パス" FontWeight="Bold"/>
      <DockPanel>
        <TextBox x:Name="UxplayPathBox" MinWidth="220" Text=""/>
        <Button Content="参照" Margin="6,0,0,0" Click="BrowseUxplay_Click"/>
      </DockPanel>

      <Separator Margin="0,10,0,10"/>

      <TextBlock Text="解像度 (例: 1280x720)" FontWeight="Bold"/>
      <DockPanel>
        <ComboBox x:Name="ResolutionBox" MinWidth="220" IsEditable="True">
          <ComboBoxItem Content="1280x720" IsSelected="True"/>
          <ComboBoxItem Content="1920x1080"/>
          <ComboBoxItem Content="2560x1440"/>
        </ComboBox>
      </DockPanel>

      <TextBlock Text="FPS" FontWeight="Bold" Margin="0,8,0,0"/>
      <ComboBox x:Name="FpsBox" MinWidth="120">
        <ComboBoxItem Content="30" IsSelected="True"/>
        <ComboBoxItem Content="60"/>
      </ComboBox>

      <CheckBox x:Name="HlsCheck" Content="-hls (YouTube等のAirPlay動画向け)" Margin="0,8,0,0"/>
      <CheckBox x:Name="AsyncAudioCheck" Content="-async (音質優先/遅延大)"/>
      <CheckBox x:Name="NoVsyncCheck" Content="-vsync no (低遅延寄り)"/>
      <CheckBox x:Name="AudioOffCheck" Content="音声OFF（-as 0）"/>

      <TextBlock Text="パスワード (-pw)" FontWeight="Bold" Margin="0,8,0,0"/>
      <PasswordBox x:Name="PasswordBox"/>

      <TextBlock Text="基底ポート (-p)" FontWeight="Bold" Margin="0,8,0,0"/>
      <TextBox x:Name="BasePortBox" MinWidth="120"/>

      <TextBlock Text="VideoSink (-vs) 例: d3d11videosink fullscreen-toggle-mode=alt-enter" FontWeight="Bold" TextWrapping="Wrap" Margin="0,8,0,0"/>
      <TextBox x:Name="VideoSinkBox" MinWidth="220" Text="d3d11videosink fullscreen-toggle-mode=alt-enter"/>

      <StackPanel Orientation="Horizontal" Margin="0,12,0,0">
        <Button x:Name="StartBtn" Content="起動" Width="100" Click="StartBtn_Click"/>
        <Button x:Name="StopBtn" Content="停止" Width="100" Margin="8,0,0,0" Click="StopBtn_Click"/>
      </StackPanel>
    </StackPanel>

    <GroupBox Header="ログ" Grid.Column="1">
      <ScrollViewer VerticalScrollBarVisibility="Auto">
        <TextBox x:Name="LogBox" TextWrapping="Wrap" AcceptsReturn="True" IsReadOnly="True" VerticalScrollBarVisibility="Auto"/>
      </ScrollViewer>
    </GroupBox>

    <StatusBar Grid.Row="1" Grid.ColumnSpan="2">
      <StatusBarItem>
        <TextBlock x:Name="StatusText" Text="停止中"/>
      </StatusBarItem>
    </StatusBar>
  </Grid>
</Window>
```

## src/UxplayLauncher/MainWindow.xaml.cs
```csharp
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using UxplayLauncher.Models;
using UxplayLauncher.Services;

namespace UxplayLauncher;

public partial class MainWindow : Window
{
    private readonly UxplayProcess _proc = new();
    private AppSettings _settings = new();

    public MainWindow()
    {
        InitializeComponent();
        // 既定値
        ResolutionBox.Text = "1280x720"; // 規定: HD
        FpsBox.SelectedIndex = 0; // 30fps
        VideoSinkBox.Text = "d3d11videosink fullscreen-toggle-mode=alt-enter";

        _proc.OutputReceived += (_, s) => AppendLog(s);
        _proc.ErrorReceived  += (_, s) => AppendLog(s);
        _proc.Exited += (_, __) => Dispatcher.Invoke(() => StatusText.Text = "停止中");
    }

    private void BrowseUxplay_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog {
            Filter = "uxplay.exe|uxplay.exe|Executable|*.exe|All files|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            UxplayPathBox.Text = dlg.FileName;
        }
    }

    private string BuildArgs(AppSettings s)
    {
        var a = new List<string>();
        if (!string.IsNullOrWhiteSpace(s.Resolution)) a.Add($"-s {s.Resolution}");
        if (s.Fps > 0) a.Add($"-fps {s.Fps}");
        if (s.EnableHls) a.Add("-hls");
        if (s.AsyncAudio) a.Add("-async");
        if (s.NoVSync) a.Add("-vsync no");
        if (s.AudioOff) a.Add("-as 0");
        if (!string.IsNullOrEmpty(s.Password)) a.Add($"-pw \"{s.Password}\"");
        if (s.BasePort.HasValue) a.Add($"-p {s.BasePort.Value}");
        if (!string.IsNullOrWhiteSpace(s.VideoSink)) a.Add($"-vs \"{s.VideoSink}\"");
        return string.Join(" ", a);
    }

    private void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        _settings = new AppSettings
        {
            UxplayPath = UxplayPathBox.Text,
            Resolution = (ResolutionBox.Text ?? string.Empty).Trim(),
            Fps = int.TryParse((FpsBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString(), out var fps) ? fps : 30,
            EnableHls = HlsCheck.IsChecked == true,
            AsyncAudio = AsyncAudioCheck.IsChecked == true,
            NoVSync = NoVsyncCheck.IsChecked == true,
            AudioOff = AudioOffCheck.IsChecked == true,
            Password = PasswordBox.Password,
            BasePort = int.TryParse(BasePortBox.Text, out var p) ? p : null,
            VideoSink = VideoSinkBox.Text
        };

        if (!File.Exists(_settings.UxplayPath))
        {
            MessageBox.Show("uxplay.exe のパスが不正です。","Uxplay Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var args = BuildArgs(_settings);

        // 必要に応じてGStreamerのパスを調整
        var extraEnv = new Dictionary<string, string?> {
            // 例: MSYS2の既定パス。環境に応じてUIで可変にしても良い
            {"GST_PLUGIN_SYSTEM_PATH_1_0", @"C:\\msys64\\mingw64\\lib\\gstreamer-1.0"}
        };

        AppendLog($"> {Path.GetFileName(_settings.UxplayPath)} {args}");
        try
        {
            _proc.Start(_settings.UxplayPath, args, Path.GetDirectoryName(_settings.UxplayPath)!, extraEnv);
            StatusText.Text = "実行中";
        }
        catch (Exception ex)
        {
            AppendLog(ex.ToString());
            StatusText.Text = "停止中";
        }
    }

    private void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        _proc.Stop();
        StatusText.Text = "停止中";
    }

    private void AppendLog(string? line)
    {
        if (string.IsNullOrEmpty(line)) return;
        Dispatcher.Invoke(() => {
            LogBox.AppendText(line + Environment.NewLine);
            LogBox.ScrollToEnd();
        });
    }
}
```

---

## src/UxplayLauncher/Models/AppSettings.cs
```csharp
namespace UxplayLauncher.Models;

public class AppSettings
{
    public string UxplayPath { get; set; } = string.Empty;
    public string Resolution { get; set; } = "1280x720"; // 既定
    public int Fps { get; set; } = 30;
    public bool EnableHls { get; set; }
    public bool AsyncAudio { get; set; }
    public bool NoVSync { get; set; }
    public bool AudioOff { get; set; }
    public string? Password { get; set; }
    public int? BasePort { get; set; }
    public string? VideoSink { get; set; }
}
```

---

## src/UxplayLauncher/Services/UxplayProcess.cs
```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace UxplayLauncher.Services;

public class UxplayProcess
{
    private Process? _proc;

    public event EventHandler<string?>? OutputReceived;
    public event EventHandler<string?>? ErrorReceived;
    public event EventHandler? Exited;

    public bool IsRunning => _proc is { HasExited: false };

    public void Start(string exePath, string args, string workingDir, IDictionary<string, string?>? extraEnv = null)
    {
        Stop();

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (extraEnv != null)
        {
            foreach (var kv in extraEnv)
                psi.Environment[kv.Key] = kv.Value;
        }

        _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _proc.OutputDataReceived += (_, e) => OutputReceived?.Invoke(this, e.Data);
        _proc.ErrorDataReceived  += (_, e) => ErrorReceived?.Invoke(this, e.Data);
        _proc.Exited += (_, __) => Exited?.Invoke(this, EventArgs.Empty);

        _proc.Start();
        _proc.BeginOutputReadLine();
        _proc.BeginErrorReadLine();
    }

    public void Stop()
    {
        if (_proc == null) return;
        try
        {
            if (!_proc.HasExited)
            {
                _proc.Kill(true);
                _proc.WaitForExit(2000);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _proc.Dispose();
            _proc = null;
        }
    }
}
```

---

## src/UxplayLauncher/app.manifest
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <assemblyIdentity version="1.0.0.0" name="UxplayLauncher.app" />
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <requestedExecutionLevel level="asInvoker" uiAccess="false" />
      </requestedPrivileges>
    </security>
  </trustInfo>
  <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
    <application>
      <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
    </application>
  </compatibility>
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    </windowsSettings>
  </application>
</assembly>
```

---

## .gitignore（抜粋）
```gitignore
# Build artifacts
bin/
obj/
*.user
*.suo
*.userprefs
*.csproj.user
*.pidb
*.svclog
*.Designer.cs
*.db
.vs/
# Rider / VSCode
.idea/
.vscode/
# Publish output
publish/
artifacts/
```

## .editorconfig（最小）
```ini
root = true

[*.cs]
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
csharp_new_line_before_open_brace = all
indent_size = 2
insert_final_newline = true
end_of_line = lf
charset = utf-8
```

## .gitattributes（行末など）
```gitattributes
* text eol=lf
*.sln text eol=crlf
*.cs text eol=lf
*.xaml text eol=lf
```

---

## .github/workflows/build.yml（Windowsランナーでビルド＆配布ZIPを生成）
```yaml
name: build
on:
  push:
    branches: [ main ]
  pull_request:

jobs:
  build-win:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore ./src/UxplayLauncher/UxplayLauncher.csproj
      - name: Build
        run: dotnet build ./src/UxplayLauncher/UxplayLauncher.csproj -c Release
      - name: Publish (self-contained)
        run: >-
          dotnet publish ./src/UxplayLauncher/UxplayLauncher.csproj -c Release
          -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
          -o ./artifacts/win-x64
      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: UxplayLauncher-win-x64
          path: artifacts/win-x64
```

> ※ MSIX 化は後段でも可能（Windows Application Packaging Project を別プロジェクトとして追加し、CI で `msbuild` する）。まずはポータブルZIPで配布→動作が固まったらMSIX化を推奨。

---

## src/UxplayLauncher/README.local.md（使い方メモ）
```md
# UxplayLauncher (WPF)

MSYS2/MinGW でビルド済みの `uxplay.exe` をGUIで起動するためのランチャ。

## 依存
- Windows 10/11 x64
- MSYS2 の GStreamer プラグイン（必要に応じて）
- mDNS(UDP 5353)がFWで許可されていること

## 使い方
1. 起動後、`uxplay.exe` のパスを指定
2. 解像度とFPS等を選択（規定: 1280x720 / 30fps）
3. 必要なら `-hls`, `-vsync no`, `-async`, `-as 0`, `-pw`, `-p` を設定
4. 起動

## 備考
- `VideoSink` は既定で `d3d11videosink fullscreen-toggle-mode=alt-enter`
- GStreamerパスが必要なら環境変数 `GST_PLUGIN_SYSTEM_PATH_1_0` を設定（UIから可変に拡張予定）


---

## UxPlay を同梱ビルドする（git submodule + MSYS2 MinGW64 + CMake）

> 方針: リポジトリに `third_party/UxPlay` を **サブモジュール**として取り込み、CI（Windows）で MSYS2/MinGW64 を使って CMake ビルド → 生成された `uxplay.exe` を WPF の `publish` へ同梱します。ローカル Windows でも同じスクリプトでビルド可能。

### 1) サブモジュール追加
```bash
# 既存リポジトリのルートで
git submodule add https://github.com/antimof/UxPlay.git third_party/UxPlay
# 固定したいコミット/タグがあれば checkout
# cd third_party/UxPlay && git checkout v1.72.2  # 例

git commit -m "chore: add UxPlay as submodule"
```

### 2) 依存ツール（MSYS2/MinGW64, Bonjour, GStreamer など）
- **Windows でのビルド/実行**は **MSYS2 + MinGW-64** が前提。Bonjour(mDNSResponder) が必要（AirPlay検出）。GStreamer と各種プラグインも必要。DeepWikiのまとめがわかりやすいです。citeturn3view0

### 3) CMake ビルドスクリプト（MSYS2内で実行）
- `scripts/build-uxplay-mingw64.sh` を追加して、MinGW64 環境から CMake → MinGW Makefiles でビルド。

#### `scripts/build-uxplay-mingw64.sh`
```bash
#!/usr/bin/env bash
set -euxo pipefail
# MSYS2 mingw64 shell で実行することを想定
# 依存パッケージ（必要に応じて調整）
pacman -S --needed --noconfirm \
  mingw-w64-x86_64-gcc \
  mingw-w64-x86_64-cmake \
  mingw-w64-x86_64-pkgconf \
  mingw-w64-x86_64-openssl \
  mingw-w64-x86_64-libplist \
  mingw-w64-x86_64-gstreamer \
  mingw-w64-x86_64-gst-plugins-base \
  mingw-w64-x86_64-gst-plugins-good \
  mingw-w64-x86_64-gst-plugins-bad \
  mingw-w64-x86_64-gst-libav

# サブモジュールを更新
git submodule update --init --recursive

pushd third_party/UxPlay
rm -rf build && mkdir build && cd build
cmake -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=Release ..
cmake --build . --config Release -j
# 成果物の場所（uxplay.exe）を検索して上位へコピー
UXOUT=$(find . -type f -name uxplay.exe | head -n1)
cp -f "$UXOUT" ../../uxplay.exe
popd
```

> **補足**: 依存は UxPlay の README/DeepWiki 記載の通り（OpenSSL/Libplist/GStreamer等）。Windowsでは別途 **Bonjour for Windows**（mDNSResponder）も必要。citeturn3view0

### 4) PowerShell ラッパ（CI/ローカル両用）
- Windows 上で MSYS2 を呼び出して上記スクリプトを実行します。

#### `scripts/invoke-msys2-build.ps1`
```powershell
param(
  [string]$Msys2Root = "C:/msys64"
)
$bash = Join-Path $Msys2Root "usr/bin/bash.exe"
if (-Not (Test-Path $bash)) { throw "MSYS2 not found at $Msys2Root" }
& $bash -lc "cd `"$PWD`" && ./scripts/build-uxplay-mingw64.sh"
```

### 5) WPF の csproj に **BeforeBuild**（または AfterBuild）で同梱
- `UxplayLauncher.csproj` に **ターゲット**を追加して、ソースツリー直下にない `uxplay.exe` を `publish` 出力にコピー。

#### `src/UxplayLauncher/UxplayLauncher.csproj` 追記
```xml
  <Target Name="BuildUxplay" BeforeTargets="Build">
    <!-- MSYS2 経由で UxPlay をビルド（CI/ローカル共通） -->
    <Exec Command="powershell -ExecutionPolicy Bypass -File $(SolutionDir)scripts/invoke-msys2-build.ps1" />
  </Target>

  <Target Name="CopyUxplay" AfterTargets="Publish">
    <ItemGroup>
      <_UxExe Include="$(SolutionDir)third_party/UxPlay/../uxplay.exe" />
    </ItemGroup>
    <Copy SourceFiles="@(_UxExe)" DestinationFolder="$(PublishDir)" SkipUnchangedFiles="true" />
  </Target>
```

> **ヒント**: ローカル開発で「毎回ビルドが重い」場合は、`Condition` を付けて CI のみ実行する運用も可（例: `Condition=" '$(CI)' == 'true' "`）。

### 6) GitHub Actions を **MSYS2 付き**に拡張
- 公式 `msys2/setup-msys2` アクションで MinGW64 をセットアップ → ビルド → WPF publish に同梱します。

#### `.github/workflows/build.yml` 差し替え（抜粋）
```yaml
name: build
on:
  push:
    branches: [ main ]
  pull_request:

jobs:
  build-win:
    runs-on: windows-latest
    env:
      CI: "true"
    steps:
      - uses: actions/checkout@v4
        with:
          submodules: recursive

      - name: Setup MSYS2 (MinGW64)
        uses: msys2/setup-msys2@v2
        with:
          msystem: MINGW64
          install: >-
            git
            mingw-w64-x86_64-gcc
            mingw-w64-x86_64-cmake
            mingw-w64-x86_64-pkgconf
            mingw-w64-x86_64-openssl
            mingw-w64-x86_64-libplist
            mingw-w64-x86_64-gstreamer
            mingw-w64-x86_64-gst-plugins-base
            mingw-w64-x86_64-gst-plugins-good
            mingw-w64-x86_64-gst-plugins-bad
            mingw-w64-x86_64-gst-libav

      - name: Build uxplay (MinGW64)
        shell: bash
        run: |
          set -euxo pipefail
          ./scripts/build-uxplay-mingw64.sh

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore ./src/UxplayLauncher/UxplayLauncher.csproj

      - name: Publish (self-contained)
        run: >-
          dotnet publish ./src/UxplayLauncher/UxplayLauncher.csproj -c Release
          -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
          -o ./artifacts/win-x64

      - name: Copy uxplay.exe into artifact
        run: |
          copy uxplay.exe artifacts/win-x64/uxplay.exe

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: UxplayLauncher-with-uxplay-win-x64
          path: artifacts/win-x64
```

### 7) 実行時（配布物）
- 配布ZIPには `UxplayLauncher.exe` と **同ディレクトリ**に `uxplay.exe` が入ります。ランチャは同梱版を自動検出→UI設定を引数へ変換→起動。Bonjour と GStreamer プラグインがシステムにあることが前提（Windowsで必要）。citeturn3view0

### 8) ライセンス表記
- 本リポジトリは **GPL-3.0-only**。`third_party/UxPlay` も GPLv3 なので整合します。配布ZIP内に `LICENSE`（あなたのリポジトリのGPLv3）と、UxPlay の `LICENSE` を同梱しておくのが無難。

---

## （任意）アプリ側：同梱 uxplay.exe の自動検出
- `MainWindow.xaml.cs` の起動時に `Path.Combine(AppContext.BaseDirectory, "uxplay.exe")` を探し、見つかればUIのパス欄を自動入力する処理を足すとUXが上がります。

```csharp
// MainWindow() 末尾などに
var bundled = System.IO.Path.Combine(AppContext.BaseDirectory, "uxplay.exe");
if (System.IO.File.Exists(bundled)) {
  UxplayPathBox.Text = bundled;
}
```

---
