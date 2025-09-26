#!/usr/bin/env bash
set -euxo pipefail
# MSYS2 mingw64 shell で実行することを想定
# 依存パッケージ（必要に応じて調整）
pacman -S --needed --noconfirm \
  mingw-w64-x86_64-gcc \
  mingw-w64-x86_64-cmake \
  mingw-w64-x86_64-pkgconf \
  mingw-w64-x86_64-openssl \
  mingw-w64-x86_64-libplist \
  mingw-w64-x86_64-gstreamer \
  mingw-w64-x86_64-gst-plugins-base \
  mingw-w64-x86_64-gst-plugins-good \
  mingw-w64-x86_64-gst-plugins-bad \
  mingw-w64-x86_64-gst-libav

# サブモジュールを更新
git submodule update --init --recursive

pushd third_party/UxPlay
rm -rf build && mkdir build && cd build
cmake -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=Release ..
cmake --build . --config Release -j
# 成果物の場所（uxplay.exe）を検索して上位へコピー
UXOUT=$(find . -type f -name uxplay.exe | head -n1)
cp -f "$UXOUT" ../../uxplay.exe
popd