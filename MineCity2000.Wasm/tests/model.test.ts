// Unit tests for web/model.ts (pure page state): `npm test` in MineCity2000.Wasm.
import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { canConvert, errorText, initialState, reduce, statusText } from '../web/model.ts';
import type { Job, PageState, Ready } from '../web/model.ts';

const file = { name: 'city.sc2', size: 10 } as File;
const otherFile = { name: 'other.sc2', size: 20 } as File;
const idle: Job = { kind: 'idle' };
const converting: Job = { kind: 'converting', percent: null };
const done: Job = { kind: 'done', url: 'blob:x', name: 'city.zip', size: 1048576 };
const failed: Job = { kind: 'failed', message: 'Bad file.' };
const startError: Ready = { error: 'Worker crashed.' };

describe('statusText', () => {
  it('shows a loading message until the converter is ready', () => {
    assert.equal(statusText('loading', null, idle), 'Loading converter…');
    assert.equal(statusText('loading', file, idle), 'Loading converter…');
  });

  it('asks for a file once ready, then says it is ready to convert', () => {
    assert.equal(statusText('ready', null, idle), 'Choose a SimCity 2000 city file (.sc2).');
    assert.equal(statusText('ready', file, idle), 'Ready to convert.');
  });

  it('reports a worker start failure, even after a file is chosen', () => {
    assert.equal(statusText(startError, null, idle), 'The converter could not start.');
    assert.equal(statusText(startError, file, idle), 'The converter could not start.');
  });

  it('shows the building-models message while converting before the first progress', () => {
    assert.equal(
      statusText('ready', file, converting),
      'Loading building models (about 24 MB on the first run)…',
    );
  });

  it('shows the percentage once progress arrives, including 0', () => {
    assert.equal(statusText('ready', file, { kind: 'converting', percent: 0 }), 'Converting… 0%');
    assert.equal(statusText('ready', file, { kind: 'converting', percent: 42 }), 'Converting… 42%');
    assert.equal(statusText('ready', file, { kind: 'converting', percent: 100 }), 'Converting… 100%');
  });

  it('falls back to the loading message when converting without a ready worker', () => {
    assert.equal(statusText('loading', file, converting), 'Loading converter…');
    assert.equal(statusText(startError, file, converting), 'Loading converter…');
    assert.equal(statusText(startError, file, { kind: 'converting', percent: 7 }), 'Converting… 7%');
  });

  it('formats the finished size in MB with one decimal', () => {
    const sized = (size: number): Job => ({ kind: 'done', url: 'blob:x', name: 'city.zip', size });
    assert.equal(statusText('ready', file, sized(1048576)), 'Done: 1.0 MB');
    assert.equal(statusText('ready', file, sized(1572864)), 'Done: 1.5 MB');
    assert.equal(statusText('ready', file, sized(0)), 'Done: 0.0 MB');
    assert.equal(statusText('ready', file, sized(25 * 1048576 + 52429)), 'Done: 25.1 MB');
  });

  it('prefers done over the start-error and file prompts', () => {
    assert.equal(statusText(startError, file, done), 'Done: 1.0 MB');
    assert.equal(statusText('ready', null, done), 'Done: 1.0 MB');
  });

  it('does not mention a failed conversion in the status line', () => {
    assert.equal(statusText('ready', file, failed), 'Ready to convert.');
    assert.equal(statusText('ready', null, failed), 'Choose a SimCity 2000 city file (.sc2).');
    assert.equal(statusText('loading', file, failed), 'Loading converter…');
    assert.equal(statusText(startError, file, failed), 'The converter could not start.');
  });
});

describe('canConvert', () => {
  it('needs a file', () => {
    assert.equal(canConvert('ready', null, idle), false);
    assert.equal(canConvert('ready', file, idle), true);
  });

  it('is allowed while the worker is still loading (the click waits for it)', () => {
    assert.equal(canConvert('loading', file, idle), true);
    assert.equal(canConvert('loading', null, idle), false);
  });

  it('is disabled permanently when the worker could not start, even with a file', () => {
    assert.equal(canConvert(startError, null, idle), false);
    assert.equal(canConvert(startError, file, idle), false);
    assert.equal(canConvert(startError, file, failed), false);
    assert.equal(canConvert(startError, file, done), false);
  });

  it('is disabled while converting', () => {
    assert.equal(canConvert('ready', file, converting), false);
    assert.equal(canConvert('ready', file, { kind: 'converting', percent: 50 }), false);
  });

  it('is enabled again after a finished or failed conversion', () => {
    assert.equal(canConvert('ready', file, done), true);
    assert.equal(canConvert('ready', file, failed), true);
  });
});

describe('errorText', () => {
  it('is empty when nothing went wrong', () => {
    assert.equal(errorText('loading', idle), '');
    assert.equal(errorText('ready', idle), '');
    assert.equal(errorText('ready', converting), '');
    assert.equal(errorText('ready', done), '');
  });

  it('shows the conversion error', () => {
    assert.equal(errorText('ready', failed), 'Bad file.');
    assert.equal(errorText('loading', failed), 'Bad file.');
  });

  it('shows the worker start error', () => {
    assert.equal(errorText(startError, idle), 'Worker crashed.');
  });

  it('lets the worker start error win over a conversion error', () => {
    assert.equal(errorText(startError, failed), 'Worker crashed.');
  });
});

describe('reduce', () => {
  const withFile: PageState = { file, job: idle };

  it('starts with no file and an idle job', () => {
    assert.deepEqual(initialState, { file: null, job: { kind: 'idle' } });
  });

  it('choose sets the file and resets the job to idle', () => {
    assert.deepEqual(reduce(initialState, { type: 'choose', file }), { file, job: idle });
    assert.deepEqual(reduce({ file, job: failed }, { type: 'choose', file: otherFile }), {
      file: otherFile,
      job: idle,
    });
  });

  it('choose after done drops the download link', () => {
    const next = reduce({ file, job: done }, { type: 'choose', file: otherFile });
    assert.deepEqual(next, { file: otherFile, job: idle });
  });

  it('choose is ignored while converting (same state object)', () => {
    const state: PageState = { file, job: { kind: 'converting', percent: 30 } };
    assert.equal(reduce(state, { type: 'choose', file: otherFile }), state);
    assert.equal(reduce(state, { type: 'choose', file: null }), state);
  });

  it('start begins a conversion with unknown progress', () => {
    assert.deepEqual(reduce(withFile, { type: 'start' }), { file, job: converting });
  });

  it('start from done or failed begins a new conversion', () => {
    assert.deepEqual(reduce({ file, job: done }, { type: 'start' }), { file, job: converting });
    assert.deepEqual(reduce({ file, job: failed }, { type: 'start' }), { file, job: converting });
  });

  it('start is ignored while converting, so a second click is a no-op', () => {
    const state: PageState = { file, job: { kind: 'converting', percent: 30 } };
    assert.equal(reduce(state, { type: 'start' }), state);
  });

  it('start is ignored without a file', () => {
    assert.equal(reduce(initialState, { type: 'start' }), initialState);
  });

  it('progress, done and fail apply while converting', () => {
    const running: PageState = { file, job: converting };
    assert.deepEqual(reduce(running, { type: 'progress', percent: 55 }), {
      file,
      job: { kind: 'converting', percent: 55 },
    });
    assert.deepEqual(reduce(running, { type: 'done', url: 'blob:y', name: 'a.zip', size: 5 }), {
      file,
      job: { kind: 'done', url: 'blob:y', name: 'a.zip', size: 5 },
    });
    assert.deepEqual(reduce(running, { type: 'fail', message: 'Boom.' }), {
      file,
      job: { kind: 'failed', message: 'Boom.' },
    });
  });

  it('progress, done and fail are ignored outside converting (same state object)', () => {
    const states: PageState[] = [initialState, withFile, { file, job: done }, { file, job: failed }];
    for (const state of states) {
      assert.equal(reduce(state, { type: 'progress', percent: 1 }), state);
      assert.equal(reduce(state, { type: 'done', url: 'blob:z', name: 'z.zip', size: 1 }), state);
      assert.equal(reduce(state, { type: 'fail', message: 'Late.' }), state);
    }
  });

  it('never mutates its input', () => {
    const state: PageState = { file, job: { kind: 'converting', percent: 10 } };
    const snapshot = structuredClone({ job: state.job });
    reduce(state, { type: 'progress', percent: 20 });
    reduce(state, { type: 'done', url: 'blob:y', name: 'a.zip', size: 5 });
    reduce(state, { type: 'fail', message: 'Boom.' });
    reduce(state, { type: 'choose', file: otherFile });
    reduce(state, { type: 'start' });
    assert.equal(state.file, file);
    assert.deepEqual({ job: state.job }, snapshot);
  });
});
