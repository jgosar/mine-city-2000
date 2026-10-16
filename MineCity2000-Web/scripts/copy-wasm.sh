#!/usr/bin/env bash
# Publishes the .NET WebAssembly converter and copies it into Angular's assets.
# Run from the MineCity2000-Web folder (npm run build does this via "prebuild").
#
# Asset wiring: angular.json maps src/assets -> /assets (in addition to the default public/ folder),
# so the runtime URL of the worker is <base href>assets/worker.js, and it loads ./_framework/* and
# ./buildings.zip relative to itself. Only src/assets/.nojekyll is tracked in git; everything this
# script copies there is git-ignored.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

dotnet publish ../MineCity2000.Wasm -c Release

publish=../MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot
assets=src/assets

[ -d "$publish/_framework" ] || { echo "publish output missing: $publish" >&2; exit 1; }

mkdir -p "$assets/_framework"
rsync -a --delete "$publish/_framework/" "$assets/_framework/"
cp "$publish/worker.js" "$publish/main.js" "$publish/buildings.zip" "$assets/"
