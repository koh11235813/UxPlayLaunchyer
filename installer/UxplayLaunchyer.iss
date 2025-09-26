#define MyAppName "Uxplay Launcher"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "YourName"
#define MyAppURL "https://github.com/yourname/UxPlayLaunchyer"
#define MyAppExeName "UxplayLauncher.exe"

[Setup]
AppId={{B7E1C5B7-6E2B-4F7F-9A43-3B1F3A2E9A10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={pf}\UxplayLauncher
DefaultGroupName=Uxplay Launcher
DisableProgramGroupPage=yes
OutputDir=.\artifacts\installer
OutputBaseFilename=UxplayLauncher-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
LicenseFile=..\LICENSE

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成する"; GroupDescription: "追加タスク:"; Flags: unchecked

[Files]
; 発行済みバイナリを丸ごと同梱（SelfContained/SingleFile想定）
Source: "..\artifacts\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
; UxPlayのライセンスも同梱
Source: "..\artifacts\win-x64\LICENSE.UxPlay"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Uxplay Launcher"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\Uxplay Launcher"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Uxplay Launcher を起動"; Flags: nowait postinstall skipifsilent
