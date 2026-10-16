// @ts-check
// End-to-end check in headless Chrome. Usage: node chrome-e2e.mjs [siteDir] [--clean]
// siteDir defaults to the Release publish wwwroot; --clean deletes that publish folder and re-runs `dotnet publish` first.
// Env: CHROME (browser binary), MC2K_PREFIX (URL prefix, default /mine-city-2000/), MC2K_OUT (kept output dir, default a temp dir).
// Needs Node >= 22.4 (global WebSocket), `unzip`, and `dotnet` (the golden comparison runs the test project).
import { execFileSync, spawn } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { startServer } from './serve.mjs';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..');
const args = process.argv.slice(2);
const siteDir = path.resolve(args.find((a) => !a.startsWith('--')) ?? path.join(repo, 'MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot'));
const prefix = process.env.MC2K_PREFIX ?? '/mine-city-2000/';
const sleep = (/** @type {number} */ ms) => new Promise((r) => setTimeout(r, ms));
const assert = (/** @type {unknown} */ ok, /** @type {string} */ what) => { if (!ok) throw new Error(`FAILED: ${what}`); };
const fixture = path.join(repo, 'MineCity2000.Tests/Fixtures/test.sc2');

function findChrome() {
  const mac = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
  if (process.env.CHROME) {
    fs.accessSync(process.env.CHROME, fs.constants.X_OK); // an explicit CHROME must work; no silent fallback
    return process.env.CHROME;
  }
  for (const c of [fs.existsSync(mac) ? mac : '', 'google-chrome', 'google-chrome-stable', 'chromium']) {
    if (!c) continue;
    try { return path.isAbsolute(c) ? (fs.accessSync(c, fs.constants.X_OK), c) : execFileSync('which', [c], { encoding: 'utf8' }).trim(); } catch { /* try the next one */ }
  }
  throw new Error('No Chrome found: set CHROME to a Chrome/Chromium binary');
}

const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'mc2k-e2e-'));
const out = path.resolve(process.env.MC2K_OUT ?? path.join(tmp, 'out'));
/** @type {import('node:child_process').ChildProcess | undefined} */ let chrome;
/** @type {{ port: number, close: () => void } | undefined} */ let server;
/** @type {WebSocket | undefined} */ let ws;
/** @type {Error | undefined} */ let chromeFailure;
/** execFileSync that explains a missing dotnet. @param {string} cmd @param {string[]} cmdArgs @param {import('node:child_process').ExecFileSyncOptions} [opts] */
const run = (cmd, cmdArgs, opts) => {
  try { return String(execFileSync(cmd, cmdArgs, opts) ?? ''); } catch (e) {
    if (/** @type {any} */ (e).code === 'ENOENT' && cmd === 'dotnet') throw new Error('dotnet not on PATH (on macOS with a user-local SDK: export PATH="$HOME/.dotnet:$PATH")');
    throw e;
  }
};
let cleanedUp = false;
async function cleanup() {
  if (cleanedUp) return;
  cleanedUp = true;
  try { ws?.close(); } catch { /* already closed */ }
  chrome?.kill('SIGKILL');
  server?.close();
  await sleep(300);
  fs.rmSync(tmp, { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
}
// Watchdog: a hung page or build must not hold a CI job until the job timeout.
setTimeout(async () => {
  console.error('e2e timed out after 10 minutes');
  process.exitCode = 1;
  await cleanup();
  process.exit();
}, 10 * 60 * 1000).unref();
const timings = /** @type {Record<string, number>} */ ({});
const timed = async (/** @type {string} */ name, /** @type {() => Promise<void>} */ fn) => { const t = Date.now(); await fn(); timings[name] = Date.now() - t; console.log(`ok  ${name} (${timings[name]} ms)`); };

try {
  if (args.includes('--clean')) {
    const publish = path.dirname(siteDir);
    assert(path.basename(publish) === 'publish', '--clean needs a siteDir inside a "publish" folder');
    fs.rmSync(publish, { recursive: true, force: true });
    run('dotnet', ['publish', path.join(repo, 'MineCity2000.Wasm'), '-c', 'Release'], { stdio: 'inherit' });
  }
  assert(fs.existsSync(path.join(siteDir, 'index.html')), `no index.html in ${siteDir} (run dotnet publish -c Release first)`);
  fs.mkdirSync(out, { recursive: true });
  for (const stale of ['golden.zip', 'ui.zip']) fs.rmSync(path.join(out, stale), { force: true });
  const notes = path.join(tmp, 'notes.txt');
  fs.writeFileSync(notes, 'this is not a city file at all');

  server = await startServer({ root: siteDir, prefix, fixtures: path.dirname(fixture), uploads: out });
  const origin = `http://127.0.0.1:${server.port}`;
  const profile = path.join(tmp, 'profile');
  const chromePath = findChrome();
  try {
    chrome = spawn(chromePath, ['--headless=new', '--no-sandbox', '--disable-gpu', '--no-first-run', '--remote-debugging-port=0', `--user-data-dir=${profile}`, 'about:blank'], { stdio: 'ignore' });
  } catch (e) {
    throw new Error(`Could not start Chrome (${chromePath}): ${e instanceof Error ? e.message : e}`);
  }
  chrome.on('error', (e) => (chromeFailure = new Error(`Could not start Chrome (${chromePath}): ${e.message}`)));
  chrome.on('exit', (code, signal) => (chromeFailure ??= new Error(`Chrome (${chromePath}) exited unexpectedly (${signal ?? `code ${code}`})`)));
  let debugPort = 0;
  for (let i = 0; i < 300 && !debugPort; i++) {
    if (chromeFailure) throw chromeFailure;
    try { debugPort = Number(fs.readFileSync(path.join(profile, 'DevToolsActivePort'), 'utf8').split('\n')[0]); } catch { await sleep(100); }
  }
  assert(debugPort, 'Chrome did not write DevToolsActivePort');
  let page;
  for (let i = 0; i < 100 && !page; i++) {
    try { page = (await (await fetch(`http://127.0.0.1:${debugPort}/json`)).json()).find((/** @type {any} */ t) => t.type === 'page'); } catch { /* not up yet */ }
    if (!page) await sleep(100);
  }
  assert(page, 'Chrome has no page target');

  const socket = (ws = new WebSocket(page.webSocketDebuggerUrl));
  await new Promise((r) => (socket.onopen = r));
  let nextId = 0;
  const pending = new Map();
  socket.onmessage = ({ data }) => {
    const m = JSON.parse(String(data));
    if (m.id) pending.get(m.id)?.(m);
    else if (m.method === 'Runtime.exceptionThrown') console.log('[page exception]', m.params.exceptionDetails.exception?.description ?? m.params.exceptionDetails.text);
    else if (m.method === 'Runtime.consoleAPICalled' && m.params.type === 'error') console.log('[console.error]', m.params.args.map((/** @type {any} */ a) => a.value ?? a.description).join(' '));
  };
  const send = (/** @type {string} */ method, params = {}, timeoutMs = 30_000) => new Promise((resolve, reject) => {
    const id = ++nextId;
    const timer = setTimeout(() => { pending.delete(id); reject(new Error(`FAILED: Chrome did not answer ${method} within ${timeoutMs / 1000} s`)); }, timeoutMs);
    pending.set(id, (/** @type {any} */ m) => { clearTimeout(timer); pending.delete(id); if (m.error) reject(new Error(`${method}: ${m.error.message}`)); else resolve(m.result); });
    socket.send(JSON.stringify({ id, method, params }));
  });
  const evaluate = async (/** @type {string} */ expression, timeoutMs = 30_000) => {
    const r = /** @type {any} */ (await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }, timeoutMs));
    if (r.exceptionDetails) throw new Error(`page: ${r.exceptionDetails.exception?.description ?? r.exceptionDetails.text}`);
    return r.result.value;
  };
  const waitFor = async (/** @type {string} */ expression, /** @type {string} */ what, seconds = 180) => {
    for (let i = 0; i < seconds * 10; i++) { if (await evaluate(expression).catch(() => false)) return; await sleep(100); }
    throw new Error(`FAILED: timeout waiting for ${what}`);
  };
  const setFile = async (/** @type {string} */ file) => {
    const { root } = /** @type {any} */ (await send('DOM.getDocument'));
    const { nodeId } = /** @type {any} */ (await send('DOM.querySelector', { nodeId: root.nodeId, selector: '#file-input' }));
    await send('DOM.setFileInputFiles', { files: [file], nodeId });
  };
  const text = (/** @type {string} */ id) => `document.getElementById('${id}').textContent.trim()`;
  const unzip = (/** @type {string[]} */ ...a) => run('unzip', a, { encoding: 'utf8' });

  await send('Runtime.enable'); await send('DOM.enable'); await send('Page.enable');
  await send('Page.navigate', { url: `${origin}${prefix}` });
  await waitFor(`${text('status')} === 'Choose a SimCity 2000 city file (.sc2).' || !document.getElementById('error').hidden`, 'the page to be ready', 60);
  assert(!(await evaluate(`!document.getElementById('error').hidden`)), `worker start error: ${await evaluate(text('error'))}`);

  await timed('golden: deterministic world matches the baseline', async () => {
    const info = await evaluate(`(async () => {
      const { ConverterClient } = await import('./converter-client.js');
      const file = new File([await (await fetch('/fixtures/test.sc2')).arrayBuffer()], 'test.sc2');
      const zip = await new ConverterClient().convert(file, false, false, { deterministic: { timestamp: 1700000000, seed: 12345 } });
      const r = await fetch('/upload/golden.zip', { method: 'POST', body: zip });
      if (!r.ok) throw new Error('upload failed: ' + r.status);
      return zip.length;
    })()`, 180_000);
    assert(info > 0, 'golden zip is empty');
    const world = path.join(out, 'golden');
    fs.rmSync(world, { recursive: true, force: true });
    unzip('-q', path.join(out, 'golden.zip'), '-d', world);
    assert(fs.existsSync(path.join(world, 'test/level.dat')), 'golden zip has no test/level.dat');
    // The hook passes trivially without a directory, so the path is checked above and the summary below.
    const result = run('dotnet', ['test', 'MineCity2000.Tests', '--filter', 'externalWorld_matchesBaseline_whenCompareDirSet'], { cwd: repo, encoding: 'utf8', timeout: 600_000, env: { ...process.env, MC2K_COMPARE_DIR: path.join(world, 'test') } });
    const summary = /Failed:\s+0,\s+Passed:\s+1,.*/.exec(result);
    assert(summary, `golden test did not report Passed: 1\n${result}`);
    console.log(`    ${summary[0].trim()}`);
  });

  await timed('ui: convert test.sc2 and download the zip', async () => {
    await setFile(fixture);
    await waitFor(`${text('status')} === 'Ready to convert.' && !document.getElementById('convert').disabled`, 'Convert to be enabled');
    await evaluate(`window.seen = []; new MutationObserver(() => { const m = /Converting… (\\d+)%/.exec(${text('status')}); if (m) window.seen.push(Number(m[1])); })
      .observe(document.getElementById('status'), { childList: true, characterData: true, subtree: true });
      document.getElementById('convert').click()`);
    await waitFor(`!document.getElementById('download').hidden`, 'the download link');
    const seen = /** @type {number[]} */ (await evaluate('window.seen'));
    assert(seen.length > 1 && Math.min(...seen) < 100 && Math.max(...seen) === 100 && seen.every((p, i) => i === 0 || p >= seen[i - 1]), `progress did not run up to 100: ${seen}`);
    assert(await evaluate(`document.getElementById('download').download === 'test.zip' && ${text('download')} === 'Download test.zip'`), 'download link is not "Download test.zip"');
    await evaluate(`(async () => { const blob = await (await fetch(document.getElementById('download').href)).blob(); const r = await fetch('/upload/ui.zip', { method: 'POST', body: blob }); if (!r.ok) throw new Error('upload failed: ' + r.status); })()`);
    const entries = unzip('-Z1', path.join(out, 'ui.zip')).split('\n').filter(Boolean);
    assert(entries.includes('test/level.dat'), 'ui zip has no test/level.dat');
    const regions = entries.filter((e) => /^test\/region\/[^/]+\.mca$/.test(e));
    assert(regions.length === 32, `ui zip has ${regions.length} region files, expected 32`);
    console.log(`    progress ${seen[0]}% -> ${seen.at(-1)}% in ${seen.length} steps; ${entries.length} zip entries`);
  });

  await timed('ui: a .txt file shows the error and re-enables Convert', async () => {
    await setFile(notes);
    await waitFor(`${text('file-name')} === 'notes.txt' && !document.getElementById('convert').disabled`, 'notes.txt to be chosen');
    await evaluate(`document.getElementById('convert').click()`);
    await waitFor(`!document.getElementById('error').hidden`, 'the error text');
    assert((await evaluate(text('error'))).includes('Not a SimCity 2000 city file'), `unexpected error: ${await evaluate(text('error'))}`);
    await waitFor(`!document.getElementById('convert').disabled`, 'Convert to be enabled again', 10);
  });

  console.log(`PASS e2e (${Object.values(timings).reduce((a, b) => a + b, 0)} ms total)`);
} catch (e) {
  console.error(e instanceof Error ? e.message : e);
  process.exitCode = 1;
} finally {
  await cleanup();
  process.exit();
}
