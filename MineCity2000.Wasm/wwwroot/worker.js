// Module Web Worker hosting the .NET WebAssembly runtime.
//   main -> worker : { type: 'convert', sc2: Uint8Array, cityName: string, fill: bool, terrain: bool, timestamp?: number, seed?: number }
//   worker -> main : { type: 'ready' } | { type: 'progress', percent } | { type: 'done', zip: Uint8Array } | { type: 'error', message }
// When both timestamp and seed are numbers, the reproducible ConvertDeterministic export is used (verification only).
import { dotnet } from './_framework/dotnet.js';

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();
setModuleImports('worker.js', {
  progress: percent => self.postMessage({ type: 'progress', percent }),
});
const exports = await getAssemblyExports(getConfig().mainAssemblyName);
const converter = exports.ConverterExports;

let buildingsLoaded = false;

async function loadBuildings() {
  if (buildingsLoaded) return;
  const response = await fetch(new URL('./buildings.zip', import.meta.url), { cache: 'force-cache' });
  if (!response.ok) throw new Error(`Could not load building models (HTTP ${response.status})`);
  converter.LoadBuildings(new Uint8Array(await response.arrayBuffer()));
  buildingsLoaded = true;
}

self.onmessage = async ({ data }) => {
  if (data.type !== 'convert') return;
  try {
    await loadBuildings();
    const { sc2, cityName, fill, terrain, timestamp, seed } = data;
    const zip = (typeof timestamp === 'number' && typeof seed === 'number')
      ? converter.ConvertDeterministic(sc2, cityName, fill, terrain, timestamp, seed)
      : converter.Convert(sc2, cityName, fill, terrain);
    self.postMessage({ type: 'done', zip }, [zip.buffer]);
  } catch (e) {
    self.postMessage({ type: 'error', message: String(e?.message ?? e) });
  }
};

self.postMessage({ type: 'ready' });
