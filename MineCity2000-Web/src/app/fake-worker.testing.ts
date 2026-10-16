/** Stand-in for the module worker: records posted messages and lets a test emit worker messages. */
export class FakeWorker {
  onmessage: ((e: MessageEvent) => void) | null = null;
  onerror: ((e: ErrorEvent) => void) | null = null;
  onmessageerror: ((e: MessageEvent) => void) | null = null;
  readonly posted: unknown[] = [];

  postMessage(message: unknown): void {
    this.posted.push(message);
  }

  emit(data: unknown): void {
    this.onmessage?.({ data } as MessageEvent);
  }

  asWorker(): Worker {
    return this as unknown as Worker;
  }
}
