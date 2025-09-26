#!/usr/bin/env bash
set -euxo pipefail
# MSYS2 mingw64 shell で実行することを想定

echo "=== UxPlay ビルドスクリプト開始 ==="
echo "現在のディレクトリ: $(pwd)"
echo "日時: $(date)"

# MSYS2環境の確認
echo "=== MSYS2環境の確認 ==="
echo "MINGW64: $MSYSTEM"
echo "PATH: $PATH"

if [[ "${MSYSTEM:-}" != "MINGW64" ]]; then
  echo "ERROR: MSYSTEM is '${MSYSTEM:-}'. Please run under MINGW64."
  exit 1
fi

# 依存パッケージのインストール
echo "=== 依存パッケージをインストール中 ==="
pacman -S --needed --noconfirm \
  mingw-w64-x86_64-gcc \
  mingw-w64-x86_64-cmake \
  mingw-w64-x86_64-ninja \
  mingw-w64-x86_64-pkgconf \
  mingw-w64-x86_64-openssl \
  mingw-w64-x86_64-libplist \
  mingw-w64-x86_64-gstreamer \
  mingw-w64-x86_64-gst-plugins-base \
  mingw-w64-x86_64-gst-plugins-good \
  mingw-w64-x86_64-gst-plugins-bad \
  mingw-w64-x86_64-gst-libav

export CC=gcc
export CXX=g++
hash -r
gcc --version
g++ --version

echo "=== サブモジュールを更新中 ==="
git submodule update --init --recursive

echo "=== UxPlay ディレクトリに移動 ==="
pushd third_party/UxPlay

echo "=== ビルドディレクトリをクリーンアップ ==="
rm -rf build
mkdir build
cd build

echo "=== CMake でビルド設定を生成中 ==="
# cmake -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=gcc -DCMAKE_CXX_COMPILER=g++ ..
cmake ..

echo "=== UxPlay をビルド中 ==="
cmake --build . --config Release -j$(nproc)

echo "=== ビルド成果物を検索中 ==="
UXOUT=$(find . -type f -name uxplay.exe | head -n1)

if [ -z "$UXOUT" ]; then
    echo "❌ エラー: uxplay.exe が見つかりません"
    echo "ビルドログを確認してください"
    exit 1
fi

echo "✅ uxplay.exe が見つかりました: $UXOUT"

echo "=== 成果物をコピー中 ==="
cp -f "$UXOUT" ../../uxplay.exe

if [ -f "../../uxplay.exe" ]; then
    echo "✅ uxplay.exe のコピーが完了しました"
    ls -la ../../uxplay.exe
else
    echo "❌ エラー: uxplay.exe のコピーに失敗しました"
    exit 1
fi

popd

echo "=== UxPlay ビルドスクリプト完了 ==="
echo "日時: $(date)"