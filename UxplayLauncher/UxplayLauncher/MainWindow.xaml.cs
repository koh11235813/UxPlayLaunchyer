using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using UxplayLauncher.Models;
using UxplayLauncher.Services;

namespace UxplayLauncher;

public partial class MainWindow : Window
{
    private readonly UxplayProcess _proc = new();
    private readonly UxplayBuildService _buildService = new();
    private readonly DependencyManager _dependencyManager = new();
    private AppSettings _settings = new();
    private bool _isBuilding = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeSettings();
        SetupEventHandlers();
        CheckUxplayPath();
    }

    private void InitializeSettings()
    {
        // 既定値
        ResolutionBox.Text = "1280x720"; // 規定: HD
        FpsBox.SelectedIndex = 0; // 30fps
        VideoSinkBox.Text = "d3d11videosink fullscreen-toggle-mode=alt-enter";

        // バージョン情報を設定
        VersionText.Text = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";
    }

    private void SetupEventHandlers()
    {
        _proc.OutputReceived += (_, s) => AppendLog(s);
        _proc.ErrorReceived += (_, s) => AppendLog(s);
        _proc.Exited += (_, __) => Dispatcher.Invoke(() =>
        {
            StatusText.Text = "停止中";
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
        });

        _buildService.BuildProgress += (_, progress) => Dispatcher.Invoke(() =>
        {
            BuildStatusText.Text = progress;
            BuildStatusText.Foreground = progress.Contains("完了") ? System.Windows.Media.Brushes.Green :
                                       progress.Contains("エラー") ? System.Windows.Media.Brushes.Red :
                                       System.Windows.Media.Brushes.Orange;
        });

        _buildService.BuildCompleted += (_, success) => Dispatcher.Invoke(() =>
        {
            _isBuilding = false;
            BuildBtn.IsEnabled = true;
            UpdateBuildBtn.IsEnabled = true;
            BuildBtn.Content = "UxPlay をビルド";
            UpdateBuildBtn.Content = "最新版をビルド";
            if (success)
            {
                CheckUxplayPath();
                AppendLog("✅ UxPlay のビルドが完了しました");
            }
            else
            {
                AppendLog("❌ UxPlay のビルドに失敗しました");
            }
        });
    }

    private void CheckUxplayPath()
    {
        // 自動的にuxplay.exeを検索（単一ファイル実行や発行ディレクトリも考慮）
        string? exeDir = null;
        try { exeDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty); } catch { }

        var candidateDirs = new List<string?>
        {
            AppDomain.CurrentDomain.BaseDirectory,
            Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName,
            Environment.CurrentDirectory,
            exeDir,
            // 発行規定パス候補
            Path.Combine(Environment.CurrentDirectory, "UxplayLauncher", "UxplayLauncher", "bin", "Release", "net8.0-windows", "win-x64", "publish")
        };

        foreach (var dir in candidateDirs)
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            var path = Path.Combine(dir, "uxplay.exe");
            if (File.Exists(path))
            {
                UxplayPathBox.Text = path;
                BuildStatusText.Text = $"UxPlay が見つかりました: {path}";
                BuildStatusText.Foreground = System.Windows.Media.Brushes.Green;
                return;
            }
        }

        BuildStatusText.Text = "UxPlay が見つかりません - ビルドが必要です";
        BuildStatusText.Foreground = System.Windows.Media.Brushes.Orange;
    }

    private void BrowseUxplay_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
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

        // 基本設定
        if (!string.IsNullOrWhiteSpace(s.Resolution)) a.Add($"-s {s.Resolution}");
        if (s.Fps > 0) a.Add($"-fps {s.Fps}");

        // オプション設定
        if (s.EnableHls) a.Add("-hls");
        if (s.AsyncAudio) a.Add("-async");
        if (s.NoVSync) a.Add("-vsync no");
        if (s.EnableDebug) a.Add("-d");
        if (s.EnableVerbose) a.Add("-v");
        if (s.EnableMirror) a.Add("-m");
        if (s.EnableAirplay2) a.Add("-a2");
        if (s.EnableRaop) a.Add("-r");

        // 音声設定
        if (s.AudioOff)
        {
            a.Add("-as 0");
        }
        else if (!string.IsNullOrWhiteSpace(s.AudioSink))
        {
            a.Add($"-as \"{s.AudioSink}\"");
        }

        if (s.AudioLatency.HasValue) a.Add($"-al {s.AudioLatency.Value}");

        // セキュリティ設定
        if (!string.IsNullOrEmpty(s.Password)) a.Add($"-pw \"{s.Password}\"");
        if (s.BasePort.HasValue) a.Add($"-p {s.BasePort.Value}");
        if (!string.IsNullOrWhiteSpace(s.Name)) a.Add($"-n \"{s.Name}\"");

        // 高度な設定
        if (!string.IsNullOrWhiteSpace(s.VideoSink)) a.Add($"-vs \"{s.VideoSink}\"");

        // カスタム引数
        if (!string.IsNullOrWhiteSpace(s.CustomArgs)) a.Add(s.CustomArgs);

        return string.Join(" ", a);
    }

    private async void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        // 自動ビルドが有効で、uxplay.exeが見つからない場合はビルドを実行
        if (AutoBuildCheck.IsChecked == true && !File.Exists(UxplayPathBox.Text))
        {
            AppendLog("🔨 自動ビルドを開始します...");
            await BuildUxplayAsync();

            // ビルド後に再度パスをチェック
            if (!File.Exists(UxplayPathBox.Text))
            {
                MessageBox.Show("UxPlay のビルドに失敗しました。手動でパスを指定してください。",
                    "Uxplay Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        _settings = new AppSettings
        {
            UxplayPath = UxplayPathBox.Text,
            Resolution = (ResolutionBox.Text ?? string.Empty).Trim(),
            Fps = int.TryParse((FpsBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString(), out var fps) ? fps : 30,
            EnableHls = HlsCheck.IsChecked == true,
            AsyncAudio = AsyncAudioCheck.IsChecked == true,
            NoVSync = NoVsyncCheck.IsChecked == true,
            AudioOff = AudioOffCheck.IsChecked == true,
            EnableDebug = EnableDebugCheck.IsChecked == true,
            EnableVerbose = EnableVerboseCheck.IsChecked == true,
            EnableMirror = EnableMirrorCheck.IsChecked == true,
            EnableAirplay2 = EnableAirplay2Check.IsChecked == true,
            EnableRaop = EnableRaopCheck.IsChecked == true,
            Password = PasswordBox.Password,
            BasePort = int.TryParse(BasePortBox.Text, out var p) ? p : null,
            Name = DeviceNameBox.Text,
            VideoSink = VideoSinkBox.Text,
            AudioSink = AudioSinkBox.Text,
            AudioLatency = int.TryParse(AudioLatencyBox.Text, out var al) ? al : null,
            CustomArgs = CustomArgsBox.Text
        };

        if (!File.Exists(_settings.UxplayPath))
        {
            MessageBox.Show("uxplay.exe のパスが不正です。", "Uxplay Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var args = BuildArgs(_settings);

        // GStreamerとMSYS2の環境変数を設定
        var extraEnv = new Dictionary<string, string?> {
            // GStreamerプラグインパス
            {"GST_PLUGIN_SYSTEM_PATH_1_0", @"C:\msys64\mingw64\lib\gstreamer-1.0"},
            {"GST_PLUGIN_PATH_1_0", @"C:\msys64\mingw64\lib\gstreamer-1.0"},
            
            // MSYS2のライブラリパス
            {"PATH", $@"C:\msys64\mingw64\bin;C:\msys64\usr\bin;{Environment.GetEnvironmentVariable("PATH")}"},
            
            // GStreamerの基本設定
            {"GST_DEBUG", "2"},
            {"GST_DEBUG_NO_COLOR", "1"},
            
            // その他の必要な環境変数
            {"PKG_CONFIG_PATH", @"C:\msys64\mingw64\lib\pkgconfig"},
            {"LD_LIBRARY_PATH", @"C:\msys64\mingw64\lib"}
        };

        // 依存DLLをコピー
        var uxplayDir = Path.GetDirectoryName(_settings.UxplayPath)!;
        if (!_dependencyManager.AreDependenciesAvailable(uxplayDir))
        {
            AppendLog("📦 必要なDLLをコピーしています...");
            if (_dependencyManager.CopyRequiredDependencies(uxplayDir))
            {
                AppendLog("✅ 依存DLLのコピーが完了しました");
            }
            else
            {
                AppendLog("⚠️ 一部のDLLのコピーに失敗しました");
            }
        }

        AppendLog($"> {Path.GetFileName(_settings.UxplayPath)} {args}");
        try
        {
            _proc.Start(_settings.UxplayPath, args, uxplayDir, extraEnv);
            StatusText.Text = "実行中";
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            AppendLog($"❌ エラー: {ex.Message}");
            StatusText.Text = "停止中";
        }
    }

    private void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        _proc.Stop();
        StatusText.Text = "停止中";
        StartBtn.IsEnabled = true;
        StopBtn.IsEnabled = false;
    }

    private async void BuildBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isBuilding) return;
        await BuildUxplayAsync(UpdateToLatestCheck.IsChecked == true);
    }

    private async void UpdateBuildBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_isBuilding) return;
        await BuildUxplayAsync(true); // 常に最新版でビルド
    }

    private async Task BuildUxplayAsync(bool updateToLatest = false)
    {
        if (_isBuilding) return;

        _isBuilding = true;
        BuildBtn.IsEnabled = false;
        UpdateBuildBtn.IsEnabled = false;
        BuildBtn.Content = "ビルド中...";
        UpdateBuildBtn.Content = "ビルド中...";

        try
        {
            await _buildService.BuildUxplayAsync(updateToLatest);
        }
        catch (Exception ex)
        {
            AppendLog($"❌ ビルドエラー: {ex.Message}");
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
    }

    private void SaveLog_Click(object sender, RoutedEventArgs e)
    {
        var saveDialog = new SaveFileDialog
        {
            Filter = "テキストファイル|*.txt|すべてのファイル|*.*",
            DefaultExt = "txt",
            FileName = $"uxplay_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(saveDialog.FileName, LogBox.Text);
                AppendLog($"📁 ログを保存しました: {saveDialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ログの保存に失敗しました: {ex.Message}", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void AppendLog(string? line)
    {
        if (string.IsNullOrEmpty(line)) return;
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogBox.AppendText($"[{timestamp}] {line}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        });
    }
}