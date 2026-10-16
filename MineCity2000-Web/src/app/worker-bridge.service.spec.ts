import { TestBed } from '@angular/core/testing';
import { firstValueFrom, toArray } from 'rxjs';
import { FakeWorker } from './fake-worker';
import { WORKER_FACTORY, WorkerBridgeService } from './worker-bridge.service';

describe('WorkerBridgeService', () => {
  let worker: FakeWorker;
  let service: WorkerBridgeService;
  const file = new File([new Uint8Array([1, 2, 3])], 'Springfield.sc2');
  const tick = () => new Promise((r) => setTimeout(r));

  beforeEach(() => {
    worker = new FakeWorker();
    TestBed.configureTestingModule({ providers: [{ provide: WORKER_FACTORY, useValue: () => worker.asWorker() }] });
    service = TestBed.inject(WorkerBridgeService);
  });

  it('emits ready, then progress values, then the zip', async () => {
    let ready = false;
    service.ready$.subscribe(() => (ready = true));
    worker.emit({ type: 'ready' });
    expect(ready).toBe(true);

    const events = firstValueFrom(service.convert(file, true, false).pipe(toArray()));
    await tick();
    expect(worker.posted[0]).toMatchObject({ type: 'convert', cityName: 'Springfield', fill: true, terrain: false });
    expect(Array.from(worker.posted[0].sc2)).toEqual([1, 2, 3]);

    const zip = new Uint8Array([9]);
    worker.emit({ type: 'progress', percent: 10 });
    worker.emit({ type: 'progress', percent: 60 });
    worker.emit({ type: 'done', zip });
    expect(await events).toEqual([{ percent: 10 }, { percent: 60 }, { zip }]);
  });

  it('waits for ready before posting the convert message', async () => {
    const events = firstValueFrom(service.convert(file, true, true).pipe(toArray()));
    await tick();
    expect(worker.posted).toEqual([]);
    worker.emit({ type: 'ready' });
    await tick();
    expect(worker.posted.length).toBe(1);
    worker.emit({ type: 'done', zip: new Uint8Array() });
    await events;
  });

  it('forwards deterministic timestamp and seed', async () => {
    worker.emit({ type: 'ready' });
    const events = firstValueFrom(service.convert(file, true, true, { timestamp: 5, seed: 6 }).pipe(toArray()));
    await tick();
    expect(worker.posted[0]).toMatchObject({ timestamp: 5, seed: 6 });
    worker.emit({ type: 'done', zip: new Uint8Array() });
    await events;
  });

  it('errors the observable on an error message and allows another conversion', async () => {
    worker.emit({ type: 'ready' });
    const failed = firstValueFrom(service.convert(file, true, true));
    await tick();
    worker.emit({ type: 'error', message: 'Not a SimCity 2000 city file' });
    await expect(failed).rejects.toThrow('Not a SimCity 2000 city file');

    const next = firstValueFrom(service.convert(file, true, true).pipe(toArray()));
    await tick();
    worker.emit({ type: 'done', zip: new Uint8Array() });
    expect(await next).toHaveLength(1);
  });

  it('surfaces worker.onerror before ready through ready$', async () => {
    const failed = firstValueFrom(service.ready$);
    worker.onerror!({ message: 'failed to load' } as ErrorEvent);
    await expect(failed).rejects.toThrow('failed to load');
  });
});
