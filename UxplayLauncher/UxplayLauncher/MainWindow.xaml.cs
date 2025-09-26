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
        _proc.ErrorReceived += (_, s) => AppendLog(s);
        _proc.Exited += (_, __) => Dispatcher.Invoke(() => StatusText.Text = "停止中");
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
            MessageBox.Show("uxplay.exe のパスが不正です。", "Uxplay Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
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