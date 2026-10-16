import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { WorkerBridgeService, cityNameOf } from '../worker-bridge.service';

@Component({
  selector: 'app-convert',
  templateUrl: './convert.component.html',
  styleUrl: './convert.component.css',
})
export class ConvertComponent {
  private readonly bridge = inject(WorkerBridgeService);

  protected readonly file = signal<File | null>(null);
  protected readonly fill = signal(false);
  protected readonly terrain = signal(false);
  protected readonly workerReady = signal(false);
  protected readonly converting = signal(false);
  /** null until the converter reports its first progress (building models are still being loaded). */
  protected readonly percent = signal<number | null>(null);
  private readonly workerError = signal('');
  private readonly conversionError = signal('');
  protected readonly error = computed(() => this.workerError() || this.conversionError());
  protected readonly dragging = signal(false);
  protected readonly downloadUrl = signal('');
  protected readonly downloadName = signal('');
  protected readonly downloadSize = signal(0);

  protected readonly canConvert = computed(
    () => this.file() !== null && !this.converting() && !this.workerError(),
  );
  protected readonly status = computed(() => {
    if (this.converting()) {
      const percent = this.percent();
      if (percent !== null) return `Converting… ${percent}%`;
      return this.workerReady()
        ? 'Loading building models (about 24 MB on the first run)…'
        : 'Loading converter…';
    }
    if (this.downloadUrl()) return `Done: ${(this.downloadSize() / 1048576).toFixed(1)} MB`;
    if (this.workerError()) return 'The converter could not start.';
    if (!this.workerReady()) return 'Loading converter…';
    return this.file() ? 'Ready to convert.' : 'Choose a SimCity 2000 city file (.sc2).';
  });

  constructor() {
    this.bridge.ready$.subscribe({
      next: () => this.workerReady.set(true),
      error: (e: Error) => this.workerError.set(e.message),
    });
    inject(DestroyRef).onDestroy(() => this.revokeDownload());
  }

  protected onFileInput(event: Event): void {
    this.setFile((event.target as HTMLInputElement).files?.[0] ?? null);
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    this.setFile(event.dataTransfer?.files?.[0] ?? null);
  }

  protected convert(): void {
    const file = this.file();
    if (!file || this.converting()) return;
    this.revokeDownload();
    this.conversionError.set('');
    this.percent.set(null);
    this.converting.set(true);
    this.bridge.convert(file, this.fill(), this.terrain()).subscribe({
      next: (event) => {
        if ('percent' in event) {
          this.percent.set(event.percent);
        } else {
          const blob = new Blob([event.zip], { type: 'application/zip' });
          this.downloadUrl.set(URL.createObjectURL(blob));
          this.downloadName.set(`${cityNameOf(file)}.zip`);
          this.downloadSize.set(blob.size);
        }
      },
      error: (e: Error) => {
        this.conversionError.set(e.message);
        this.converting.set(false);
      },
      complete: () => this.converting.set(false),
    });
  }

  private setFile(file: File | null): void {
    if (!file || this.converting()) return;
    this.file.set(file);
    this.conversionError.set('');
    this.revokeDownload();
  }

  private revokeDownload(): void {
    const url = this.downloadUrl();
    if (url) URL.revokeObjectURL(url);
    this.downloadUrl.set('');
  }
}
