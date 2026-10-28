# TextPlus2

<p align="center">
  <img src="resources/TextPlus2Logo.png" width="112" alt="TextPlus2 logo">
</p>

A small text editor for Windows and Linux, and the successor to [TextPlus](https://github.com/Nan0MK/TextPlus).

TextPlus2 keeps several files open in tabs, colors source from the file extension, and shows a file tree beside the editor. The window follows the system theme at startup. Light and dark can be switched for the rest of the session.

<p align="center">
  <img src="docs/images/editing-dark.png" alt="TextPlus2 in the dark theme. Editor.cs is colored, Guide.md and notes.txt are open in tabs, and the Workbench folder is listed in the file tree.">
</p>

## Features

- Syntax highlighting chosen from the file extension. An unknown extension stays plain text.
- Markdown stays in the editor. The markdown grammar colors headings, code, and links in the source.
- Document tabs. Each tab keeps its own text, undo stack, and caret.
- A file tree. A click opens a file. Open Folder sets the root and leaves the current document alone.
- Find and Find Next.
- Line numbers, zoom from 50% to 300%, and word wrap.
- Light and dark themes, and the app icon in the title bar and on the Windows executable.

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


## This release leaves out

Replace, language servers, and a rendered markdown preview. Theme, zoom, the file tree, follow-open-file, and word wrap last for the session only.
