# MineCity2000-Web

Angular front end for the in-browser SimCity 2000 to Minecraft converter. The conversion itself runs in
`MineCity2000.Wasm` (.NET WebAssembly) inside a module Web Worker; this app only owns the DOM.

## Build

```
npm install
npm run build -- --base-href /mine-city-2000/   # prebuild publishes the wasm project and copies it into src/assets
npm test                                          # Vitest + jsdom, headless
```

Requires the .NET 10 SDK with the `wasm-tools` workload (for `dotnet publish -c Release`) and `rsync`.

## Where the worker lives

`scripts/copy-wasm.sh` copies the publish output into `src/assets/` (`_framework/`, `worker.js`, `main.js`,
`buildings.zip`). `angular.json` maps `src/assets` to `<base href>assets/`, so the runtime URL of the worker is
`assets/worker.js`. Everything copied there is git-ignored; only `src/assets/.nojekyll` is tracked.
`ng serve` / `ng build` without `npm run build` need `scripts/copy-wasm.sh` to have been run once.
