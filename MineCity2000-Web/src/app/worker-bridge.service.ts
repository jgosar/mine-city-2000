import { Injectable, InjectionToken, inject } from '@angular/core';
import { Observable, ReplaySubject, Subscriber, switchMap, take } from 'rxjs';

export type ConvertEvent = { percent: number } | { zip: Uint8Array };

/** Fixed timestamp and seed, so the output world is reproducible (verification only; the UI does not use it). */
export interface Deterministic {
  timestamp: number;
  seed: number;
}

/** Creates the module worker that hosts the .NET runtime. Replace in tests with a fake. */
export const WORKER_FACTORY = new InjectionToken<() => Worker>('WORKER_FACTORY', {
  providedIn: 'root',
  factory: () => () => new Worker(new URL('assets/worker.js', document.baseURI), { type: 'module' }),
});

@Injectable({ providedIn: 'root' })
export class WorkerBridgeService {
  private readonly ready = new ReplaySubject<void>(1);
  private isReady = false;
  private busy = false;
  private job: Subscriber<ConvertEvent> | null = null;

  /** Emits once when the .NET runtime is booted; errors if the worker cannot start. */
  readonly ready$: Observable<void> = this.ready.asObservable();

  private readonly worker: Worker;

  constructor() {
    this.worker = inject(WORKER_FACTORY)();
    this.worker.onmessage = ({ data }: MessageEvent) => this.handle(data);
    this.worker.onerror = (e: ErrorEvent) => this.fail(e.message || 'The converter worker failed to load');
    this.worker.onmessageerror = () => this.fail('The converter worker sent an unreadable message');
  }

  convert(file: File, fill: boolean, terrain: boolean, deterministic?: Deterministic): Observable<ConvertEvent> {
    return this.ready$.pipe(
      take(1),
      switchMap(
        () =>
          new Observable<ConvertEvent>((subscriber) => {
            if (this.busy) {
              subscriber.error(new Error('A conversion is already running'));
              return;
            }
            this.busy = true;
            this.job = subscriber;
            file.arrayBuffer().then(
              (buffer) => {
                if (this.job !== subscriber) return;
                const sc2 = new Uint8Array(buffer);
                this.worker.postMessage(
                  { type: 'convert', sc2, cityName: cityNameOf(file), fill, terrain, ...deterministic },
                  [sc2.buffer],
                );
              },
              (e) => this.fail(`Could not read the file: ${e?.message ?? e}`),
            );
            // Unsubscribing cannot cancel the worker; it stays busy until its next done/error.
            return () => {
              if (this.job === subscriber) this.job = null;
            };
          }),
      ),
    );
  }

  private handle(data: { type: string; percent?: number; zip?: Uint8Array; message?: string }): void {
    switch (data.type) {
      case 'ready':
        this.isReady = true;
        this.ready.next();
        break;
      case 'progress':
        this.job?.next({ percent: data.percent ?? 0 });
        break;
      case 'done':
        this.finish((job) => {
          job.next({ zip: data.zip! });
          job.complete();
        });
        break;
      case 'error':
        this.fail(data.message ?? 'Unknown converter error');
        break;
    }
  }

  private fail(message: string): void {
    const error = new Error(message);
    if (!this.isReady) this.ready.error(error);
    this.finish((job) => job.error(error));
  }

  private finish(action: (job: Subscriber<ConvertEvent>) => void): void {
    const job = this.job;
    this.job = null;
    this.busy = false;
    if (job) action(job);
  }
}

export function cityNameOf(file: File): string {
  return file.name.replace(/\.[^.]*$/, '') || 'city';
}
