# TextPlus2

<p align="center">
  <img src="resources/TextPlus2Logo.png" width="112" alt="TextPlus2 logo">
</p>

A small, simple text editor, that looks good.

The successor to [TextPlus](https://github.com/Nan0MK/TextPlus).

<p align="center">
  <img src="docs/images/editing-dark.png" alt="TextPlus2 in the dark theme. Editor.cs is colored, Guide.md and notes.txt are open in tabs, and the Workbench folder is listed in the file tree.">
</p>

## Features

- Syntax highlighting.
- Tabs.
- A file tree.
- Word Wrap.
- Light and dark modes.
- Standard editing capabilities.

### Markdown

![Guide.md in the light theme. Headings, emphasis, inline code, a C# fence, and a link are colored in the source.](docs/images/markdown-light.png)

### Find

![The find bar contains IsDirty, that word is selected in Editor.cs, and the status bar says Find: match.](docs/images/find.png)

### Word wrap

A long line stays one line in the file. With word wrap on, line 3 folds inside the window and the line number stays 3. `notes.txt` has no grammar, so it stays plain.

![notes.txt in the light theme with word wrap on. Line 3 folds across the editor while the line number stays 3.](docs/images/word-wrap.png)

## Shortcuts

| Action | Shortcut |
| --- | --- |
| New | Ctrl+N |
| Open | Ctrl+O |
| Save | Ctrl+S |
| Save As | Ctrl+Shift+S |
| Close tab | Ctrl+W |
| Switch tab | Ctrl+Tab, Ctrl+Shift+Tab |
| Find | Ctrl+F |
| Find Next | F3 |
| File tree | Ctrl+B |
| Zoom | Ctrl+Plus, Ctrl+Minus, Ctrl+0, Ctrl+scroll |

Undo, redo, cut, copy, paste, and select all use the usual editor shortcuts.

## Run

The current version is **26_1.0.0**. The app targets .NET 10. From the repo root:

```powershell
dotnet run --project csharp
```

`run-csharp.ps1` does the same thing and forwards arguments.

```powershell
dotnet run --project csharp -- path\to\file.txt
dotnet run --project csharp -- path\to\folder
```

`build-release.ps1 -Version 26_1.0.0` writes self-contained Windows and Linux builds to `releases/`. That folder stays on your machine. The Windows executable is over GitHub's 100 MiB file limit, so the builds are not in this repository. `publish-release.ps1 -Version 26_1.0.0` uploads a built folder to a GitHub Release. On Linux, mark the binary executable if the file system did not: `chmod +x TextPlus2`.

Pass a file to open it, or a folder to root the file tree there.

The interface is [ChiselUI](chiselui-avalonia/README.md). The editor is AvaloniaEdit with TextMate grammars, and the editor face is JetBrains Mono SemiBold.

## This release leaves out

Replace, language servers, and a rendered markdown preview. Theme, zoom, the file tree, follow-open-file, and word wrap last for the session only.
