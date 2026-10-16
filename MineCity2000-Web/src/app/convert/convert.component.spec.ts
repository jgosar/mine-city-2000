import { TestBed } from '@angular/core/testing';
import { FakeWorker } from '../fake-worker.testing';
import { WORKER_FACTORY } from '../worker-bridge.service';
import { ConvertComponent } from './convert.component';

describe('ConvertComponent', () => {
  let worker: FakeWorker;
  let root: HTMLElement;
  let fixture: ReturnType<typeof TestBed.createComponent<ConvertComponent>>;
  const tick = () => new Promise((r) => setTimeout(r));
  const button = () => root.querySelector('button') as HTMLButtonElement;

  async function chooseFile(name = 'Town.sc2') {
    const input = root.querySelector('input[type=file]') as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: [new File([new Uint8Array([1])], name)], configurable: true });
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();
  }

  async function chooseFileAndConvert() {
    await chooseFile();
    button().click();
    await tick();
    await fixture.whenStable();
  }

  beforeEach(async () => {
    worker = new FakeWorker();
    await TestBed.configureTestingModule({
      imports: [ConvertComponent],
      providers: [{ provide: WORKER_FACTORY, useValue: () => worker.asWorker() }],
    }).compileComponents();
    fixture = TestBed.createComponent(ConvertComponent);
    root = fixture.nativeElement;
    await fixture.whenStable();
    worker.emit({ type: 'ready' });
  });

  it('keeps Convert disabled until a file is chosen', () => {
    expect(button().disabled).toBe(true);
  });

  it('disables Convert while converting and offers the zip afterwards', async () => {
    await chooseFileAndConvert();
    expect(button().disabled).toBe(true);
    expect(root.querySelector('progress')).not.toBeNull();

    worker.emit({ type: 'progress', percent: 40 });
    worker.emit({ type: 'done', zip: new Uint8Array([1, 2]) });
    await fixture.whenStable();
    expect(button().disabled).toBe(false);
    const link = root.querySelector('a.download') as HTMLAnchorElement;
    expect(link.getAttribute('download')).toBe('Town.zip');
  });

  it('shows the error text and re-enables Convert on error', async () => {
    await chooseFileAndConvert();
    worker.emit({ type: 'error', message: 'Not a SimCity 2000 city file' });
    await fixture.whenStable();
    expect(root.querySelector('.error')?.textContent).toContain('Not a SimCity 2000 city file');
    expect(button().disabled).toBe(false);
    expect(root.querySelector('a.download')).toBeNull();
  });

  it('revokes the previous download URL when a new file replaces a finished result', async () => {
    const revoke = vi.spyOn(URL, 'revokeObjectURL');
    await chooseFileAndConvert();
    worker.emit({ type: 'done', zip: new Uint8Array([1, 2]) });
    await fixture.whenStable();
    const url = (root.querySelector('a.download') as HTMLAnchorElement).href;
    await chooseFile('Other.sc2');
    expect(revoke).toHaveBeenCalledWith(url);
    expect(root.querySelector('a.download')).toBeNull();
  });
});

describe('ConvertComponent with a worker that failed to start', () => {
  it('keeps the start error visible when a file is chosen', async () => {
    const worker = new FakeWorker();
    await TestBed.configureTestingModule({
      imports: [ConvertComponent],
      providers: [{ provide: WORKER_FACTORY, useValue: () => worker.asWorker() }],
    }).compileComponents();
    const fixture = TestBed.createComponent(ConvertComponent);
    const root: HTMLElement = fixture.nativeElement;
    await fixture.whenStable();
    worker.onerror!({ message: 'failed to load' } as ErrorEvent);
    await fixture.whenStable();
    expect(root.querySelector('.error')?.textContent).toContain('failed to load');

    const input = root.querySelector('input[type=file]') as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: [new File([new Uint8Array([1])], 'Town.sc2')] });
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    expect(root.querySelector('.error')?.textContent).toContain('failed to load');
  });
});
