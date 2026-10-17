// Pure page state: no DOM, no Preact. The components render from this and dispatch its actions.

export type Ready = 'loading' | 'ready' | { error: string };

export type Job =
  | { kind: 'idle' }
  /** percent is null until the converter reports its first progress (building models are still being loaded). */
  | { kind: 'converting'; percent: number | null }
  | { kind: 'done'; url: string; name: string; size: number }
  | { kind: 'failed'; message: string };

export interface PageState {
  file: File | null;
  job: Job;
}

export type Action =
  | { type: 'choose'; file: File | null }
  | { type: 'start' }
  | { type: 'progress'; percent: number }
  | { type: 'done'; url: string; name: string; size: number }
  | { type: 'fail'; message: string };

export const initialState: PageState = { file: null, job: { kind: 'idle' } };

/** Returns the same state object when an action does not apply, so callers can tell nothing changed. */
export function reduce(state: PageState, action: Action): PageState {
  const converting = state.job.kind === 'converting';
  switch (action.type) {
    case 'choose':
      return converting ? state : { file: action.file, job: { kind: 'idle' } };
    case 'start':
      return converting || state.file === null
        ? state
        : { file: state.file, job: { kind: 'converting', percent: null } };
    case 'progress':
      return converting ? { ...state, job: { kind: 'converting', percent: action.percent } } : state;
    case 'done':
      return converting
        ? { ...state, job: { kind: 'done', url: action.url, name: action.name, size: action.size } }
        : state;
    case 'fail':
      return converting ? { ...state, job: { kind: 'failed', message: action.message } } : state;
  }
}

function startError(ready: Ready): string {
  return typeof ready === 'object' ? ready.error : '';
}

export function statusText(ready: Ready, file: File | null, job: Job): string {
  if (job.kind === 'converting') {
    if (job.percent !== null) return `Converting… ${job.percent}%`;
    return ready === 'ready'
      ? 'Loading building models (about 24 MB on the first run)…'
      : 'Loading converter…';
  }
  if (job.kind === 'done') return `Done: ${(job.size / 1048576).toFixed(1)} MB`;
  if (typeof ready === 'object') return 'The converter could not start.';
  if (ready === 'loading') return 'Loading converter…';
  return file ? 'Ready to convert.' : 'Choose a SimCity 2000 city file (.sc2).';
}

export function canConvert(ready: Ready, file: File | null, job: Job): boolean {
  return file !== null && job.kind !== 'converting' && typeof ready !== 'object';
}

/** The worker-start error wins over a conversion error. */
export function errorText(ready: Ready, job: Job): string {
  return startError(ready) || (job.kind === 'failed' ? job.message : '');
}
