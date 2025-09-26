# UxPlay Launcher (for windows)

## Requirements
- dotnet8.0-sdk
- msys2
- Bonjour SDK

You may not have Bonjour SDK, you can download it from [Apple Developer](https://developer.apple.com/download/all/?q=bonjour%20sdk).
May have to developer registration your Apple Account

## usage 
```sh
git clone https://github.com/koh11235813/UxPlayLaunchyer.git
cd UxPlayLauncher
dotnet publish .\UxplayLauncher\UxplayLauncher\UxplayLauncher.csproj -c Release
```
then, you can find `.exe` on `.\UxplayLauncher\UxplayLauncher\bin\Release\net8.0-windows\win-x64\UxplayLauncher.exe`
