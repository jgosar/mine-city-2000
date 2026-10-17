End-to-end check of the web page in headless Chrome (nothing to install). Needs Chrome (or set `CHROME`), Node >= 22.4, `unzip` and `dotnet`.
The site must be built (and the page bundled) first: `dotnet publish MineCity2000.Wasm -c Release` does both (it needs Node and npm), then run `node MineCity2000.Wasm/tests/e2e/chrome-e2e.mjs [siteDir]` (default: that publish's `wwwroot`).
Optional: `--clean` deletes the publish folder and republishes; `MC2K_PREFIX` (URL prefix, default `/mine-city-2000/`) and `MC2K_OUT` (keep the downloaded zips in this folder).
