// @ts-check
// Static server for the e2e run: the site under a URL prefix, /fixtures/* from a folder, and POST /upload/<name> into a folder.
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';

const MIME = { '.js': 'text/javascript', '.mjs': 'text/javascript', '.wasm': 'application/wasm', '.html': 'text/html', '.css': 'text/css', '.json': 'application/json', '.zip': 'application/zip' };

/**
 * @param {{ root: string, prefix: string, fixtures: string, uploads: string }} options prefix starts and ends with '/', e.g. '/mine-city-2000/'
 * @returns {Promise<{ port: number, close: () => void }>} listens on a free port
 */
export function startServer({ root, prefix, fixtures, uploads }) {
  fs.mkdirSync(uploads, { recursive: true });
  /** @param {string} base @param {string} rel @returns {string | null} the file under base, or null when it would escape base */
  const inside = (base, rel) => {
    const file = path.resolve(base, rel);
    return file.startsWith(path.resolve(base) + path.sep) ? file : null;
  };
  const server = http.createServer((req, res) => {
    const url = decodeURIComponent(new URL(req.url ?? '/', 'http://x').pathname);
    if (req.method === 'POST' && url.startsWith('/upload/')) {
      /** @type {Buffer[]} */
      const chunks = [];
      req.on('data', (chunk) => chunks.push(chunk));
      req.on('end', () => {
        fs.writeFileSync(path.join(uploads, path.basename(url)), Buffer.concat(chunks));
        res.end('ok');
      });
      return;
    }
    const file = url.startsWith('/fixtures/') ? inside(fixtures, url.slice('/fixtures/'.length))
      : url.startsWith(prefix) ? inside(root, url.slice(prefix.length) || 'index.html') : null;
    if (!file || !fs.existsSync(file) || fs.statSync(file).isDirectory()) {
      res.statusCode = 404;
      return res.end('not found');
    }
    res.setHeader('Content-Type', MIME[path.extname(file)] ?? 'application/octet-stream');
    fs.createReadStream(file).pipe(res);
  });
  return new Promise((resolve) => {
    server.listen(0, '127.0.0.1', () => {
      const address = /** @type {import('node:net').AddressInfo} */ (server.address());
      resolve({ port: address.port, close: () => server.close() });
    });
  });
}
