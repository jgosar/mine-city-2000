# Open questions

Everything these pages tag **Inferred** or **Unknown**, with what has been tried. When one is settled, move the answer to the page it belongs on and delete it here.

## Container and compression

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Does the game clear the buffers of segments missing from a file before loading? | Unknown | The loader itself does not ([container.md](container.md#missing-repeated-and-unknown-segments)) | Read the callers of `0x4302e0` (new city / open city paths) |
| Is a non-IFF file of 27 248 bytes a SimCity Classic city? | Inferred | `0x482850` checks for exactly that size; Classic `.cty` files have that size | Read what `0x482850`'s caller does when it returns 1 |
| Does a decoding error abort the load? | Inferred | `0x430dc0` shows an error when input is left over; Ghidra does not show its return value clearly | Check the return register in the disassembly of `0x430dc0` |
| Character set of strings | Inferred | ANSI code page of the saving machine | Find a city saved on a non-Western system |

## Conventions

| Question | Status | What we know | How to settle it |
|---|---|---|---|
| Which screen direction do "row" and "col" run along? | Open | Rows are the outer index in memory and in the file | Per-tile segment pages (M1) |
| Which way does one compass step rotate the arrays? | Open | `0x465490` and `0x466660` rotate in opposite directions and step the compass | Per-tile segment pages (M1) |
| Overlay cell ↔ tile mapping (2 × 2 and 4 × 4) | Inferred | Grid sizes are confirmed | Minimap page (M2) |
| Signedness of MISC values | Inferred | Some are copied into 8- or 16-bit variables | MISC pages (M3) |
