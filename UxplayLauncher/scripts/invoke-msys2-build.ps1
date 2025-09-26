param(
  [string]$Msys2Root = "C:\msys64"
)

$msys2shell = Join-Path $Msys2Root "msys2_shell.cmd"
if (-not (Test-Path $msys2shell)) { throw "msys2_shell.cmd not found: $msys2shell" }

Write-Host "=== MSYS2 ビルドスクリプト開始 ==="
Write-Host "MSYS2 Root: $Msys2Root"
Write-Host "Shell: $msys2shell"
Write-Host "Current directory: $PWD"

# CRLF -> LF を確実に潰す（shebang対策）
$sh = "UxplayLauncher\scripts\build-uxplay-mingw64.sh"
(Get-Content $sh -Raw).Replace("`r`n","`n") | Set-Content $sh -NoNewline

# -here: 現在のディレクトリを自動で /c/... に変換してくれる
# -mingw64: MINGW64 環境
# -no-start: 既存コンソール上で実行（新しいウィンドウを開かない）
# -c: このコマンドを実行
& $msys2shell -mingw64 -here -no-start -c "./UxplayLauncher/scripts/build-uxplay-mingw64.sh"
if ($LASTEXITCODE -ne 0) { throw "ビルドが失敗しました (Exit Code: $LASTEXITCODE)" }
