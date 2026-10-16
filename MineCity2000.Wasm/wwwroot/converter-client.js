// @ts-check
// Promise API over the module worker that hosts the .NET runtime (message contract: see worker.js).

/** Real SimCity 2000 cities are well under 1 MB; anything this large is certainly the wrong file. */
export const MAX_FILE_BYTES = 16 * 1024 * 1024;

/**
 * @typedef {{ type: 'ready' }
 *   | { type: 'progress', percent: number }
 *   | { type: 'done', zip: Uint8Array }
 *   | { type: 'error', message: string }} WorkerMessage
 * @typedef {{ timestamp: number, seed: number }} Deterministic Fixed timestamp and seed, so the output world is reproducible (verification only; the UI does not use it).
 * @typedef {{ name: string, size: number, arrayBuffer(): Promise<ArrayBuffer> }} FileLike
 * @typedef {{ resolve: (zip: Uint8Array) => void, reject: (error: Error) => void, onProgress?: (percent: number) => void }} Job
 */

/**
 * The city name used for the world folder and the download: the file name without its extension.
 * @param {{ name: string }} file
 */
export function cityNameOf(file) {
  return file.name.replace(/\.[^.]*$/, '') || 'city';
}

export class ConverterClient {
  #worker;
  #ready;
  #resolveReady = () => {};
  #rejectReady = (/** @type {Error} */ _error) => {};
  #isReady = false;
  #busy = false;
  #dead = false;
  /** @type {Job | null} */
  #job = null;

  /**
   * @param {() => Worker} createWorker creates the module worker; replace in tests with a fake.
   */
  constructor(createWorker = () => new Worker(new URL('./worker.js', import.meta.url), { type: 'module' })) {
    this.#ready = new Promise((resolve, reject) => {
      this.#resolveReady = resolve;
      this.#rejectReady = reject;
    });
    // Callers that never look at `ready` must not trigger an unhandled rejection.
    this.#ready.catch(() => {});
    this.#worker = createWorker();
    this.#worker.onmessage = ({ data }) => this.#handle(data);
    this.#worker.onerror = (e) => this.#crash(e.message || 'The converter worker failed to load');
    this.#worker.onmessageerror = () => this.#crash('The converter worker sent an unreadable message');
  }

  /** Resolves once the .NET runtime is booted; rejects if the worker cannot start. */
  get ready() {
    return this.#ready;
  }

  /**
   * @param {FileLike} file
   * @param {boolean} fill
   * @param {boolean} terrain
   * @param {{ onProgress?: (percent: number) => void, deterministic?: Deterministic }} [options]
   * @returns {Promise<Uint8Array>} the world zip
   */
  async convert(file, fill, terrain, { onProgress, deterministic } = {}) {
    if (file.size > MAX_FILE_BYTES) {
      throw new Error('File is too large to be a SimCity 2000 city (max 16 MB)');
    }
    if (this.#dead) throw new Error('The converter worker has stopped; reload the page');
    await this.#ready;
    if (this.#dead) throw new Error('The converter worker has stopped; reload the page');
    if (this.#busy) throw new Error('A conversion is already running');
    this.#busy = true;
    return new Promise((resolve, reject) => {
      /** @type {Job} */
      const job = { resolve, reject, onProgress };
      this.#job = job;
      file.arrayBuffer().then(
        (buffer) => {
          if (this.#job !== job) return;
          const sc2 = new Uint8Array(buffer);
          this.#worker.postMessage(
            { type: 'convert', sc2, cityName: cityNameOf(file), fill, terrain, ...deterministic },
            [sc2.buffer],
          );
        },
        (e) => {
          if (this.#job === job) this.#fail(`Could not read the file: ${e?.message ?? e}`);
        },
      );
    });
  }

  /** @param {WorkerMessage} data */
  #handle(data) {
    switch (data.type) {
      case 'ready':
        this.#isReady = true;
        this.#resolveReady();
        break;
      case 'progress':
        this.#job?.onProgress?.(data.percent);
        break;
      case 'done':
        this.#finish((job) => job.resolve(data.zip));
        break;
      case 'error':
        this.#fail(data.message);
        break;
    }
  }

  /** The worker itself is gone or unusable: fail the current job and refuse all later ones. */
  #crash(/** @type {string} */ message) {
    this.#dead = true;
    this.#fail(message);
  }

  #fail(/** @type {string} */ message) {
    const error = new Error(message);
    if (!this.#isReady) this.#rejectReady(error);
    this.#finish((job) => job.reject(error));
  }

  /** @param {(job: Job) => void} action */
  #finish(action) {
    const job = this.#job;
    this.#job = null;
    this.#busy = false;
    if (job) action(job);
  }
}
