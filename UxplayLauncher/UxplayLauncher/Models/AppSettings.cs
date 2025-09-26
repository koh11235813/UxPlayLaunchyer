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

    // 追加のフラグ
    public bool EnableDebug { get; set; }
    public bool EnableVerbose { get; set; }
    public bool EnableMirror { get; set; }
    public string? AudioSink { get; set; }
    public int? AudioLatency { get; set; }
    public string? Name { get; set; }
    public bool EnableAirplay2 { get; set; }
    public bool EnableRaop { get; set; }
    public string? CustomArgs { get; set; }
}