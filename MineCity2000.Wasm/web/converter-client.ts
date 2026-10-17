// Promise API over the module worker that hosts the .NET runtime (message contract: see worker.js).

/** Real SimCity 2000 cities are well under 1 MB; anything this large is certainly the wrong file. */
export const MAX_FILE_BYTES = 16 * 1024 * 1024;

type WorkerMessage =
  | { type: 'ready' }
  | { type: 'progress'; percent: number }
  | { type: 'done'; zip: Uint8Array<ArrayBuffer> }
  | { type: 'error'; message: string };

/** Fixed timestamp and seed, so the output world is reproducible (verification only; the UI does not use it). */
export interface Deterministic {
  timestamp: number;
  seed: number;
}

export interface FileLike {
  name: string;
  size: number;
  arrayBuffer(): Promise<ArrayBuffer>;
}

export interface ConvertOptions {
  onProgress?: (percent: number) => void;
  deterministic?: Deterministic;
}

interface Job {
  resolve: (zip: Uint8Array<ArrayBuffer>) => void;
  reject: (error: Error) => void;
  onProgress?: (percent: number) => void;
}

/** The city name used for the world folder and the download: the file name without its extension. */
export function cityNameOf(file: { name: string }): string {
  return file.name.replace(/\.[^.]*$/, '') || 'city';
}

export class ConverterClient {
  #worker: Worker;
  #ready: Promise<void>;
  #resolveReady: () => void = () => {};
  #rejectReady: (error: Error) => void = () => {};
  #isReady = false;
  #busy = false;
  #dead = false;
  #job: Job | null = null;

  /** @param createWorker creates the module worker; replace in tests with a fake. */
  constructor(createWorker: () => Worker = () => new Worker(new URL('./worker.js', import.meta.url), { type: 'module' })) {
    this.#ready = new Promise<void>((resolve, reject) => {
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
  get ready(): Promise<void> {
    return this.#ready;
  }

  /** Resolves with the world zip (it arrives by structured clone of a worker message, so it is always backed by a plain ArrayBuffer). */
  async convert(
    file: FileLike,
    fill: boolean,
    terrain: boolean,
    { onProgress, deterministic }: ConvertOptions = {},
  ): Promise<Uint8Array<ArrayBuffer>> {
    if (file.size > MAX_FILE_BYTES) {
      throw new Error('File is too large to be a SimCity 2000 city (max 16 MB)');
    }
    if (this.#dead) throw new Error('The converter worker has stopped; reload the page');
    await this.#ready;
    if (this.#dead) throw new Error('The converter worker has stopped; reload the page');
    if (this.#busy) throw new Error('A conversion is already running');
    this.#busy = true;
    return new Promise<Uint8Array<ArrayBuffer>>((resolve, reject) => {
      const job: Job = { resolve, reject, onProgress };
      this.#job = job;
      const failJob = (e: unknown, what: string) => {
        if (this.#job === job) this.#fail(`${what}: ${e instanceof Error ? e.message : String(e)}`);
      };
      file.arrayBuffer().then(
        (buffer) => {
          if (this.#job !== job) return;
          try {
            const sc2 = new Uint8Array(buffer);
            this.#worker.postMessage(
              { type: 'convert', sc2, cityName: cityNameOf(file), fill, terrain, ...deterministic },
              [sc2.buffer],
            );
          } catch (e) {
            failJob(e, 'Could not start the conversion');
          }
        },
        (e) => failJob(e, 'Could not read the file'),
      );
    });
  }

  #handle(data: WorkerMessage): void {
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
  #crash(message: string): void {
    this.#dead = true;
    this.#fail(message);
  }

  #fail(message: string): void {
    const error = new Error(message);
    if (!this.#isReady) this.#rejectReady(error);
    this.#finish((job) => job.reject(error));
  }

  #finish(action: (job: Job) => void): void {
    const job = this.#job;
    this.#job = null;
    this.#busy = false;
    if (job) action(job);
  }
}
