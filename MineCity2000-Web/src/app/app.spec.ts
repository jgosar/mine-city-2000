import { TestBed } from '@angular/core/testing';
import { FakeWorker } from './fake-worker';
import { App } from './app';
import { WORKER_FACTORY } from './worker-bridge.service';

describe('App', () => {
  it('renders the title', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: WORKER_FACTORY, useValue: () => new FakeWorker().asWorker() }],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toBe('MineCity 2000');
  });
});
