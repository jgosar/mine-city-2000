import { TestBed } from '@angular/core/testing';
import { FakeWorker } from '../fake-worker';
import { WORKER_FACTORY } from '../worker-bridge.service';
import { ConvertComponent } from './convert.component';

describe('ConvertComponent', () => {
  let worker: FakeWorker;
  let root: HTMLElement;
  let fixture: ReturnType<typeof TestBed.createComponent<ConvertComponent>>;
  const tick = () => new Promise((r) => setTimeout(r));
  const button = () => root.querySelector('button') as HTMLButtonElement;

  async function chooseFileAndConvert() {
    const input = root.querySelector('input[type=file]') as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: [new File([new Uint8Array([1])], 'Town.sc2')] });
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();
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
});
