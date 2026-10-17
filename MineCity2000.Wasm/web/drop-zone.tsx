// The drop target with its file picker; reports a chosen or dropped file through onFile.
import { useRef, useState } from 'preact/hooks';

interface DropZoneProps {
  label: string;
  disabled: boolean;
  onFile: (file: File) => void;
}

export function DropZone({ label, disabled, onFile }: DropZoneProps) {
  // Drag events also fire for the zone's children; the highlight stays on until every enter has a matching leave.
  const depth = useRef(0);
  const [dragging, setDragging] = useState(false);

  // A cleared file input or a drop without files chooses nothing.
  const choose = (file: File | undefined) => {
    if (file) onFile(file);
  };

  return (
    <div
      class={dragging ? 'drop-zone dragging' : 'drop-zone'}
      id="drop-zone"
      onDragEnter={(event) => {
        event.preventDefault();
        depth.current++;
        setDragging(true);
      }}
      // Without preventDefault here the browser never fires drop.
      onDragOver={(event) => {
        event.preventDefault();
        setDragging(true);
      }}
      onDragLeave={() => {
        depth.current = Math.max(0, depth.current - 1);
        if (depth.current === 0) setDragging(false);
      }}
      onDrop={(event) => {
        event.preventDefault();
        depth.current = 0;
        setDragging(false);
        choose(event.dataTransfer?.files[0]);
      }}
    >
      <p id="file-name">{label}</p>
      <label>
        or choose a file{' '}
        <input
          type="file"
          id="file-input"
          accept=".sc2"
          disabled={disabled}
          onChange={(event) => choose(event.currentTarget.files?.[0])}
        />
      </label>
    </div>
  );
}
