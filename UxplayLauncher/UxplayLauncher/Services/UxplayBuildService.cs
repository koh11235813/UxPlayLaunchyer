using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace UxplayLauncher.Services;

public class UxplayBuildService
{
    public event EventHandler<string>? BuildProgress;
    public event EventHandler<bool>? BuildCompleted;

    private readonly string _msys2Root = "C:/msys64";
    private readonly string _bashPath;
    private readonly string _buildScriptPath;
    private readonly string _repoRoot;

    public UxplayBuildService()
    {
        _bashPath = Path.Combine(_msys2Root, "usr/bin/bash.exe");
        // スクリプト位置を堅牢に検出
        _buildScriptPath = FindScriptPath();
        // リポジトリルート（./UxplayLauncher/scripts の親の親）
        _repoRoot = _buildScriptPath != string.Empty
            ? Directory.GetParent(Directory.GetParent(_buildScriptPath)!.FullName)!.FullName
            : AppDomain.CurrentDomain.BaseDirectory;
    }

    public async Task<bool> BuildUxplayAsync(bool updateToLatest = true)
    {
        try
        {
            OnBuildProgress("ビルドを開始しています...");
            OnBuildProgress($"Resolved build script: {_buildScriptPath}");
            OnBuildProgress($"WorkingDirectory: {_repoRoot}");

            // MSYS2の存在確認
            if (!File.Exists(_bashPath))
            {
                OnBuildProgress("❌ MSYS2 が見つかりません。C:/msys64 にインストールしてください。");
                OnBuildCompleted(false);
                return false;
            }

            // ビルドスクリプトの存在確認
            if (string.IsNullOrEmpty(_buildScriptPath) || !File.Exists(_buildScriptPath))
            {
                OnBuildProgress("❌ ビルドスクリプトが見つかりません。");
                OnBuildCompleted(false);
                return false;
            }

            if (updateToLatest)
            {
                OnBuildProgress("最新のソースコードを取得しています...");
                await RunGitCommand("fetch origin");
                await RunGitCommand("reset --hard origin/main");
            }

            OnBuildProgress("サブモジュールを更新しています...");
            await RunGitCommand("submodule update --init --recursive");

            OnBuildProgress("UxPlay をビルドしています...");
            var success = await RunBuildScript();

            if (success)
            {
                OnBuildProgress("✅ ビルドが完了しました");
                OnBuildCompleted(true);
                return true;
            }
            else
            {
                OnBuildProgress("❌ ビルドに失敗しました");
                OnBuildCompleted(false);
                return false;
            }
        }
        catch (Exception ex)
        {
            OnBuildProgress($"❌ ビルドエラー: {ex.Message}");
            OnBuildCompleted(false);
            return false;
        }
    }

    private async Task RunGitCommand(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Git コマンドが失敗しました: {error}");
        }

        if (!string.IsNullOrEmpty(output))
        {
            OnBuildProgress($"Git: {output.Trim()}");
        }
    }

    private async Task<bool> RunBuildScript()
    {
        // PowerShellスクリプトを使用してMSYS2環境でビルドを実行
        var psScriptPath = ResolveInvokeScriptPath();

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{psScriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            // リポジトリルートで実行（./UxplayLauncher/scripts/... が解決できるように）
            WorkingDirectory = _repoRoot
        };

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                OnBuildProgress($"Build: {e.Data}");
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                OnBuildProgress($"Build Error: {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        return process.ExitCode == 0;
    }

    public bool IsMsys2Available()
    {
        return File.Exists(_bashPath);
    }

    public string GetMsys2Path()
    {
        return _msys2Root;
    }

    private static string ResolveInvokeScriptPath()
    {
        // 1) 実行ディレクトリ近傍
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "invoke-msys2-build.ps1"),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "scripts", "invoke-msys2-build.ps1")),
            // リポジトリルート（UxplayLauncher/scripts）
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "UxplayLauncher", "scripts", "invoke-msys2-build.ps1"))
        };
        foreach (var p in candidates)
        {
            if (File.Exists(p)) return p;
        }
        // 最後にワーキングディレクトリ基準
        var wd = Directory.GetCurrentDirectory();
        var p4 = Path.Combine(wd, "UxplayLauncher", "scripts", "invoke-msys2-build.ps1");
        return File.Exists(p4) ? p4 : string.Empty;
    }

    private static string FindScriptPath()
    {
        var relative = Path.Combine("UxplayLauncher", "scripts", "build-uxplay-mingw64.sh");
        // 現在/実行/親を遡って探索
        var startDirs = new[]
        {
            AppDomain.CurrentDomain.BaseDirectory,
            Directory.GetCurrentDirectory()
        };
        foreach (var start in startDirs)
        {
            var dir = new DirectoryInfo(start);
            for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent!)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return string.Empty;
    }

    private void OnBuildProgress(string message)
    {
        BuildProgress?.Invoke(this, message);
    }

    private void OnBuildCompleted(bool success)
    {
        BuildCompleted?.Invoke(this, success);
    }
}
