# FCC

[![CodeFactor](https://www.codefactor.io/repository/github/mzknek/fcc/badge)](https://www.codefactor.io/repository/github/mzknek/fcc)

Console tool with an optional graphical interface for inspecting, grouping and
sizing the contents of a directory.

## Usage

```
fcc [options]
```

When no `-p` is given, the current directory is used.

## Options

| Option | Description |
| ------ | ----------- |
| `-h`, `--help` | prints help |
| `-a` | include hidden files and directories |
| `-c` | enable colored output |
| `-s` | include group size in output |
| `-g` | calculate avg file size in group (only with `-s`) |
| `-d` | print names of subdirs (only with `-r`) |
| `-r` | enable recursive mode |
| `-v` | enable verbose mode (one line per file) |
| `-p <dir>` | path to directory |
| `--out <dir>` | directory where the output is saved (one `.fcc` file) |
| `--min <n>` | min length of the common name prefix used for grouping (default 20) |
| `--more <n>` | min allowed count in a group |
| `--less <n>` | max allowed count in a group |
| `--rand` | print one random entry |
| `--version` | print program version |
| `--gui` | launch the graphical interface |

## Graphical interface

Run with `--gui` to open a window:

```
fcc --gui
```

Pick a directory and tick the options (hidden, recursive, group size, average,
verbose, random, group filters). The list refreshes automatically when the
directory or an option changes; there is no separate run button.

- Results are shown one row per entry with the columns `Name`, `Count`, `Size`
  and `Unit`. Enabling `Subdir names (-d)` adds a `Dir` column.
- Click a column header to sort. `View > Size unit` switches the displayed unit
  (Auto, Bytes, KiB, MiB, GiB, TiB).
- Right-click a row to `Open`, `Open in Explorer` or `Copy path`. For a grouped
  entry, `Open` launches the first file of the group.
- `File > Save output...` (Ctrl+S) writes the text report to a `.fcc`/`.txt`
  file.

## Releases

Pushing a tag like `v1.1.0` triggers the release workflow, which builds a
self-contained single-file `fcc.exe` for Windows x64 and attaches it to a
GitHub release:

```sh
git tag v1.1.0
git push origin v1.1.0
```

## Grouping

Files are grouped by the longest common prefix of adjacent names. Only prefixes
longer than `--min` characters qualify, so short or unrelated names stay as
separate entries. Use `--more` / `--less` to filter groups by their file count.

## Colors

Colors are enabled with `-c` and can be customized through environment variables:

| Variable | Meaning | Default |
| -------- | ------- | ------- |
| `FCC_COLORS` | when set (any value), the overrides below are applied | – |
| `FCC_QC` | quotes and counts | `#D33682` |
| `FCC_DC` | directory and size | `#586E75` |
| `FCC_FC` | file and group name | `#2AA198` |
| `FCC_BC` | background | `#002B36` |

## Build and test

Requires the .NET 8 SDK. The GUI uses WinForms, so the app targets Windows.

```sh
dotnet build src/fcc.csproj
dotnet test tests/fcc.Tests/fcc.Tests.csproj
```

Publish a self-contained `win-x64` binary:

```sh
dotnet publish src/fcc.csproj
```

Publish a single-file executable (as used by the release workflow):

```sh
dotnet publish src/fcc.csproj -c Release -r win-x64 -p:PublishSingleFile=true
```

## Exit codes

| Code | Meaning |
| ---- | ------- |
| 0 | success |
| 1 | invalid arguments |
| 2 | insufficient permissions while reading |
| 3 | output could not be saved / I/O error |

## License

Released under the [MIT License](LICENSE). Copyright (c) 2023-2026 Sniku.
