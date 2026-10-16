# MineCity 2000 Modernization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Each PR below is one branch; execute PRs in order, each on top of the merged previous one.

**Goal:** Make MineCity 2000 build, test and run identically on Windows and macOS, cut its memory use ~20x, and replace the MAUI GUI with a browser-only Angular + .NET WebAssembly web app, all protected by an automated test suite created before any refactoring.

**Architecture:** The four pure-C# projects (SimCityReader, AnvilFile, MinecraftEditor, MineCity2000) stay the conversion engine. A new xUnit project pins their behaviour with unit tests plus a golden-master test against a committed city and baseline world. Only then does the block storage get flattened. Finally the engine is compiled to WebAssembly and driven from an Angular page; the MAUI GUI is removed last.

**Tech Stack:** C# / .NET 8 (bumped to .NET 10 in PR 6), xUnit, GitHub Actions, .NET WebAssembly Browser App (`Microsoft.NET.Sdk.WebAssembly`, `[JSExport]`, runtime hosted in a Web Worker), a plain HTML/CSS/JS page (Angular was tried first in PR 6 and replaced in PR 6b, see below), GitHub Pages.

**PR sequence at a glance:**
| PR | Title | Depends on | Size |
|---|---|---|---|
| 1 | Cross-platform CLI (paths, net8.0, default_args) | — | S |
| 2 | Determinism + byte-level seams | 1 | S |
| 3 | Test project, golden baseline, CI on 3 OSes | 2 | M |
| 4 | Built-in zlib instead of zlib.net.dll | 3 | S |
| 5 | Flat section storage (memory ~20x down) | 4 | M |
| 6 | Web UI: .NET 10, CLI/lib split, WASM worker, Angular, Pages | 5 | L |
| 7 | Remove MAUI, CI release zips, README | 6 | S |

**Spec:** This plan was derived from the investigation in the conversation of 2026-10-15 (no separate spec file). Key verified facts are recorded in the Context section so executors need nothing else.

## Context

- The repo has never built outside Windows. All five csproj files target `net8.0-windows10.0.19041.0`; on macOS `dotnet build` fails with NETSDK1100 unless `-p:EnableWindowsTargeting=true` is passed. None of the four engine projects use a Windows API, so the Windows TFM is unnecessary for them.
- Paths are built with literal `\\` in 14 places (Program.cs, Reader.cs, BuildingModel.cs, SCMapper.cs, AbstractRegion.cs, AbstractWorld.cs, MainPage.xaml.cs). On macOS this produces files literally named `test1\region\r.1.4.mca` and, worse, `File.Exists(folder + "\\" + code + ".mca")` is always false so **no buildings are placed, silently**.
- `default_args.txt` is split on `"\r\n"` (breaks with LF endings) and then on spaces (breaks any path containing a space, e.g. macOS `~/Library/Application Support/minecraft/saves`).
- Output is deterministic except: bytes 4096–8191 of every region file (chunk timestamp table, from `DateTime.UtcNow` in MCAWriter) and `LastPlayed` + `RandomSeed` in level.dat. Verified by running twice and diffing all 33 files.
- Memory: one 16×16×16 section is stored as `byte[16][16][16][2]` = 4369 heap objects ≈ 169 KB for 6 KB of payload. Measured: default conversion 12–16 s / 0.7 GB RSS; fill-underground 117 s / 3.5 GB. `AbstractRegion.getBlockData` allocates a full 169 KB section on reads of empty sections and discards it.
- Fixture: `~/Downloads/test.sc2` (99 KB), owner-made, covers 165 building codes, roads, rails, power lines (incl. sloped), tunnel entrances 63–66, bridge parts, hydro 198 & 199, rotatable 221/223, water, altitude 2–10. It converts cleanly with the current code.
- Dev Mac: arm64, no Xcode (CLT only), .NET 8 SDK at `~/.dotnet` (not on PATH: `export PATH="$HOME/.dotnet:$PATH"`). No Minecraft installed.
- Decisions taken with the owner: Angular front end + .NET WASM library; remove MAUI only after the web UI ships; add CI (3 OSes) and a CI release workflow replacing `release.bat`.

## Global Constraints

- Engine projects target plain `net8.0` from PR 1 until PR 6 bumps everything to `net10.0`.
- Windows behaviour and output must stay byte-identical through PR 1–3 (verified by the golden test running on the Windows CI runner).
- Generated worlds must stay compatible with Minecraft Java 1.3.2+ (Anvil format, `version` 19133 in level.dat) — no changes to NBT structure.
- No new NuGet dependencies in the engine projects (keeps the WASM payload small and trimming simple). Test project may use xUnit only.
- Keep the existing code style (Java-style lowerCamel method names, 2-space indent in MineCity2000/SimCityReader, 4-space in MinecraftEditor/AnvilFile). Do not reformat untouched code.
- Nullable warnings: do not add new ones; fixing existing ones is out of scope.
- Commit messages: imperative, prefixed `PR<n>:` is NOT required; use conventional style `fix:`, `feat:`, `test:`, `refactor:`, `ci:`, `docs:`.

## Review Focus

1. A `.sc2` whose embedded CNAM name is empty or whitespace-only → `LevelName` must fall back to the file name without extension (the world folder already comes from the file name) (tests in PR 3, `CityDataTests.cityName_*_fallsBackToFileName`). Non-printable "garbage" names are not detected; revisit in PR 6, where browser uploads make odd files likelier.
2. An output path containing spaces on any OS → must be used verbatim (test in PR 1 `DefaultArgsTests.value_with_spaces_is_kept`).
3. A region file produced on Windows and one produced on macOS from the same city → identical decompressed chunk NBT (golden test runs on all 3 CI OSes in PR 3).
4. A building model `.mca` with a section containing only data nibbles (block id 0, data ≠ 0) → must still be loaded as an existing section (PR 5 `BuildingModelTests.allModels_matchBaselineHashes` covers all 160 real models; the verbatim existence check in `BuildingModel` plus that hash baseline are the guard).
5. Browser upload of a non-`.sc2` file or a truncated file → clear error message, no hung progress bar (PR 6 `convert_rejectsInvalidFile` test + manual check).

---

## PR 1: Cross-platform CLI (paths, target framework, default_args)

**Branch:** `pr1-cross-platform-cli`
**Outcome:** `dotnet build` and `dotnet run` work on macOS with no flags; Windows behaviour unchanged; buildings are placed on macOS.

**Files:**
- Modify: `AnvilFile/AnvilFile.csproj`, `SimCityReader/SimCityReader.csproj`, `MinecraftEditor/MinecraftEditor.csproj`, `MineCity2000/MineCity2000.csproj` — `<TargetFramework>net8.0</TargetFramework>`
- Modify: `MineCity2000/Program.cs` (parseArgs, default_args parsing, working dir)
- Modify: `MineCity2000/BuildingModel.cs:24,41`, `MineCity2000/SCMapper.cs:720,725`, `MinecraftEditor/AbstractRegion.cs:161`, `MinecraftEditor/AbstractWorld.cs:52,58`, `SimCityReader/Reader.cs:14`, `SimCityReader/Program.cs:21`
- Modify: `MineCity2000-GUI/MainPage.xaml.cs:62-67,98,101` (same Path.Combine treatment; project keeps its Windows TFM)
- Modify: `MineCity2000/default_args.txt`, `.vscode/launch.json` (CLI program path becomes `.../net8.0/MineCity2000.dll` run via `dotnet`), `.gitignore` (add `MineCity2000/generated`)
- Create: `MineCity2000/DefaultArgs.cs` (small parser, so it can be unit-tested in PR 3)

**Interfaces:**
- Produces: `public static class DefaultArgs { public static string[] parse(string text); }` — returns a flat argv-style array, one option per line, value = rest of line after first space, lines starting with `#` or blank ignored, any line ending.
- Produces: `public static string? defaultSavesDir()` in `DefaultArgs` — platform Minecraft saves folder if it exists, else null.

- [ ] **Step 1: Retarget the four engine csproj files** to `net8.0`. Build on macOS: `export PATH="$HOME/.dotnet:$PATH"; dotnet build MineCity2000/MineCity2000.csproj` → expect 0 errors (the 36 nullable warnings remain). Check `Microsoft.Windows.SDK.NET.dll` and `WinRT.Runtime.dll` are no longer in `MineCity2000/bin/Debug/net8.0/`.

- [ ] **Step 2: Replace every `"\\"` path concatenation** with `Path.Combine`. Pattern (apply at each listed line):

```csharp
// before
MCAWriter.writeToFile(chunks, outputFolder + "\\" + fileName);
// after
MCAWriter.writeToFile(chunks, Path.Combine(outputFolder, fileName));
```
City-name extraction (Program.cs:61, Reader.cs:14, MainPage.xaml.cs:101):
```csharp
String cityName = Path.GetFileNameWithoutExtension(inputFile).ToLower();
```
`Reader.cs` keeps the `.SC2`-insensitive behaviour automatically because `GetFileNameWithoutExtension` strips any extension. `SimCityReader/Program.cs:21` is a dev-only scratch program; just make the literal use `Path.Combine("C:", "Program Files", ...)` or leave the string (it is a Windows-only sample path) — leave the string, it is never executed by users.

- [ ] **Step 3: Working directory** — in Program.cs and MainPage.xaml.cs replace
```csharp
String workingDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
```
with `String workingDir = AppContext.BaseDirectory;` (works for single-file publish too).

- [ ] **Step 4: Create `MineCity2000/DefaultArgs.cs`:**
```csharp
using System.Runtime.InteropServices;

namespace com.mc2k.MineCity2000
{
  public static class DefaultArgs
  {
    // One option per line: "-o /path with spaces" -> ["-o", "/path with spaces"]
    public static string[] parse(string text)
    {
      List<string> result = new List<string>();
      foreach (string rawLine in text.Split('\n'))
      {
        string line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith("#")) continue;
        int space = line.IndexOf(' ');
        if (space < 0) { result.Add(line); continue; }
        result.Add(line.Substring(0, space));
        result.Add(line.Substring(space + 1).Trim());
      }
      return result.ToArray();
    }

    public static string? defaultSavesDir()
    {
      string candidate;
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "saves");
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "minecraft", "saves");
      else
        candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".minecraft", "saves");
      return Directory.Exists(candidate) ? candidate : null;
    }
  }
}
```

- [ ] **Step 5: Use it in `Program.parseArgs`:** replace the `defaultArgsText.Split("\r\n")...` line with
```csharp
String defaultArgsPath = Path.Combine(workingDir, "default_args.txt");
String[] defaultArgs = File.Exists(defaultArgsPath) ? DefaultArgs.parse(File.ReadAllText(defaultArgsPath)) : [];
```
and the output-dir default with
```csharp
buildingsDir = Path.Combine(workingDir, "buildings");
outputDir = DefaultArgs.defaultSavesDir() ?? Path.Combine(workingDir, "generated");
if (outputArg != null) outputDir = outputArg[0];
outputDir = Path.Combine(outputDir, cityName);
```

- [ ] **Step 6: Rewrite `default_args.txt`** (keep CRLF or LF, both now work):
```
# Options in this file are added to every MineCity2000 call. One option per line, e.g.:
#   -o C:\Users\you\AppData\Roaming\.minecraft\saves
# If no -o is given, the Minecraft saves folder of the current user is used when it exists,
# otherwise a "generated" folder next to the program.
```

- [ ] **Step 7: Verify on macOS end-to-end.** `dotnet run --project MineCity2000 -- ~/Downloads/test.sc2 -o /tmp/mc2k-pr1 -c`. Expect `/tmp/mc2k-pr1/test/level.dat` plus `/tmp/mc2k-pr1/test/region/` with 32 files (16 city regions `r.0..3.0..3.mca` + 16 border regions with index -1 or 4). Confirm buildings were placed: before the fix the largest city region file on this Mac was exactly 4,202,496 bytes (terrain only); after the fix at least one city region must be larger, and `MCAReader.readFile` on `r.1.1.mca` must yield a chunk whose section 2–4 `Blocks` array contains ids other than 0, 9 and 24 (a quick throwaway `dotnet run` snippet or the PR 3 tests will do).

- [ ] **Step 8: Verify on Windows (owner):** build + run the same command; output folder layout identical to before.

- [ ] **Step 9: Commit** `fix: build and run the CLI on macOS (Path.Combine, net8.0, default_args parsing)`.

---

## PR 2: Determinism and byte-level seams (testability prep)

**Branch:** `pr2-testability-seams`
**Outcome:** Output is 100% reproducible when a timestamp and seed are supplied; readers accept byte arrays. No behaviour change for users (defaults are still "now" and random).

**Files:**
- Modify: `MineCity2000/MapperOptions.cs`, `MinecraftEditor/MCAWriter.cs`, `MinecraftEditor/LevelDat.cs`, `MinecraftEditor/AbstractRegion.cs` (saveToFolder signature), `MineCity2000/SCMapper.cs` (createLevelDat, saveRegion), `MinecraftEditor/MCAReader.cs`, `SimCityReader/Reader.cs`
- Modify: `MineCity2000/Program.cs` — new CLI flags `--timestamp <unixSeconds>` and `--seed <long>` (hidden from help text? No: document them in one line as "for reproducible output / testing").

**Interfaces (Produces):**
```csharp
// MapperOptions
public MapperOptions(bool fillUnderground, bool generateTerrain, long? timestamp = null, long? randomSeed = null)
public long timestamp { get; }   // unix seconds; resolved at construction: timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
public long randomSeed { get; }  // resolved: randomSeed ?? new Random().Next()
// MCAWriter
public static byte[] toBytes(List<Chunk> chunks, int timestamp)
public static void writeToFile(List<Chunk> chunks, String fileName, int timestamp)   // old 2-arg overload delegates with now
// MCAReader
public static List<Chunk> read(byte[] fileBytes)      // readFile(path) delegates
// LevelDat
public LevelDat(String levelName, double[] playerPosition, Boolean generateTerrain, long lastPlayedSeconds, long randomSeed)  // old ctor delegates with now/random
public byte[] toBytes()   // gzip-compressed NBT; saveToFile delegates
// Reader
public static CityData read(byte[] fileBytes, String cityName)   // readFile(path) delegates
// AbstractRegion
public void saveToFolder(String outputFolder, int timestamp)   // old overload delegates
```

- [ ] **Step 1:** Add the properties to `MapperOptions` (read the file first; it is a tiny class with two public bools).
- [ ] **Step 2:** `MCAWriter`: move the body of `writeToFile` into `toBytes(chunks, timestamp)` returning `output.ToArray()`; `writeToFile(chunks, fileName, timestamp)` = `File.WriteAllBytes(fileName, toBytes(chunks, timestamp))`; keep 2-arg overload computing `(int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()`.
- [ ] **Step 3:** `LevelDat`: store `_lastPlayedSeconds`, `_randomSeed`; `toNBTTag()` uses them instead of `DateTime.UtcNow`/`new Random()`. `toBytes()` = gzip of `toNBTTag().toBytesWithHeader()` into a MemoryStream; `saveToFile` writes `toBytes()`.
- [ ] **Step 4:** `SCMapper.createLevelDat` passes `options.timestamp`, `options.randomSeed`; `saveRegion(region, outputDir, (int)options.timestamp)`; `createBorderRegions` gets `options` parameter for the same reason.
- [ ] **Step 5:** `MCAReader.read(byte[])` / `Reader.read(byte[], cityName)` extracted; path versions delegate.
- [ ] **Step 6:** CLI flags: in `parseArgs`, `parseArg(["--timestamp"], 1, allArgs)` and `parseArg(["--seed"], 1, allArgs)` → `long.Parse`, passed into `MapperOptions`. Add to `helpText`:
```
--timestamp <unix seconds>, --seed <number>
            Fix the timestamps and random seed written into the world, so that two runs produce identical files (used by the tests).
```
- [ ] **Step 7: Verify determinism manually:** run twice with `--timestamp 0 --seed 42` into two folders; `diff -r` must be empty. Run once without → still works.
- [ ] **Step 8: Commit** `feat: injectable timestamp/seed and byte-array reader overloads`.

---

## PR 3: Test project, golden baseline, CI

**Branch:** `pr3-tests-and-ci`
**Outcome:** `dotnet test` runs ~30 fast unit tests plus two golden-master tests; GitHub Actions runs them on ubuntu/windows/macos on every push; the Windows and macOS runs prove identical output.

**Files:**
- Create: `MineCity2000.Tests/MineCity2000.Tests.csproj` (xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk; `net8.0`; ProjectReferences to the four engine projects; `<None Include="Fixtures/**" CopyToOutputDirectory="PreserveNewest" />`)
- Create: `MineCity2000.Tests/Fixtures/test.sc2` (copy of `~/Downloads/test.sc2`)
- Create: `MineCity2000.Tests/Fixtures/baseline/default.zip`, `MineCity2000.Tests/Fixtures/baseline/fill.zip` (generated in Step 6)
- Create: `MineCity2000.Tests/GoldenWorldTests.cs`, `WorldComparer.cs`, `AnvilFile/NbtTagTests.cs`, `AnvilFile/ByteConverterTests.cs`, `MinecraftEditor/McaRoundTripTests.cs`, `MinecraftEditor/AbstractRegionTests.cs`, `MinecraftEditor/LevelDatTests.cs`, `SimCityReader/ReaderTests.cs`, `SimCityReader/CityDataTests.cs`, `MineCity2000/DefaultArgsTests.cs`, `MineCity2000/TerrainTests.cs`, `MineCity2000/BuildingModelTests.cs`
- Modify: `MineCity2000/MineCity2000.csproj`, `MinecraftEditor/MinecraftEditor.csproj`, `SimCityReader/SimCityReader.csproj`: `<ItemGroup><InternalsVisibleTo Include="MineCity2000.Tests" /></ItemGroup>`; change the private static helpers under test in `SCMapper` (`reticulateSplines`, `interpolateTriangles`, `getLowestTerrainLevel`, `readCityHeightmap`) and `BuildingType` predicates to `internal static`.
- Modify: `mine-city-2000.sln` (add test project), `.gitignore` (`MineCity2000.Tests/bin`, `MineCity2000.Tests/obj`), `README.md` (a "Running the tests" paragraph)
- Create: `.github/workflows/ci.yml`

**Interfaces (Produces):**
```csharp
// WorldComparer (test helper)
public static void assertWorldsEqual(string expectedZip, string actualDir)
// throws Xunit.Sdk.XunitException with message like "r.1.2.mca chunk (33,70): NBT differs at byte 5120 (expected 0x18, actual 0x00)"
```

- [ ] **Step 1: Scaffold the test project** (`dotnet new xunit -n MineCity2000.Tests`, add references, add to sln). Run `dotnet test` → 1 placeholder test passes. Commit `test: add xUnit test project`.

- [ ] **Step 2: Unit tests that need no fixtures.** Write each, run, see it pass (these characterize existing behaviour; if one fails, the test's expectation is wrong unless it reveals a real bug — then note it in the PR, do not fix behaviour in this PR).

`ByteConverterTests` — big-endian int/short/long/float/double round trips via `ByteConverter.intBytes` + `readNumber` (read `AnvilFile/Tags/ByteConverter.cs` for exact names).

`NbtTagTests`:
```csharp
[Fact]
public void compoundTag_roundTripsThroughBytes()
{
  var tag = new CompoundNBTTag("", new List<AnyNBTTag> {
    new ByteNBTTag("b", 7), new IntNBTTag("i", -5), new LongNBTTag("l", 1L << 40),
    new StringNBTTag("s", "héllo"), new ByteArrayNBTTag("ba", new byte[] { 1, 2, 3 }),
    new IntArrayNBTTag("ia", new int[] { 4, 5 }),
    new CompoundNBTTag("inner", new List<AnyNBTTag> { new ShortNBTTag("sh", 300) }) });
  byte[] bytes = tag.toBytesWithHeader();
  var parsed = (CompoundNBTTag)AnyNBTTag.parseTag(bytes, 0);
  Assert.Equal(bytes, parsed.toBytesWithHeader());
  Assert.Equal(-5, (int)parsed.getObjectOnPath("/i"));
  Assert.Equal((short)300, (short)parsed.getObjectOnPath("/inner/sh"));
}
```

`AbstractRegionTests`:
```csharp
[Fact] public void putBlock_thenGetBlock_returnsSameValue() { var r = new AbstractRegion(0, 0); r.putBlock(17, 40, 100, 24); Assert.Equal(24, r.getBlock(17, 40, 100)); }
[Fact] public void getBlock_onUntouchedPosition_isAir() { Assert.Equal(0, new AbstractRegion(0, 0).getBlock(5, 5, 5)); }
[Fact] public void putBlock_aboveWorldHeight_isIgnored() { var r = new AbstractRegion(0, 0); r.putBlock(0, 0, 256, 24); /* no throw */ }
[Fact] public void putBlockData_storesDataNibble() { var r = new AbstractRegion(0,0); r.putBlockData(3, 4, 5, new byte[]{35, 14}); Assert.Equal(new byte[]{35,14}, r.getBlockData(3,4,5)); }
[Fact] public void saveToFolder_writesOneRegionFileWithOnlyTouchedChunks() { /* put one block at (0,0,0) and one at (511,511,0); save with timestamp 0 to a temp dir; MCAReader.read -> exactly 2 chunks with xPos/zPos (0,0) and (31,31); section 0 Blocks[0] == block */ }
```

`McaRoundTripTests`: build 2 chunks with sections, `MCAWriter.toBytes(chunks, 123)` → `MCAReader.read` → compare each chunk's `NBTTag.toBytesWithHeader()`; also assert bytes 4096..4099 encode timestamp 123 for the first chunk and that the file length is a multiple of 4096.

`LevelDatTests`: `new LevelDat("city", new double[]{1,2,3}, false, 1000, 42).toBytes()` gunzipped → parse → `/Data/LastPlayed == 1000000L`, `/Data/RandomSeed == 42L`, `/Data/generatorName == "flat"`, `/Data/version == 19133`; with `generateTerrain=true` → `"default"`. Also: calling `toBytes()` twice gives identical bytes.

`DefaultArgsTests`:
```csharp
[Fact] public void value_with_spaces_is_kept() => Assert.Equal(new[]{"-o","/a b/c"}, DefaultArgs.parse("-o /a b/c"));
[Fact] public void crlf_and_lf_both_work() => Assert.Equal(new[]{"-f","-g"}, DefaultArgs.parse("-f\r\n-g\n"));
[Fact] public void comments_and_blank_lines_are_skipped() => Assert.Empty(DefaultArgs.parse("# hi\n\n   \n"));
```

`ReaderTests`: build a minimal IFF file in memory (`"FORM"` + big-endian length + `"SCDH"` + segments `ALTM`(32768 zero bytes), `XBLD`, `XTER`, `MISC` RLE-encoded as a single run `0x80+n` of zeros) → `Reader.read(bytes, "x")` → `getSquare(0,0).buildingType == 0`, `getWaterLevel()` equals what you encoded in MISC (read `CityData.interpretMISC` for the offset). Write an `IffBuilder` helper in the test project for this.

`CityDataTests.cityName_fallsBackToFileName`: `Reader.read(bytesWithoutCNAM, "myCity").getCityName() == "myCity"`; and with a CNAM whose first byte is the length and content `"Test"` → `"Test"`.

`TerrainTests` (internal statics): `interpolateTriangles(p1,p2,p3,p4)` on four equal heights returns a 16×16 array of that height; on `(0,0,0,16)` the corner value is 16 and the opposite corner 0; `getLowestTerrainLevel` of a 2×2 array `{{5,3},{9,1}}` is 1.

`BuildingModelTests.allModels_loadWithoutError`: for every `buildings/*.mca` file, `new BuildingModel(buildingsDir, code, variant)` does not throw and `getChunkData(0,0)` has 16 entries. (The hash-pinning version arrives in PR 5 when the API changes.) Fixture location: the test resolves `buildings` by walking up from `AppContext.BaseDirectory` until a `buildings` folder with `1.mca`… is found (no copy; 79 MB).

Run `dotnet test` → all pass. Commit `test: unit tests for NBT, region files, reader, args parsing, terrain`.

- [ ] **Step 3: `WorldComparer.assertWorldsEqual(expectedZip, actualDir)`** — opens the zip with `System.IO.Compression.ZipFile.OpenRead`; asserts the set of entry names equals the set of relative file paths in `actualDir` (forward slashes); for each `region/*.mca` compares `MCAReader.read(expectedBytes)` vs `MCAReader.read(actualBytes)` chunk-by-chunk (keyed by `getXPos()/getZPos()`, assert same count), comparing `chunk.NBTTag.toBytesWithHeader()`; for `level.dat` compares gunzipped bytes; any other file byte-for-byte. The message must name the file, chunk coordinates, and first differing byte offset. Note: comparing decompressed NBT (not raw file bytes) is deliberate so PR 4 can change the zlib implementation without invalidating the baseline.

- [ ] **Step 4: `GoldenWorldTests`:**
```csharp
public class GoldenWorldTests
{
  private static readonly string FixtureCity = Path.Combine(AppContext.BaseDirectory, "Fixtures", "test.sc2");
  private static readonly string BuildingsDir = TestPaths.buildingsDir(); // walks up to repo /buildings

  [Theory]
  [InlineData(false, "default.zip")]
  [Trait("Category", "Golden")]
  public void defaultConversion_matchesBaseline(bool fill, string baselineName) => run(fill, baselineName);

  [Theory]
  [InlineData(true, "fill.zip")]
  [Trait("Category", "Golden"), Trait("Category", "Slow")]
  public void fillUndergroundConversion_matchesBaseline(bool fill, string baselineName) => run(fill, baselineName);

  private static void run(bool fill, string baselineName)
  {
    string outDir = Path.Combine(Path.GetTempPath(), "mc2k-golden-" + Guid.NewGuid());
    var options = new MapperOptions(fill, generateTerrain: false, timestamp: 1700000000, randomSeed: 12345);
    new SCMapper(BuildingsDir, null).makeMap(FixtureCity, outDir, options);

    string baseline = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline", baselineName);
    if (Environment.GetEnvironmentVariable("MC2K_UPDATE_BASELINE") == "1")
    {
      string repoBaseline = Path.Combine(TestPaths.repoRoot(), "MineCity2000.Tests", "Fixtures", "baseline", baselineName);
      File.Delete(repoBaseline);
      ZipFile.CreateFromDirectory(outDir, repoBaseline, CompressionLevel.Optimal, includeBaseDirectory: false);
      return;
    }
    WorldComparer.assertWorldsEqual(baseline, outDir);
    Directory.Delete(outDir, true);
  }
}
```
`TestPaths.repoRoot()` walks up from `AppContext.BaseDirectory` to the directory containing `mine-city-2000.sln`.

- [ ] **Step 5: Copy the fixture:** `cp ~/Downloads/test.sc2 MineCity2000.Tests/Fixtures/test.sc2`.

- [ ] **Step 6: Generate the baselines** (on macOS after PR 1 is merged, buildings ARE placed now): `MC2K_UPDATE_BASELINE=1 dotnet test --filter Category=Golden`. Expect `default.zip` ≈ 1–2 MB, `fill.zip` ≈ 2–3 MB. Then run `dotnet test` normally → both golden tests pass. Sanity-check the baseline is not terrain-only: unzip, `MCAReader` chunk count of `r.1.1.mca` > 0 and file size > the 4.2 MB observed in the no-buildings run OR simply inspect that a chunk in the city centre contains block ids other than 0/9/24.

- [ ] **Step 7: CI workflow `.github/workflows/ci.yml`:**
```yaml
name: CI
on: [push, pull_request]
jobs:
  test:
    strategy:
      fail-fast: false
      matrix: { os: [ubuntu-latest, windows-latest, macos-latest] }
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet build MineCity2000.Tests/MineCity2000.Tests.csproj -c Release
      - run: dotnet test MineCity2000.Tests/MineCity2000.Tests.csproj -c Release --no-build --logger "trx" --logger "console;verbosity=normal"
```
(The MAUI GUI project is intentionally not built in CI; build the test project, not the solution.)

- [ ] **Step 8:** README: add under "Projects": "MineCity2000.Tests: unit and golden-master tests. Run `dotnet test`; use `--filter Category!=Slow` to skip the 2-minute fill-underground test; set `MC2K_UPDATE_BASELINE=1` to regenerate the baseline worlds after an intentional output change."

- [ ] **Step 9:** Push, confirm green on all three OSes (this is the proof that Windows and macOS produce the same world). Commit messages: `test: golden-master world tests with committed baseline`, `ci: run tests on ubuntu/windows/macos`.

---

## PR 4: Replace the zlib.net binary with System.IO.Compression

**Branch:** `pr4-builtin-zlib`
**Outcome:** No binary dependency; same decompressed output (compressed bytes may differ, which the golden test tolerates by design).

**Files:**
- Modify: `MinecraftEditor/ZLibUtils.cs`, `MinecraftEditor/MinecraftEditor.csproj` (remove `<Reference Include="zlib">`)
- Delete: `MinecraftEditor/lib/zlib.net.dll`
- Test: `MineCity2000.Tests/MinecraftEditor/ZLibUtilsTests.cs`

- [ ] **Step 1: Write the failing test** (fails to compile against the new signature only if you change it; keep `CompressData/DecompressData(byte[] in, out byte[] out)` unchanged so the test passes before and after):
```csharp
[Fact] public void compress_thenDecompress_roundTrips() { byte[] input = Enumerable.Range(0, 10000).Select(i => (byte)(i % 7)).ToArray(); ZLibUtils.CompressData(input, out var c); ZLibUtils.DecompressData(c, out var d); Assert.Equal(input, d); Assert.True(c.Length < input.Length); }
[Fact] public void compressed_isZlibFormat() { ZLibUtils.CompressData(new byte[100], out var c); Assert.Equal(0x78, c[0]); } // RFC1950 header, Minecraft requires zlib (type 2) not raw deflate
[Fact] public void decompress_readsAllBuildingModelChunks() { /* MCAReader.readFile on buildings/1.mca returns >0 chunks — exercises decompression of real data */ }
```
- [ ] **Step 2: Reimplement** with `System.IO.Compression.ZLibStream` (available since .NET 6):
```csharp
public static void CompressData(byte[] inData, out byte[] outData)
{
    using var output = new MemoryStream();
    using (var z = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true)) { z.Write(inData, 0, inData.Length); }
    outData = output.ToArray();
}
public static void DecompressData(byte[] inData, out byte[] outData)
{
    using var input = new MemoryStream(inData);
    using var z = new ZLibStream(input, CompressionMode.Decompress);
    using var output = new MemoryStream();
    z.CopyTo(output);
    outData = output.ToArray();
}
```
Remove `using zlib;`, `testDecompress`, `CopyStream`.
- [ ] **Step 3:** delete the dll and the csproj Reference. `dotnet test` → all green including golden. Commit `refactor: use System.IO.Compression.ZLibStream instead of bundled zlib.net.dll`.

---

## PR 5: Flat section storage (memory ~20x down)

**Branch:** `pr5-flat-sections`
**Outcome:** Default and fill-underground conversions both around 200 MB RSS (of which ~45 MB is the .NET runtime; the rest is the `int[2049][2049]` terrain grid, the model cache and `MCAWriter` buffers, untouched by this PR) and several times faster; golden tests unchanged; public behaviour unchanged. (Measured 2026-10-16: default 953 MB → 193 MB, 24 s → 3.2 s; fill 3,685 MB → 202 MB, 107 s → 5.7 s.)

**Files:**
- Modify: `MinecraftEditor/Section.cs` (becomes the storage unit with lazy/uniform fill), `MinecraftEditor/AbstractRegion.cs` (holds `Section?[,,]`), `MineCity2000/BuildingModel.cs` (holds `Section?[,,]`), `MineCity2000/SCMapper.cs` (call sites of `putBlockData`/`getBlockData`/`getChunkData`), `MinecraftEditor/AbstractWorld.cs` (if it touches `getBlockData` — read it)
- Test: `MineCity2000.Tests/MinecraftEditor/LegacyAbstractRegion.cs` (verbatim copy of the current class, renamed, test-only, deleted in the last commit of this PR), `AbstractRegionDifferentialTests.cs`, `SectionTests.cs`, updated `AbstractRegionTests.cs`, `BuildingModelTests.cs` (hash pinning)

**Interfaces (Produces):**
```csharp
// Section (MinecraftEditor)
public Section(byte y)                       // starts as uniform air; arrays allocated on first non-air write
public Section(byte y, byte uniformBlock)    // uniform fill (e.g. sandstone) with data 0, no arrays allocated until needed
public byte getBlock(int x, int y, int z)    // 0..15 each; index = x + 16*z + 256*y
public byte getData(int x, int y, int z)
public void putBlock(int x, int y, int z, byte block, byte data)
public bool isEmpty                           // true if uniform air (used by saveToFolder to skip)
public byte[] getBlocks() / getData()         // materialize if uniform, then return (existing callers keep working)
// AbstractRegion
public byte getBlock(int x, int z, int y)                     // unchanged signature
public byte getData(int x, int z, int y)                      // new
public void putBlock(int x, int z, int y, byte block)         // unchanged
public void putBlock(int x, int z, int y, byte block, byte data)   // replaces putBlockData(byte[])
public void fillSection(int chunkX, int chunkZ, int sectionY, byte block)  // new: whole-section uniform fill
// removed: byte[] getBlockData(...), putBlockData(byte[])
// BuildingModel
public Section? getSection(int chunkX, int chunkZ, int sectionY)   // replaces getChunkData(x,z)
```
Note the argument order difference: `AbstractRegion` keeps its historical `(x, z, y)`; `Section` uses `(x, y, z)` to match Minecraft's index formula. Document both in XML comments.

- [ ] **Step 1: Copy `AbstractRegion.cs` into the test project as `LegacyAbstractRegion`** (namespace `com.mc2k.Tests`, class renamed, otherwise verbatim). Commit `test: snapshot legacy AbstractRegion for differential testing`.

- [ ] **Step 2: Write the differential test (fails until the new API exists):**
```csharp
[Fact]
public void randomWrites_matchLegacyImplementation()
{
  var rng = new Random(2024);
  var legacy = new LegacyAbstractRegion(1, 2);
  var fresh = new AbstractRegion(1, 2);
  for (int i = 0; i < 200_000; i++)
  {
    int x = rng.Next(512), z = rng.Next(512), y = rng.Next(270); // includes y >= 256
    byte block = (byte)rng.Next(256), data = (byte)rng.Next(16);
    if (rng.Next(4) == 0) { legacy.putBlock(x, z, y, block); fresh.putBlock(x, z, y, block); }
    else { legacy.putBlockData(x, z, y, new byte[] { block, data }); fresh.putBlock(x, z, y, block, data); }
  }
  for (int i = 0; i < 200_000; i++)
  {
    int x = rng.Next(512), z = rng.Next(512), y = rng.Next(270);
    Assert.Equal(legacy.getBlockData(x, z, y)[0], fresh.getBlock(x, z, y));
    Assert.Equal(legacy.getBlockData(x, z, y)[1], fresh.getData(x, z, y));
  }
  string a = Path.GetTempFileName() + "L", b = Path.GetTempFileName() + "N";
  Directory.CreateDirectory(a); Directory.CreateDirectory(b);
  legacy.saveToFolder(a, 7); fresh.saveToFolder(b, 7);
  Assert.Equal(File.ReadAllBytes(Path.Combine(a, "r.1.2.mca")), File.ReadAllBytes(Path.Combine(b, "r.1.2.mca")));
}
```
Plus `SectionTests`: uniform sandstone section → `getBlock` anywhere is 24 and `getBlocks()` is 4096×24; writing air into a uniform section materializes and keeps the rest 24; nibble packing: `putBlock(1,0,0,b,0xF)` then `getData()[0] == 0xF0`, `putBlock(0,0,0,b,0xF)` → `getData()[0] == 0xFF`.

- [ ] **Step 3: Implement `Section`** per the interface. Storage: `byte[]? _blocks` (4096), `byte[]? _data` (2048), `byte _uniformBlock`. `putBlock`: if arrays null and `block == _uniformBlock && data == 0` return; else materialize (`Array.Fill(_blocks, _uniformBlock)`), then write. `NBTTag` getter materializes first. Keep `_blockLight`/`_skyLight` as today (allocated on materialize, skyLight 255).

- [ ] **Step 3b (added 2026-10-16): fix the pre-existing read transposition.** The old `getBlockData` reads `section[x%16][y%16][z%16]` while `putBlock` writes `section[x%16][z%16][y%16]`. The new implementation must read and write the same index (`x + 16*z + 256*y`). Replace the PR 3 characterisation test `AbstractRegionTests.getBlock_readsZAndYSwappedWithinSection_currentBehaviour` with a proper asymmetric round-trip (`putBlock(1,2,3,24)` then `getBlock(1,2,3)==24` and `getBlock(1,3,2)==0`), and verify the golden baselines are unchanged (the only reader, `fillWithWaterUpToWaterLevel`, runs before buildings exist, so the fix should be output-neutral; if a baseline changes, stop and report the diff before regenerating).

- [ ] **Step 4: Implement `AbstractRegion`** over `Section?[,,] _sections = new Section?[32, 32, 16]`. `getBlock` returns 0 for `y >= 256` or null section (**no allocation** — this removes the discarded-allocation bug). `saveToFolder` iterates sections, skips null and `isEmpty`, builds chunks exactly as today (same chunk ordering: i then j, sections ascending k). `fillSection(cx,cz,sy,block)` sets `_sections[cx,cz,sy] = new Section((byte)sy, block)` (used by the fill-underground path).

- [ ] **Step 5: Run the differential test + SectionTests** → pass. Commit `refactor: flat Section storage in AbstractRegion`.

- [ ] **Step 6: `BuildingModel`** — same `Section?[,,]` layout `[size, size, 16]`; loading rule unchanged: a section is created iff some block id or data nibble in the source section is non-zero; the wool/bedrock filtering logic is kept verbatim, writing through `section.putBlock(x, y, z, block, data)`. `getSection(cx, cz, sy)`.

- [ ] **Step 7: `SCMapper` call sites** — in `mapRegion` replace the `buildingChunkData[section][sx][sz][sy]` loop with `Section? s = buildingModel.getSection(offX, offZ, section); if (s == null) continue;` then `byte b = s.getBlock(sx, sy, sz), d = s.getData(sx, sy, sz);` and `newRegion.putBlock(blockX, blockZ, y, b, d)`. `repeatBlockUntilTunnelEndsX/Z` take `(byte block, byte data)`. In `mapRegionLandscape`, when `fillUnderground`, fill whole sections below `terrainHeight/16` with `newRegion.fillSection(x/16, z/16, sy, SANDSTONE_BLOCK)` once per chunk column (loop over chunks, compute min terrain height in the chunk, fill sections entirely below it, then per-column `putBlock` only for the partial section). Preserve the exact set of sandstone blocks: the result must equal the old per-block loop (the golden `fill.zip` test enforces this).

- [ ] **Step 8:** `dotnet test` (including Slow) → green. Commit `refactor: BuildingModel and SCMapper use flat sections`.

- [ ] **Step 9: Pin building models:** replace `allModels_loadWithoutError` with `allModels_matchBaselineHashes`: for each model compute SHA-256 over the concatenation of `getBlocks()` + `getData()` of every non-null section in `(cx, cz, sy)` order and compare with `Fixtures/building_model_hashes.txt` (generated with `MC2K_UPDATE_BASELINE=1`, same pattern as golden). Generate the file from the **PR 4 merge commit** (checkout, run generator, copy file forward) so it pins pre-refactor behaviour.

- [ ] **Step 10: Measure and record** in the PR description: `/usr/bin/time -l dotnet MineCity2000/bin/Release/net8.0/MineCity2000.dll MineCity2000.Tests/Fixtures/test.sc2 -o /tmp/x -c [-f]` before (main) and after. Expected: both modes around 200 MB RSS; both several times faster. Further levers (for PR 6, where browser memory is tighter): allocate light arrays only at serialisation with a shared sky-light array; narrower terrain heightmap type; stream `MCAWriter` output instead of `List<byte>`.

- [ ] **Step 11:** Delete `LegacyAbstractRegion.cs` and the differential test (they served their purpose; the golden + unit tests remain). Commit `test: remove legacy differential harness`.

---

## PR 6: Web UI — Angular + .NET WebAssembly

**Branch:** `pr6-web-ui`
**Outcome:** A static site on GitHub Pages (`https://jgosar.github.io/mine-city-2000/`) where the user drops a `.sc2`, ticks the two options, watches a progress bar, and downloads `<city>.zip` containing the world. No server. Includes the .NET 10 bump and the CLI/library split.

**Design (researched; sources at the end of this PR):**
- .NET side is a **WebAssembly Browser App** (`Microsoft.NET.Sdk.WebAssembly`, template `wasmbrowser`), not Blazor: it is the only form Microsoft documents for running .NET inside a Web Worker, and it exposes C# via `[JSExport]` to any JS front end.
- The **whole .NET runtime runs in a module Web Worker** so the Angular page stays responsive during the 10–60 s conversion. `WasmEnableThreads` is rejected (experimental, needs COOP/COEP headers GitHub Pages cannot set, and JS interop is main-thread-only). Progress flows C# → `[JSImport]` → `self.postMessage` → Angular.
- **Building models** ship as one `buildings.zip` (~24 MB) generated by MSBuild `ZipDirectory` from `/buildings`, fetched lazily on the first conversion, passed to C# as `byte[]`, and read on demand through `ZipArchive` via a new `Func<string, byte[]?>` loader on `SCMapper`. No 162 file writes, no MEMFS extraction.
- **Output zip** is produced in C# with `ZipFile.CreateFromDirectory` over the in-memory Emscripten filesystem (`/work/out`), returned as `byte[]`, transferred (not copied) to the main thread and downloaded as a Blob.
- **AOT** (`RunAOTCompilation`) on for Release publish only; expected 3–10x faster than the interpreter for this array-heavy code. `InvariantGlobalization`, `InvariantTimezone`, `PublishTrimmed`/`TrimMode=full`. Memory: default 2 GB max heap is plenty after PR 5; set `EmccInitialHeapSize=268435456` to avoid repeated growth.
- **Angular** owns the DOM only. The contract with the worker is three `postMessage` types: `convert`, `progress`, `done` (+ `error`). The wasm publish output's `_framework/` folder is copied into Angular's `src/assets/` at build time.

**Unverified items (resolve in the spike, Step 1):** `ZipArchive` works in browser-wasm (strong evidence, no official sentence); `wasm-tools` workload installs on macOS with Command Line Tools only; AOT publish size (~4–6 MB compressed expected).

**Files:**
- Create: `global.json` (`"sdk": { "version": "10.0.100", "rollForward": "latestFeature" }`)
- Modify: all engine csproj + test csproj → `net10.0`; `.github/workflows/ci.yml` → `dotnet-version: '10.0.x'`
- Create: `MineCity2000.Cli/MineCity2000.Cli.csproj` (Exe) — move `Program.cs`, `DefaultArgs.cs`, `default_args.txt`, the `CopyBuildingsAfterBuild` target here; `MineCity2000/MineCity2000.csproj` becomes `OutputType Library` keeping `SCMapper`, `BuildingModel`, `MapperOptions`
- Create: `MineCity2000/BuildingSource.cs`, `MineCity2000/ZipBuildingSource.cs`, `MineCity2000/WebConverter.cs`
- Modify: `MineCity2000/SCMapper.cs`, `MineCity2000/BuildingModel.cs` (constructor from bytes)
- Create: `MineCity2000.Wasm/MineCity2000.Wasm.csproj`, `MineCity2000.Wasm/ConverterExports.cs`, `MineCity2000.Wasm/wwwroot/main.js` (required `WasmMainJSPath`; minimal), `MineCity2000.Wasm/wwwroot/worker.js`
- Create: `MineCity2000-Web/` (Angular CLI workspace: `src/app/convert/convert.component.ts|html|css`, `src/app/worker-bridge.service.ts`, `src/assets/.nojekyll`, `package.json`, `angular.json`), `MineCity2000-Web/scripts/copy-wasm.sh`
- Create: `.github/workflows/pages.yml`
- Modify: `.gitignore` (`MineCity2000-Web/node_modules`, `MineCity2000-Web/dist`, `MineCity2000-Web/src/assets/_framework`, `MineCity2000-Web/src/assets/buildings.zip`, `MineCity2000.Wasm/bin|obj`, `MineCity2000.Cli/bin|obj`), `mine-city-2000.sln`, `.vscode/launch.json` (CLI path), `README.md` (link to the web app)
- Test: `MineCity2000.Tests/MineCity2000/WebConverterTests.cs`, `ZipBuildingSourceTests.cs`

**Interfaces (Produces):**
```csharp
// MineCity2000/BuildingSource.cs
public interface BuildingSource { byte[]? read(string fileName); }          // fileName like "198a.mca"; null if absent
public sealed class DirectoryBuildingSource : BuildingSource { public DirectoryBuildingSource(string dir); }
// MineCity2000/ZipBuildingSource.cs
public sealed class ZipBuildingSource : BuildingSource { public ZipBuildingSource(byte[] zipBytes); }   // entries "123.mca" or "buildings/123.mca" both accepted
// SCMapper
public SCMapper(String buildingsDir, ProgressCallback? progressCallback)           // unchanged, wraps DirectoryBuildingSource
public SCMapper(BuildingSource buildings, ProgressCallback? progressCallback)      // new
// BuildingModel
public static bool modelFileExists(BuildingSource src, byte typeCode, String modelVariant)
public BuildingModel(BuildingSource src, byte typeCode, String modelVariant)       // uses MCAReader.read(byte[])
// MineCity2000/WebConverter.cs  (platform-neutral, unit-tested)
public static class WebConverter
{
  // Throws InvalidDataException("Not a SimCity 2000 city file") if bytes do not start with "FORM" + "SCDH" at offset 8.
  // Writes the world into Path.Combine(workDir, cityName), zips it (entries relative: level.dat, region/r.X.Z.mca), deletes workDir, returns zip bytes.
  public static byte[] convertToZip(byte[] sc2, string cityName, BuildingSource buildings, MapperOptions options, SCMapper.ProgressCallback? progress, string workDir);
}
// MineCity2000.Wasm/ConverterExports.cs
[SupportedOSPlatform("browser")]
public static partial class ConverterExports
{
  [JSExport] public static void LoadBuildings(byte[] buildingsZip);                                   // once per worker
  [JSExport] public static byte[] Convert(byte[] sc2, string cityName, bool fillUnderground, bool generateTerrain); // returns world zip
  [JSImport("progress", "worker.js")] internal static partial void ReportProgress(int percent);
}
```
Worker message contract (JS):
```
main → worker : { type: 'convert', sc2: Uint8Array, cityName: string, fill: bool, terrain: bool }
worker → main : { type: 'ready' } | { type: 'progress', percent } | { type: 'done', zip: Uint8Array } | { type: 'error', message }
```

- [ ] **Step 1: Spike (throwaway branch, ≤ half a day).** Install .NET 10 SDK on the Mac (`bash dotnet-install.sh --channel 10.0 --install-dir ~/.dotnet`), `dotnet workload install wasm-tools`, `dotnet new install Microsoft.NET.Runtime.WebAssembly.Templates`, `dotnet new wasmbrowser -n Spike`, reference `MineCity2000`, add a `[JSExport] static byte[] Zip(byte[] x)` that writes `x` to `/work/a.bin`, `ZipFile.CreateFromDirectory("/work", "/work/o.zip")`, returns the bytes; run `dotnet run` and call it from the browser console. Then `dotnet publish -c Release` with AOT and note the size of `_framework`. Record findings in the PR description. **If `ZipArchive` fails in wasm**, fall back to writing the zip with a tiny hand-rolled stored-entry (no compression) ZIP writer — ~60 lines, still inside `WebConverter` — and note it.

- [ ] **Step 2: .NET 10 bump + CLI/library split.** Add `global.json`; set every csproj to `net10.0`; create `MineCity2000.Cli` and move `Program.cs`, `DefaultArgs.cs`, `default_args.txt`, the copy target; `MineCity2000` → `OutputType Library`. Update test project references (tests referencing `DefaultArgs` now reference `MineCity2000.Cli`), `.vscode/launch.json`, CI `dotnet-version`. `dotnet test` green on all 3 OSes. Commit `chore: move to .NET 10 and split CLI from conversion library`.

- [ ] **Step 3: `BuildingSource` + `ZipBuildingSource` (test first).**
```csharp
[Fact] public void zipSource_readsEntryWithOrWithoutFolderPrefix() { /* build a zip in memory with "buildings/5.mca" => bytes {1,2,3}; new ZipBuildingSource(zip).read("5.mca") equals {1,2,3}; read("6.mca") is null */ }
[Fact] public void directorySource_matchesFileSystem() { /* repo buildings dir: read("1.mca") equals File.ReadAllBytes */ }
[Fact] public void mapper_withZipSource_producesSameWorldAsDirectorySource() { /* zip the repo buildings dir into memory; run SCMapper(ZipBuildingSource) on test.sc2 with timestamp/seed; WorldComparer.assertWorldsEqual(default.zip, outDir) */ }
```
Implement; `BuildingModel` switches to `src.read(typeCode + variant + ".mca")` + `MCAReader.read(bytes)`; `modelFileExists` = `src.read(...) != null` — cache the bytes in `BuildingModel` construction to avoid reading twice (read once in `loadBuildingModel`: `byte[]? bytes = src.read(name); if (bytes == null) return null;`). Commit `feat: BuildingSource abstraction with directory and zip implementations`.

- [ ] **Step 4: `WebConverter` (test first).**
```csharp
[Fact] public void convertToZip_matchesGoldenBaseline() { byte[] sc2 = File.ReadAllBytes(FixtureCity); byte[] zip = WebConverter.convertToZip(sc2, "test", new DirectoryBuildingSource(BuildingsDir), new MapperOptions(false, false, 1700000000, 12345), null, tempDir); extract zip to tempDir2; WorldComparer.assertWorldsEqual(default.zip, tempDir2); }
[Fact] public void convert_rejectsInvalidFile() { var ex = Assert.Throws<InvalidDataException>(() => WebConverter.convertToZip(new byte[] { 1, 2, 3 }, "x", src, opts, null, tempDir)); Assert.Contains("SimCity 2000", ex.Message); }
[Fact] public void convert_rejectsTruncatedFile() { byte[] cut = File.ReadAllBytes(FixtureCity)[..5000]; Assert.ThrowsAny<Exception>(() => WebConverter.convertToZip(cut, "x", src, opts, null, tempDir)); } // must not hang; documents that Reader throws (IndexOutOfRange today) — wrap into InvalidDataException("City file is truncated or corrupt") in Reader.read
```
Implement: validate header, `Reader.read` via `SCMapper.makeMap(cityPath, outDir, options)` (write sc2 bytes to `Path.Combine(workDir, cityName + ".sc2")` first — `makeMap` still takes a path; this is fine in MEMFS), zip with `CompressionLevel.Fastest`, `Directory.Delete(workDir, true)`. Commit `feat: WebConverter produces a zipped world from bytes`.

- [ ] **Step 5: `MineCity2000.Wasm` project.** csproj:
```xml
<Project Sdk="Microsoft.NET.Sdk.WebAssembly">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
    <OutputType>Exe</OutputType>
    <WasmMainJSPath>wwwroot/main.js</WasmMainJSPath>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>
    <InvariantTimezone>true</InvariantTimezone>
    <PublishTrimmed>true</PublishTrimmed><TrimMode>full</TrimMode>
    <RunAOTCompilation>true</RunAOTCompilation>
    <WasmStripILAfterAOT>true</WasmStripILAfterAOT>
    <EmccInitialHeapSize>268435456</EmccInitialHeapSize>
    <CompressionEnabled>false</CompressionEnabled>   <!-- GitHub Pages does not serve .br/.gz twins -->
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="../MineCity2000/MineCity2000.csproj" /></ItemGroup>
  <Target Name="BundleBuildings" BeforeTargets="PrepareForBuild">
    <ZipDirectory SourceDirectory="../buildings" DestinationFile="wwwroot/buildings.zip" Overwrite="true" />
  </Target>
</Project>
```
`ConverterExports.cs` holds a `static BuildingSource? _buildings`; `LoadBuildings` sets `new ZipBuildingSource(zip)`; `Convert` throws `InvalidOperationException("Call LoadBuildings first")` if null, else returns `WebConverter.convertToZip(sc2, cityName, _buildings, new MapperOptions(fill, terrain), ReportProgress, "/work")`. `Program.Main` is empty (required by the template). `worker.js`:
```js
import { dotnet } from './_framework/dotnet.js';
const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();
setModuleImports('worker.js', { progress: p => self.postMessage({ type: 'progress', percent: p }) });
const exports = await getAssemblyExports(getConfig().mainAssemblyName);
let buildingsLoaded = false;
self.onmessage = async ({ data }) => {
  if (data.type !== 'convert') return;
  try {
    if (!buildingsLoaded) {
      const res = await fetch(new URL('./buildings.zip', import.meta.url), { cache: 'force-cache' });
      exports.ConverterExports.LoadBuildings(new Uint8Array(await res.arrayBuffer()));
      buildingsLoaded = true;
    }
    const zip = exports.ConverterExports.Convert(data.sc2, data.cityName, data.fill, data.terrain);
    self.postMessage({ type: 'done', zip }, [zip.buffer]);
  } catch (e) { self.postMessage({ type: 'error', message: String(e?.message ?? e) }); }
};
self.postMessage({ type: 'ready' });
```
Verify with `dotnet run` + a throwaway `index.html` that spawns the worker and converts `test.sc2`; unzip the download and compare against `default.zip` using the `WorldComparer` logic (run the golden comparison via a small test that takes a folder path from an env var `MC2K_COMPARE_DIR`, added to `GoldenWorldTests`). Commit `feat: .NET WebAssembly converter worker`.

- [ ] **Step 6: Angular app** (`npx @angular/cli@latest new MineCity2000-Web --standalone --style=css --routing=false --skip-git`). One component, `ConvertComponent`: file drop zone + `<input type="file" accept=".sc2">`, two checkboxes (`Fill underground area`, `Generate terrain around the city` — same labels as the old GUI), a `Convert` button, `<progress>` bound to a signal, status text, and a download `<a>` that appears on `done`. `WorkerBridgeService` creates `new Worker(new URL('assets/worker.js', document.baseURI), { type: 'module' })`, exposes `convert(file, fill, terrain): Observable<{percent} | {zip}>`, and surfaces `error` messages. `scripts/copy-wasm.sh`: `dotnet publish ../MineCity2000.Wasm -c Release` then `rsync -a --delete ../MineCity2000.Wasm/bin/Release/net10.0/browser-wasm/publish/wwwroot/{_framework,worker.js,buildings.zip} src/assets/` (verify exact publish path from the spike; adjust). Add `"prebuild": "bash scripts/copy-wasm.sh"` to `package.json`. Unit tests (Karma/Jasmine default): `WorkerBridgeService` with a fake Worker emits progress then zip; component disables the button while converting and shows the error text on `error`. `ng build --base-href /mine-city-2000/` succeeds. Commit `feat: Angular web UI for in-browser conversion`.

- [ ] **Step 7: `.github/workflows/pages.yml`:**
```yaml
name: Pages
on: { push: { branches: [master] }, workflow_dispatch: {} }
permissions: { contents: read, pages: write, id-token: write }
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet workload install wasm-tools
      - uses: actions/setup-node@v4
        with: { node-version: '22', cache: npm, cache-dependency-path: MineCity2000-Web/package-lock.json }
      - run: npm ci
        working-directory: MineCity2000-Web
      - run: npm run build -- --base-href /mine-city-2000/
        working-directory: MineCity2000-Web
      - run: touch MineCity2000-Web/dist/MineCity2000-Web/browser/.nojekyll
      - uses: actions/upload-pages-artifact@v3
        with: { path: MineCity2000-Web/dist/MineCity2000-Web/browser }
  deploy:
    needs: build
    runs-on: ubuntu-latest
    environment: { name: github-pages, url: ${{ steps.deployment.outputs.page_url }} }
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```
Owner action: repo Settings → Pages → Source = GitHub Actions. Commit `ci: deploy web app to GitHub Pages`.

- [ ] **Step 8: Browser verification** in Chrome, Firefox, Safari on the deployed URL: convert `test.sc2` (default options) → download → unzip → compare with `default.zip` via the `MC2K_COMPARE_DIR` test. Try fill-underground once and note time + peak memory from the browser task manager. Try dropping a `.txt` → error message shown, button re-enabled. Add README section "Using the web app" with the URL. Commit `docs: web app usage`.

Sources: [WebAssembly Browser App](https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/wasm-browser-app), [.NET on Web Workers](https://learn.microsoft.com/aspnet/core/client-side/dotnet-on-webworkers), [DotNetOnWebWorkersReact sample](https://github.com/dotnet/blazor-samples/tree/main/10.0/DotNetOnWebWorkersReact), [mono/wasm features.md](https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md), [WASM build tools and AOT](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot), [GitHub Pages hosting notes](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/github-pages).

---

## PR 6b: Replace the Angular front end with a vanilla-JS page (same PR, added 2026-10-16)

### Context

PR 6 is open as #24 with an Angular 22 front end in `MineCity2000-Web/`. After seeing what it drags in (198 npm packages, a 274 MB `node_modules`, a 7,085-line lock file, an Angular CLI build, a copy script and a Node job in CI) for a page that is one form with a progress bar, the owner decided the framework is not worth it: the page has no routing, no shared state and no component tree, and all its real logic is plain browser API (Web Worker messages, Blob download). The front end is replaced in the same PR by plain HTML/CSS/JS living next to the worker in `MineCity2000.Wasm/wwwroot/`, so that `dotnet publish` alone produces the complete deployable site and the npm toolchain disappears entirely. The Angular commits stay in the PR's history in case it is ever wanted back. Behaviour, labels and error handling of the page stay exactly as reviewed; only the implementation moves.

Owner decision (2026-10-16): keep **unit tests for the bridge on Node's built-in `node --test` runner** (zero dependencies, fake Worker) **and a committed dependency-free headless-Chrome end-to-end script** that converts the fixture city through the real UI and checks the world against the golden baseline. Both run in the Pages workflow. No `package.json`, no lock file, no `node_modules`.

### Files

- Delete: `MineCity2000-Web/` (whole folder), and the `MineCity2000-Web/*` lines in `.gitignore` (lines 24–30).
- Rewrite: `MineCity2000.Wasm/wwwroot/index.html` (today a dev harness; becomes the real page).
- Create: `MineCity2000.Wasm/wwwroot/style.css` (verbatim from `MineCity2000-Web/src/app/convert/convert.component.css`, plus `.drop-zone.dragging` etc. already there), `MineCity2000.Wasm/wwwroot/converter-client.js` (port of `WorkerBridgeService`), `MineCity2000.Wasm/wwwroot/app.js` (port of `ConvertComponent`: DOM wiring only).
- Keep unchanged: `MineCity2000.Wasm/wwwroot/worker.js`, `wwwroot/main.js`, `ConverterExports.cs`, `MineCity2000.Wasm.csproj` (except possibly excluding the `tests/` folder from content, see Step 3).
- Create: `MineCity2000.Wasm/tests/converter-client.test.mjs` (node:test, fake Worker), `MineCity2000.Wasm/tests/e2e/serve.mjs`, `MineCity2000.Wasm/tests/e2e/chrome-e2e.mjs` (ports of the step-8 scratch scripts `scratchpad/step8/{serve.mjs,drive.mjs}`, which use only `node:http`, `node:fs`, `node:child_process` and Node's global `WebSocket`), `MineCity2000.Wasm/tests/e2e/README.md` (3 lines: how to run).
- Modify: `.github/workflows/pages.yml` (drop Node/npm steps; publish with dotnet; run the two test scripts; upload `MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot`), `README.md` ("Building the web app", projects list, the "web app's unit tests" sentence, the Angular mention), `docs/superpowers/plans/2026-10-15-modernization.md` (append this PR 6b section so the repo plan matches), `.superpowers` ledger (not tracked).

### Interfaces

`converter-client.js` (ES module, `// @ts-check` + JSDoc, no build step):
```js
export const MAX_FILE_BYTES = 16 * 1024 * 1024;
export function cityNameOf(file)            // file.name without extension, or 'city'
export class ConverterClient {
  constructor(createWorker = () => new Worker(new URL('./worker.js', import.meta.url), { type: 'module' }))
  get ready()                                // Promise<void>: resolves on {type:'ready'}, rejects if the worker fails to start
  /** @returns {Promise<Uint8Array>} the world zip */
  convert(file, fill, terrain, { onProgress, deterministic } = {})   // rejects: too large (>16 MB), dead worker, already running, worker error/crash
}
```
Semantics copied 1:1 from `MineCity2000-Web/src/app/worker-bridge.service.ts`: wait for `ready` before posting; size cap before reading; one job at a time ("A conversion is already running"); `onerror`/`onmessageerror` mark the client dead ("The converter worker has stopped; reload the page") and reject the current job; a worker `{type:'error'}` rejects the current job but leaves the client usable; `deterministic: {timestamp, seed}` is spread into the message (used only by the e2e script). The `sc2` buffer is transferred. No RxJS: a Promise plus an `onProgress` callback replaces the Observable; "unsubscribe teardown" has no equivalent and is dropped (a Promise cannot be cancelled; the busy flag is reset on done/error only).

Worker message contract: unchanged (see PR 6 Interfaces).

`index.html` + `app.js`: same controls, texts and states as `convert.component.{ts,html}`: title "MineCity 2000", intro paragraph with the GitHub link, drop zone with `dragover`/`dragleave`/`drop` highlight, `<input type="file" accept=".sc2">`, checkboxes labelled exactly `Fill underground area` and `Generate terrain around the city` (with the same `title` tooltips), `Convert` button (disabled when no file, while converting, or when the worker failed to start), `<progress max="100">` shown only while converting (indeterminate until the first progress event), `<p class="status" role="status">` with the same status strings (`Loading converter…`, `Choose a SimCity 2000 city file (.sc2).`, `Ready to convert.`, `Loading building models (about 24 MB on the first run)…`, `Converting… N%`, `Done: X.Y MB`, `The converter could not start.`), `<p class="error" role="alert">`, download link `Download <city>.zip` (object URL revoked when replaced). Relative URLs only (`./style.css`, `./app.js`), no `<base href>`: the site works at `/mine-city-2000/` and at `/`.

- [ ] **Step 1: Port the client (test first).** Write `MineCity2000.Wasm/tests/converter-client.test.mjs` with `node:test` + `node:assert/strict` and a `FakeWorker` class (`postMessage` records messages; `emit(data)` calls `onmessage`; `fail(message)` calls `onerror`). Cases, ported from `worker-bridge.service.spec.ts`: resolves ready, then progress values, then the zip; waits for ready before posting; rejects files over 16 MB without reading them (fake `File` object `{ name, size, arrayBuffer }` — Node 20+ has a global `File`, but a plain object is enough); forwards `deterministic` timestamp and seed; rejects on an `error` message and allows another conversion; rejects a concurrent convert without disturbing the running one; `onerror` before ready rejects `ready` and later converts; `onerror` after ready marks the client dead. Run `node --test "MineCity2000.Wasm/tests/*.test.mjs"` → fails (module missing). Implement `converter-client.js` → passes. Commit `feat: dependency-free converter client for the web page`.

- [ ] **Step 2: Replace the page.** Rewrite `wwwroot/index.html` (semantic markup, `<link rel="stylesheet" href="./style.css">`, `<script type="module" src="./app.js">`), add `style.css`, write `app.js` (create one `ConverterClient`, wire the elements by id, port `ConvertComponent`'s state machine: `file`, `fill`, `terrain`, `workerReady`, `converting`, `percent`, `workerError`, `conversionError`, `downloadUrl`; a single `render()` function updates texts/disabled states; `setFile` ignores drops while converting and clears only conversion errors). Remove the old `window.convertBytes`/`workerReady` harness hooks (the e2e script no longer needs them). Fix the known drop-zone flicker while porting: use a `dragenter`/`dragleave` depth counter or check `event.relatedTarget` so moving over child elements does not clear the highlight. Smoke-test locally with the SDK's dev server, `dotnet run --project MineCity2000.Wasm` (verified: it serves `wwwroot` straight from the static-web-assets manifest on a random `http://127.0.0.1:<port>/`, with `text/javascript` and `application/wasm` types), open the printed URL and convert `MineCity2000.Tests/Fixtures/test.sc2`. Verified facts: the SDK copies every file under `wwwroot/` unchanged into the publish output, does not rewrite `index.html` (`OverrideHtmlAssetPlaceholders` is off), and fingerprints only the `_framework` runtime files, so plain `app.js`/`style.css`/`converter-client.js` keep their names. Commit `feat: vanilla-JS web page replaces the Angular app`.

- [ ] **Step 3: Delete the Angular app.** `git rm -r MineCity2000-Web`, drop its `.gitignore` lines, grep the repo for `MineCity2000-Web`, `angular`, `npm` and fix every hit (README, workflow, csproj comments, the dev-harness comment in `index.html`). `MineCity2000.Wasm/tests/` sits outside `wwwroot`, so the SDK does not publish it (static assets are discovered as `wwwroot/**` only); confirm once by checking the publish output has no `tests/` folder. Commit `chore: remove the Angular front end`.

- [ ] **Step 4: Committed browser e2e.** Port `scratchpad/step8/serve.mjs` → `tests/e2e/serve.mjs` (static server over a given root dir at a given URL prefix, `.wasm` → `application/wasm`, `.js` → `text/javascript`, plus `/fixtures/` and `POST /upload/<name>` as today) and `drive.mjs` → `tests/e2e/chrome-e2e.mjs`. Contract: `node MineCity2000.Wasm/tests/e2e/chrome-e2e.mjs [siteDir]` (default `MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot`) with env `CHROME` (default chain: `$CHROME`, then `/Applications/Google Chrome.app/Contents/MacOS/Google Chrome` if it exists, then `google-chrome`, `google-chrome-stable`, `chromium`; fail loudly if none is found), `MC2K_PREFIX` (default `/mine-city-2000/`), `MC2K_OUT` (default a temp dir). It must: start the server on port 0 and read the assigned port; launch Chrome with `--headless=new --no-sandbox --disable-gpu --remote-debugging-port=0 --user-data-dir=<temp>` and read the port from the profile's `DevToolsActivePort` file; connect with Node's global `WebSocket` (Node ≥ 22.4; the Mac has 24); open the page; (a) via `Runtime.evaluate`, `await import('./converter-client.js')`, construct a `ConverterClient`, fetch `/fixtures/test.sc2`, call `convert(file, false, false, { deterministic: { timestamp: 1700000000, seed: 12345 } })` and upload the zip (the golden run); (b) drive the real UI: `DOM.setFileInputFiles` with the fixture, click `Convert`, wait for the download link, read the blob through `fetch(href)` in the page and upload it; assert progress went 0→100 and the zip has `test/level.dat` + 32 `test/region/*.mca`; (c) set a `.txt` file, click Convert, assert the error text `Not a SimCity 2000 city file` appears and the button is enabled again; then exit non-zero on any failure, printing timings. Zip bytes travel back from the page as base64 via `Runtime.evaluate` (`returnByValue`) or via the existing `POST /upload/<name>` route; the script writes them to `MC2K_OUT`, extracts (a) with `unzip -q` (present on macOS and ubuntu-latest; `tar -xf` as fallback) and runs `MC2K_COMPARE_DIR=<out>/test dotnet test MineCity2000.Tests --filter externalWorld_matchesBaseline_whenCompareDirSet` as a child process, asserting that exactly one test ran and passed (the hook passes trivially when the variable is empty, so the script must never run it with an empty path). The script deletes a stale `publish` folder only if asked (`--clean`), never by default. Run it locally against a fresh Release publish (`rm -rf MineCity2000.Wasm/bin/Release/net10.0/publish` first, because the SDK does not clean it and stale fingerprinted `_framework` files pile up) → green. Commit `test: headless-Chrome end-to-end check for the web page`.

- [ ] **Step 5: Pages workflow.** `pages.yml` build job becomes: checkout; setup-dotnet 10.0.x; `dotnet workload install wasm-tools`; `actions/setup-node@v4` with `node-version: '24'` and no cache (the runner's default Node may be 20, which lacks the global `WebSocket`); `dotnet publish MineCity2000.Wasm -c Release`; `node --test "MineCity2000.Wasm/tests/*.test.mjs"`; `node MineCity2000.Wasm/tests/e2e/chrome-e2e.mjs` (ubuntu-latest ships `google-chrome`); `touch MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot/.nojekyll`; `upload-pages-artifact@v3` with that path. Deploy job unchanged (deploy-only permissions, `if` not pull_request, concurrency). Validate with `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/pages.yml'))"` (`@action-validator` needs npx; skip it). Commit `ci: build and deploy the web page straight from dotnet publish`.

- [ ] **Step 6: Docs.** README: "Building the web app" → for development `dotnet run --project MineCity2000.Wasm` and open the printed URL (interpreter build, ~11 s per conversion); for the real thing `dotnet publish MineCity2000.Wasm -c Release` then serve `MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot` with any static server (`python3 -m http.server 8765 -d <dir>`; a `file://` URL does not work for module workers); tests: `node --test "MineCity2000.Wasm/tests/*.test.mjs"` and the e2e one-liner (needs Chrome and Node ≥ 22); projects list: replace the `MineCity2000-Web` line with a note that the page lives in `MineCity2000.Wasm/wwwroot`; drop the "web app's unit tests … npm test" sentence and all Node/npm wording; keep "Using the web app" as is. Append this PR 6b section to `docs/superpowers/plans/2026-10-15-modernization.md`. Commit `docs: web page build and test instructions`.

- [ ] **Step 7: Verify and update the PR.** `dotnet test MineCity2000.Tests` green; `node --test` green; e2e green locally against the Release publish; `git grep -n "MineCity2000-Web\|angular\|npm "` returns only history/plan mentions; push; CI + Pages (PR build) green on GitHub; edit the PR #24 description (`gh pr edit --body-file`) to describe the vanilla-JS front end and drop the npm notes.

### Verification (PR 6b)

1. `node --test "MineCity2000.Wasm/tests/*.test.mjs"` → all pass, no dependencies installed.
2. `dotnet publish MineCity2000.Wasm -c Release` → `publish/wwwroot` contains `index.html`, `app.js`, `converter-client.js`, `style.css`, `worker.js`, `main.js`, `buildings.zip`, `_framework/` and no `tests/`.
3. `node MineCity2000.Wasm/tests/e2e/chrome-e2e.mjs MineCity2000.Wasm/bin/Release/net10.0/publish/wwwroot` → deterministic world matches `default.zip`; UI conversion yields a 33-entry zip; `.txt` shows the error and re-enables Convert.
4. Pages workflow green on the PR; after merge + enabling Pages, the deployed page converts `test.sc2` in Chrome, Firefox and Safari (owner).

---

## PR 7: Remove MAUI GUI, CI releases, docs

**Branch:** `pr7-retire-maui`
**Outcome:** Repo contains only the engine, tests, CLI and web app; releases are built by GitHub Actions as zips for Windows, macOS and Linux; README reflects the new world.

**Files:**
- Delete: `MineCity2000-GUI/` (whole folder), `release.bat`
- Modify: `mine-city-2000.sln` (remove GUI project), `.vscode/tasks.json` + `launch.json` (remove GUI entries; CLI launch uses `dotnet` + `MineCity2000.dll`), `.gitignore` (remove GUI lines), `README.md`
- Create: `.github/workflows/release.yml`

- [ ] **Step 1:** Remove the GUI project, sln entries, vscode entries. `dotnet build mine-city-2000.sln` on macOS → green (the sln no longer needs the MAUI workload anywhere). Commit `chore: remove MAUI GUI project`.
- [ ] **Step 2: `release.yml`** — on `push: tags: ['v*']`: matrix over `rid: [win-x64, osx-arm64, osx-x64, linux-x64]`; `dotnet publish MineCity2000.Cli/MineCity2000.Cli.csproj -c Release -r ${{ matrix.rid }} --self-contained true -o out/${{ matrix.rid }}` (the `CopyBuildingsAfterBuild` target copies into `$(OutDir)`, not the publish dir, so add two explicit steps: `cp -r buildings out/<rid>/buildings` and `cp MineCity2000.Cli/default_args.txt out/<rid>/`), zip as `mc2k-cli-${{ matrix.rid }}.zip`, attach to the GitHub release with `softprops/action-gh-release@v2`. Release notes link to the Pages URL for the web app.
- [ ] **Step 3: README** — rewrite "Download link", "Running the compiled app", "Projects", "building from source" sections: web app URL first (zero install), then CLI zips per OS (run `./MineCity2000 city.sc2`; on macOS first run may need `xattr -d com.apple.quarantine`), note that `.rar` links are historical. Mention `dotnet test`. Keep Changelog and "If you would like to help".
- [ ] **Step 4:** Tag `v1.0.0-rc1`, confirm the release workflow produces four zips and that the win-x64 zip runs on the owner's Windows machine. Commit `ci: build CLI release zips for all platforms`, `docs: update README for web app and cross-platform CLI`.

---

## Verification (whole epic)

1. After each PR: `dotnet test` green locally and on all three CI OSes.
2. After PR 3: the golden test passing on `windows-latest` and `macos-latest` from the same baseline is the cross-platform proof.
3. After PR 5: record before/after `/usr/bin/time -l` numbers in the PR description.
4. After PR 6: convert `test.sc2` in Chrome, Firefox and Safari; unzip the download; `WorldComparer`-style check (a small script or the test itself pointed at the unzipped folder) shows it equals `default.zip` baseline chunk-for-chunk. Then the owner opens the world in Minecraft on Windows as the final end-to-end check.
5. After PR 7: tagged release produces runnable zips; README links resolve.

## Suggested execution

PR 1–5 are small, interface-coupled and each has a crisp test gate; PR 6 is a greenfield project with new tooling. Recommended: subagent-driven development per PR with a fresh reviewer per task, since a silent output change (like the no-buildings-on-mac bug) is exactly what a second pair of eyes catches.
