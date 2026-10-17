// The page. State: the converter's readiness, the chosen file and its job (web/model.ts) and the two options;
// everything shown is derived from them. Relative URLs only: the site is served under /mine-city-2000/ on GitHub Pages and under / locally.
import { render } from 'preact';
import type { ComponentChildren } from 'preact';
import { useEffect, useReducer, useState } from 'preact/hooks';
import { ConverterClient, cityNameOf } from './converter-client.ts';
import { DropZone } from './drop-zone.tsx';
import { canConvert, errorText, initialState, reduce, statusText } from './model.ts';
import type { Ready } from './model.ts';

interface Converter {
  client: ConverterClient | null;
  ready: Ready;
}

function messageOf(e: unknown): string {
  return e instanceof Error ? e.message : String(e);
}

function startFailed(e: unknown): Ready {
  return { error: messageOf(e) || 'The converter could not start.' };
}

function startConverter(): Converter {
  try {
    return { client: new ConverterClient(), ready: 'loading' };
  } catch (e) {
    return { client: null, ready: startFailed(e) };
  }
}

/** Creates the converter once (its worker boots the .NET runtime) and follows its start-up. */
function useConverterReady(): Converter {
  const [started] = useState(startConverter);
  const [ready, setReady] = useState(started.ready);
  useEffect(() => {
    started.client?.ready.then(
      () => setReady('ready'),
      (e) => setReady(startFailed(e)),
    );
  }, [started]);
  return { client: started.client, ready };
}

interface CheckboxProps {
  id: string;
  title: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  children: ComponentChildren;
}

function Checkbox({ id, title, checked, onChange, children }: CheckboxProps) {
  return (
    <label title={title}>
      <input type="checkbox" id={id} checked={checked} onChange={(event) => onChange(event.currentTarget.checked)} />{' '}
      {children}
    </label>
  );
}

function App() {
  const { client, ready } = useConverterReady();
  const [{ file, job }, dispatch] = useReducer(reduce, initialState);
  const [fill, setFill] = useState(false);
  const [terrain, setTerrain] = useState(false);

  // The download's object URL lives as long as its done job: revoked on a new file, a new conversion or unmount.
  useEffect(() => (job.kind === 'done' ? () => URL.revokeObjectURL(job.url) : undefined), [job]);

  async function convert() {
    if (!client || !file || !canConvert(ready, file, job)) return;
    if (job.kind === 'converting') return; // the disabled button normally prevents this; the guard covers two clicks in the same task
    dispatch({ type: 'start' });
    try {
      const zip = await client.convert(file, fill, terrain, {
        onProgress: (percent) => dispatch({ type: 'progress', percent }),
      });
      const blob = new Blob([zip], { type: 'application/zip' });
      dispatch({ type: 'done', url: URL.createObjectURL(blob), name: `${cityNameOf(file)}.zip`, size: blob.size });
    } catch (e) {
      dispatch({ type: 'fail', message: messageOf(e) });
    }
  }

  const error = errorText(ready, job);
  const download = job.kind === 'done' ? job : null;

  return (
    <main>
      <h1>MineCity 2000</h1>
      <p>
        Turn a SimCity 2000 city into a Minecraft world. The conversion runs entirely in your browser; your file is
        never uploaded. <a href="https://github.com/jgosar/mine-city-2000">Source on GitHub</a>
      </p>

      <DropZone
        label={file ? file.name : 'Drop a .sc2 file here'}
        disabled={job.kind === 'converting'}
        onFile={(chosen) => dispatch({ type: 'choose', file: chosen })}
      />

      <div class="options">
        <Checkbox
          id="fill"
          title="The area under the city is filled with sandstone instead of left empty, so you cannot fall through when digging down."
          checked={fill}
          onChange={setFill}
        >
          Fill underground area
        </Checkbox>
        <Checkbox
          id="terrain"
          title="Minecraft generates its normal terrain outside of the city."
          checked={terrain}
          onChange={setTerrain}
        >
          Generate terrain around the city
        </Checkbox>
      </div>

      <button type="button" id="convert" disabled={!canConvert(ready, file, job)} onClick={convert}>
        Convert
      </button>

      {/* Without a value the bar is indeterminate: building models are still being loaded. */}
      <progress
        id="progress"
        max={100}
        hidden={job.kind !== 'converting'}
        value={job.kind === 'converting' ? (job.percent ?? undefined) : undefined}
      />
      <p class="status" id="status" role="status">
        {statusText(ready, file, job)}
      </p>
      <p class="error" id="error" role="alert" hidden={!error}>
        {error}
      </p>
      <a class="download" id="download" hidden={!download} href={download?.url} download={download?.name}>
        {download ? `Download ${download.name}` : ''}
      </a>
    </main>
  );
}

render(<App />, document.getElementById('app')!);
