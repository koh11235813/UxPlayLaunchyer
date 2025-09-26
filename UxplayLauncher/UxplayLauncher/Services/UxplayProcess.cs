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
        _proc.ErrorDataReceived += (_, e) => ErrorReceived?.Invoke(this, e.Data);
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