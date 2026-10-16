// Module Web Worker hosting the .NET WebAssembly runtime.
//   main -> worker : { type: 'convert', sc2: Uint8Array, cityName: string, fill: bool, terrain: bool, timestamp?: number, seed?: number }
//   worker -> main : { type: 'ready' } | { type: 'progress', percent } | { type: 'done', zip: Uint8Array } | { type: 'error', message }
// When both timestamp and seed are numbers, the reproducible ConvertDeterministic export is used (verification only).
import { dotnet } from './_framework/dotnet.js';

let converter;
try {
  const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();
  setModuleImports('worker.js', {
    progress: percent => self.postMessage({ type: 'progress', percent }),
  });
  const exports = await getAssemblyExports(getConfig().mainAssemblyName);
  converter = exports.ConverterExports;
} catch (e) {
  self.postMessage({ type: 'error', message: `Could not start the converter: ${String(e?.message ?? e)}` });
}

// One shared load: concurrent convert messages must not fetch buildings.zip twice.
let loading;

function loadBuildings() {
  loading ??= (async () => {
    const response = await fetch(new URL('./buildings.zip', import.meta.url), { cache: 'force-cache' });
    if (!response.ok) throw new Error(`Could not load building models (HTTP ${response.status})`);
    converter.LoadBuildings(new Uint8Array(await response.arrayBuffer()));
  })().catch(e => {
    loading = undefined;
    throw e;
  });
  return loading;
}

self.onmessage = async ({ data }) => {
  if (data.type !== 'convert') return;
  if (!converter) {
    self.postMessage({ type: 'error', message: 'The converter is not available' });
    return;
  }
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

if (converter) self.postMessage({ type: 'ready' });
