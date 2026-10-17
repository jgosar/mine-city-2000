// Unit tests for web/converter-client.ts on Node's built-in runner (it strips the types itself): `npm test` in MineCity2000.Wasm.
import assert from 'node:assert/strict';
import { beforeEach, describe, it } from 'node:test';
import { ConverterClient, MAX_FILE_BYTES, cityNameOf } from '../web/converter-client.ts';
import type { FileLike } from '../web/converter-client.ts';

interface PostedMessage {
  type: string;
  sc2: Uint8Array;
  cityName: string;
  fill: boolean;
  terrain: boolean;
  timestamp?: number;
  seed?: number;
}

/** Stand-in for the module worker: records posted messages and lets a test emit worker messages. */
class FakeWorker {
  onmessage: ((event: { data: unknown }) => void) | null = null;
  onerror: ((event: { message: string }) => void) | null = null;
  onmessageerror: ((event: object) => void) | null = null;
  posted: PostedMessage[] = [];
  transfers: Transferable[][] = [];

  postMessage(message: PostedMessage, transfer: Transferable[] = []) {
    this.posted.push(message);
    this.transfers.push(transfer);
  }

  emit(data: unknown) {
    this.onmessage?.({ data });
  }

  fail(message: string) {
    this.onerror?.({ message });
  }
}

/** A plain object is enough for the client: it only reads name, size and arrayBuffer(). */
function fakeFile(name: string, bytes = [1, 2, 3], size = bytes.length) {
  const file: FileLike & { reads: number } = {
    name,
    size,
    reads: 0,
    arrayBuffer: async () => {
      file.reads++;
      return new Uint8Array(bytes).buffer;
    },
  };
  return file;
}

const tick = () => new Promise((resolve) => setTimeout(resolve));

describe('ConverterClient', () => {
  let worker: FakeWorker;
  let client: ConverterClient;
  const file = fakeFile('Springfield.sc2');

  beforeEach(() => {
    worker = new FakeWorker();
    client = new ConverterClient(() => worker as unknown as Worker);
  });

  it('resolves ready, then reports progress values, then resolves the zip', async () => {
    let ready = false;
    client.ready.then(() => (ready = true));
    worker.emit({ type: 'ready' });
    await tick();
    assert.equal(ready, true);

    const percents: number[] = [];
    const result = client.convert(file, true, false, { onProgress: (p) => percents.push(p) });
    await tick();
    assert.equal(worker.posted.length, 1);
    const message = worker.posted[0];
    assert.equal(message.type, 'convert');
    assert.equal(message.cityName, 'Springfield');
    assert.equal(message.fill, true);
    assert.equal(message.terrain, false);
    assert.deepEqual(Array.from(message.sc2), [1, 2, 3]);
    assert.equal(worker.transfers[0][0], message.sc2.buffer);

    const zip = new Uint8Array([9]);
    worker.emit({ type: 'progress', percent: 10 });
    worker.emit({ type: 'progress', percent: 60 });
    worker.emit({ type: 'done', zip });
    assert.equal(await result, zip);
    assert.deepEqual(percents, [10, 60]);
  });

  it('waits for ready before posting the convert message', async () => {
    const result = client.convert(file, true, true);
    await tick();
    assert.deepEqual(worker.posted, []);
    worker.emit({ type: 'ready' });
    await tick();
    assert.equal(worker.posted.length, 1);
    worker.emit({ type: 'done', zip: new Uint8Array() });
    await result;
  });

  it('rejects files over 16 MB without reading them', async () => {
    worker.emit({ type: 'ready' });
    const big = fakeFile('huge.sc2', [1], MAX_FILE_BYTES + 1);
    await assert.rejects(
      client.convert(big, true, true),
      { message: 'File is too large to be a SimCity 2000 city (max 16 MB)' },
    );
    assert.equal(big.reads, 0);
    assert.deepEqual(worker.posted, []);
  });

  it('forwards deterministic timestamp and seed', async () => {
    worker.emit({ type: 'ready' });
    const result = client.convert(file, true, true, { deterministic: { timestamp: 5, seed: 6 } });
    await tick();
    assert.equal(worker.posted[0].timestamp, 5);
    assert.equal(worker.posted[0].seed, 6);
    worker.emit({ type: 'done', zip: new Uint8Array() });
    await result;
  });

  it('sends no timestamp or seed by default', async () => {
    worker.emit({ type: 'ready' });
    const result = client.convert(file, true, true);
    await tick();
    assert.equal('timestamp' in worker.posted[0], false);
    assert.equal('seed' in worker.posted[0], false);
    worker.emit({ type: 'done', zip: new Uint8Array() });
    await result;
  });

  it('rejects on an error message and allows another conversion', async () => {
    worker.emit({ type: 'ready' });
    const failed = client.convert(file, true, true);
    await tick();
    worker.emit({ type: 'error', message: 'Not a SimCity 2000 city file' });
    await assert.rejects(failed, { message: 'Not a SimCity 2000 city file' });

    const next = client.convert(file, true, true);
    await tick();
    const zip = new Uint8Array([7]);
    worker.emit({ type: 'done', zip });
    assert.equal(await next, zip);
  });

  it('rejects a concurrent convert without disturbing the running one', async () => {
    worker.emit({ type: 'ready' });
    const first = client.convert(file, true, true);
    await assert.rejects(client.convert(file, true, true), /already running/);
    await tick();
    assert.equal(worker.posted.length, 1);
    const zip = new Uint8Array([1]);
    worker.emit({ type: 'done', zip });
    assert.equal(await first, zip);
  });

  it('rejects ready and later conversions when the worker fails before ready', async () => {
    const failed = client.ready;
    worker.fail('failed to load');
    await assert.rejects(failed, { message: 'failed to load' });
    await assert.rejects(client.convert(file, true, true), /worker has stopped/);
  });

  it('rejects the running job and every later convert once the worker crashed after ready', async () => {
    worker.emit({ type: 'ready' });
    const running = client.convert(file, true, true);
    await tick();
    worker.fail('boom');
    await assert.rejects(running, { message: 'boom' });
    await assert.rejects(client.convert(file, true, true), /worker has stopped/);
  });

  it('treats onmessageerror as a crash too', async () => {
    worker.emit({ type: 'ready' });
    worker.onmessageerror!({});
    await assert.rejects(client.convert(file, true, true), /worker has stopped/);
  });

  it('rejects with "Could not read the file" when arrayBuffer() rejects and allows another conversion', async () => {
    worker.emit({ type: 'ready' });
    const unreadable = { name: 'x.sc2', size: 3, arrayBuffer: async () => Promise.reject(new Error('disk gone')) };
    await assert.rejects(client.convert(unreadable, true, true), { message: 'Could not read the file: disk gone' });

    const next = client.convert(file, true, true);
    await tick();
    const zip = new Uint8Array([4]);
    worker.emit({ type: 'done', zip });
    assert.equal(await next, zip);
  });

  it('settles the promise and clears busy when postMessage throws', async () => {
    worker.emit({ type: 'ready' });
    worker.postMessage = () => {
      throw new Error('cannot clone');
    };
    await assert.rejects(client.convert(file, true, true), { message: 'Could not start the conversion: cannot clone' });

    worker.postMessage = FakeWorker.prototype.postMessage;
    const next = client.convert(file, true, true);
    await tick();
    const zip = new Uint8Array([5]);
    worker.emit({ type: 'done', zip });
    assert.equal(await next, zip);
  });

  it('a worker error before ready rejects ready and later conversions with that message', async () => {
    worker.emit({ type: 'error', message: 'boom' });
    await assert.rejects(client.ready, /boom/);
    await assert.rejects(client.convert(file, true, true), { message: 'boom' });
  });

  it('a rejected ready that nobody awaits does not raise unhandledRejection', async () => {
    let unhandled: unknown = null;
    const onUnhandled = (reason: unknown) => (unhandled = reason);
    process.once('unhandledRejection', onUnhandled);
    try {
      worker.fail('failed to load');
      await new Promise((resolve) => setImmediate(resolve));
    } finally {
      process.off('unhandledRejection', onUnhandled);
    }
    assert.equal(unhandled, null);
  });
});

describe('cityNameOf', () => {
  it('drops the extension and falls back to "city"', () => {
    assert.equal(cityNameOf({ name: 'Springfield.sc2' }), 'Springfield');
    assert.equal(cityNameOf({ name: 'a.b.sc2' }), 'a.b');
    assert.equal(cityNameOf({ name: 'noext' }), 'noext');
    assert.equal(cityNameOf({ name: '.sc2' }), 'city');
  });
});
