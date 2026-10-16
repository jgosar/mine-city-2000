// @ts-check
// DOM wiring for the page: all state lives in the variables below, render() derives everything shown from them.
import { ConverterClient, cityNameOf } from './converter-client.js';

/**
 * @template {HTMLElement} T
 * @param {string} id
 * @param {new () => T} type
 * @returns {T}
 */
function byId(id, type) {
  const element = document.getElementById(id);
  if (!(element instanceof type)) throw new Error(`Missing #${id} in index.html`);
  return element;
}

const dropZone = byId('drop-zone', HTMLDivElement);
const fileName = byId('file-name', HTMLParagraphElement);
const fileInput = byId('file-input', HTMLInputElement);
const fillBox = byId('fill', HTMLInputElement);
const terrainBox = byId('terrain', HTMLInputElement);
const convertButton = byId('convert', HTMLButtonElement);
const progressBar = byId('progress', HTMLProgressElement);
const statusText = byId('status', HTMLParagraphElement);
const errorText = byId('error', HTMLParagraphElement);
const downloadLink = byId('download', HTMLAnchorElement);

/** @type {ConverterClient | null} */
let client = null;

/** @type {File | null} */
let file = null;
let fill = false;
let terrain = false;
let workerReady = false;
let converting = false;
/** null until the converter reports its first progress (building models are still being loaded). */
let percent = /** @type {number | null} */ (null);
let workerError = '';
let conversionError = '';
let dragging = false;
/** Drag events also fire for the drop zone's children; the highlight stays on until every enter has a matching leave. */
let dragDepth = 0;
let downloadUrl = '';
let downloadName = '';
let downloadSize = 0;

function statusMessage() {
  if (converting) {
    if (percent !== null) return `Converting… ${percent}%`;
    return workerReady
      ? 'Loading building models (about 24 MB on the first run)…'
      : 'Loading converter…';
  }
  if (downloadUrl) return `Done: ${(downloadSize / 1048576).toFixed(1)} MB`;
  if (workerError) return 'The converter could not start.';
  if (!workerReady) return 'Loading converter…';
  return file ? 'Ready to convert.' : 'Choose a SimCity 2000 city file (.sc2).';
}

function render() {
  const error = workerError || conversionError;
  dropZone.classList.toggle('dragging', dragging);
  fileName.textContent = file ? file.name : 'Drop a .sc2 file here';
  fileInput.disabled = converting;
  fillBox.checked = fill;
  terrainBox.checked = terrain;
  convertButton.disabled = !(file !== null && !converting && !workerError);
  progressBar.hidden = !converting;
  if (percent === null) progressBar.removeAttribute('value');
  else progressBar.value = percent;
  statusText.textContent = statusMessage();
  errorText.hidden = !error;
  errorText.textContent = error;
  downloadLink.hidden = !downloadUrl;
  if (downloadUrl) {
    downloadLink.href = downloadUrl;
    downloadLink.download = downloadName;
    downloadLink.textContent = `Download ${downloadName}`;
  } else {
    downloadLink.removeAttribute('href');
    downloadLink.removeAttribute('download');
    downloadLink.textContent = '';
  }
}

function revokeDownload() {
  if (downloadUrl) URL.revokeObjectURL(downloadUrl);
  downloadUrl = '';
}

/** @param {File | null | undefined} chosen */
function setFile(chosen) {
  if (!chosen || converting) return;
  file = chosen;
  conversionError = '';
  revokeDownload();
  render();
}

async function convert() {
  const chosen = file;
  if (!chosen || converting) return;
  revokeDownload();
  conversionError = '';
  percent = null;
  converting = true;
  render();
  try {
    if (!client) throw new Error('The converter could not start.');
    const zip = await client.convert(chosen, fill, terrain, {
      onProgress: (value) => {
        percent = value;
        render();
      },
    });
    const blob = new Blob([zip], { type: 'application/zip' });
    downloadUrl = URL.createObjectURL(blob);
    downloadName = `${cityNameOf(chosen)}.zip`;
    downloadSize = blob.size;
  } catch (e) {
    conversionError = e instanceof Error ? e.message : String(e);
  } finally {
    converting = false;
    render();
  }
}

fileInput.addEventListener('change', () => setFile(fileInput.files?.[0]));
fillBox.addEventListener('change', () => {
  fill = fillBox.checked;
  render();
});
terrainBox.addEventListener('change', () => {
  terrain = terrainBox.checked;
  render();
});
convertButton.addEventListener('click', convert);

dropZone.addEventListener('dragenter', (event) => {
  event.preventDefault();
  dragDepth++;
  dragging = true;
  render();
});
dropZone.addEventListener('dragover', (event) => {
  event.preventDefault();
  if (!dragging) {
    dragging = true;
    render();
  }
});
dropZone.addEventListener('dragleave', () => {
  dragDepth = Math.max(0, dragDepth - 1);
  if (dragDepth === 0) {
    dragging = false;
    render();
  }
});
dropZone.addEventListener('drop', (event) => {
  event.preventDefault();
  dragDepth = 0;
  dragging = false;
  setFile(event.dataTransfer?.files?.[0]);
  render();
});

try {
  client = new ConverterClient();
  client.ready.then(
    () => {
      workerReady = true;
      render();
    },
    (e) => {
      workerError = e instanceof Error ? e.message : String(e);
      render();
    },
  );
} catch (e) {
  workerError = e instanceof Error ? e.message : String(e);
}

render();
