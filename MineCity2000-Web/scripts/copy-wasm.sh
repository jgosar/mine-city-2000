#!/usr/bin/env bash
# Publishes the .NET WebAssembly converter and copies it into Angular's assets.
# Run from the MineCity2000-Web folder (npm run build does this via "prebuild").
#
# Asset wiring: angular.json maps src/assets -> /assets (in addition to the default public/ folder),
# so the runtime URL of the worker is <base href>assets/worker.js, and it loads ./_framework/* and
# ./buildings.zip relative to itself. Only src/assets/.nojekyll is tracked in git; everything this
# script copies there is git-ignored.
#
# The publish folder is deleted first because dotnet publish does not clean it, so fingerprinted files
# from earlier publishes would otherwise pile up and be shipped. Only POSIX tools (rm, cp) are used so the
# script also runs in Git Bash on Windows.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

publish=../MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot

rm -rf ../MineCity2000.Wasm/bin/Release/net10.0/publish
dotnet publish ../MineCity2000.Wasm -c Release

assets=src/assets

[ -d "$publish/_framework" ] || { echo "publish output missing: $publish" >&2; exit 1; }

mkdir -p "$assets"
rm -rf "$assets/_framework" && cp -R "$publish/_framework" "$assets/_framework"
cp "$publish/worker.js" "$publish/main.js" "$publish/buildings.zip" "$assets/"
