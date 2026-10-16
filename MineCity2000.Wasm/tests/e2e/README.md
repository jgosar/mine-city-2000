End-to-end check of the web page in headless Chrome (no npm packages). Needs Chrome (or set `CHROME`), Node >= 22.4, `unzip` and `dotnet`.
Build the site first: `dotnet publish MineCity2000.Wasm -c Release`, then run `node MineCity2000.Wasm/tests/e2e/chrome-e2e.mjs [siteDir]` (default: that publish's `wwwroot`).
Optional: `--clean` deletes the publish folder and republishes; `MC2K_PREFIX` (URL prefix, default `/mine-city-2000/`) and `MC2K_OUT` (keep the downloaded zips in this folder).
