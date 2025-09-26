param(
[string]$Msys2Root = "C:/msys64"
)
$bash = Join-Path $Msys2Root "usr/bin/bash.exe"
if (-Not (Test-Path $bash)) { throw "MSYS2 not found at $Msys2Root" }
& $bash -lc "cd `"$PWD`" && ./scripts/build-uxplay-mingw64.sh"