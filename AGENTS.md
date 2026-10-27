# TextPlus2 handoff

Read this before changing the app. `TextPlus2.md` is the product brief. Leave it unchanged. `README.md` is the GitHub page. Its screenshots in `docs/images/` were captured from the 26_1.0.0 Windows build. `csharp/README.md` is the short build note.

The workspace `C:\DEV\LOCAL2\TextPlus2` is not a git repo. The C# app in `csharp/` is the product. The Odin bake-off is gone from the tree. Do not recreate `odin/`. `run-odin.ps1` is leftover. Leave it unless asked.

## Run and build

From the repo root, `run-csharp.ps1` runs `dotnet run --project csharp` and forwards arguments.

- A file argument opens that file. The tree root is the file's parent.
- A directory argument roots the tree there and does not open a document.
- No argument lists ready drives on Windows, or `/` on Linux. It does not always start at `C:\`.

```powershell
dotnet build csharp\TextPlus2.csproj --nologo
```

The last verified build was `csharp\bin\Debug\net10.0\TextPlus2.dll`, 0 warnings. If `TextPlus2.exe` is locked, build to another `-o` folder. Do not kill a TextPlus2 process you did not start.

`releases/` is gitignored. The Windows builds are over GitHub's 100 MiB limit, so they are not in the repository. Release `26_1.0.0` is the current self-contained drop in `releases/26_1.0.0/` on a machine that has built it. `build-release.ps1` republishes that version. The assembly file version is `26.1.0.0` and the product version is `26_1.0.0`. The Windows exe was started and showed `TextPlus2 - Untitled`, with the logo, the drives tree, and an Untitled tab. The Linux file is an ELF binary and was not run here. `releases/26_0.0.0` remains. That cut was built before the logo.

Stay on Avalonia 11.3.22. Do not take Avalonia 12 or the AvaloniaEdit 12 packages. `Avalonia.AvaloniaEdit` 11.4.1, `AvaloniaEdit.TextMate` 11.4.1, and `TextMateSharp.Grammars` 2.0.2 are the editor stack. ChiselUI is `chiselui-avalonia/src/ChiselUI.Avalonia`, net8.0. The app is net10.0. A transitive NU1903 on Tmds.DBus.Protocol 0.21.2 is known.

## Where the code lives

- `csharp/MainWindow.axaml` — menu, file tree, editor.
- `csharp/MainWindow.axaml.cs` — document, save, find, zoom, theme, word wrap.
- `csharp/MainWindow.FileTree.cs` — tree, Open Folder, follow, reveal.
- `csharp/MainWindow.Highlight.cs` — TextMate install, grammar, editor face and theme.
- `chiselui-avalonia/src/ChiselUI.Avalonia/Styles/Navigation.axaml` — menu, context, and flyout popup shadows.

`ChiselTheme` already includes Fluent. Do not add `FluentTheme` again. `UseChiselUI()` is called from `Program.cs`. Accent is `#2f6bd8`. `MainWindow` and the unsaved-changes dialog are `ChiselWindow`.

## What works

Checked on Windows after the menu, wrap, and folder work.

- New, Open, Save, Save As, Exit. Undo, redo, cut, copy, paste, select all. Find and Find Next (Ctrl+F, F3). There is no replace.
- Light/dark follows the OS at startup. The menu-bar button and View → Switch theme call `ChiselTheme.ToggleTheme()` for the rest of the session. The button label is the destination (`Light` while the theme is dark).
- Zoom is 50% to 300% in steps of 10, shown in the status bar. Ctrl+scroll, Ctrl+Plus, Ctrl+Minus, and Ctrl+0. Numpad and Shift+OemPlus are handled in `OnKeyDown` after `base.OnKeyDown`, and only when the event is not already handled.
- Line numbers are AvaloniaEdit's. Do not toggle `ShowLineNumbers` after load. That crashes (AvaloniaEdit issue 434).
- Syntax highlighting is TextMate, chosen from the extension. Unknown extensions stay plain (`.txt`, `.odin`). `.log` uses `text.log`. Markdown is the markdown grammar on the same editor, colored source, not a rendered preview. The user dropped a custom markdown view. The document text stays the source of truth for save, undo, and the dirty flag.
- The editor and line numbers use static JetBrains Mono SemiBold (`csharp/Assets/Fonts/JetBrainsMono-SemiBold.ttf`, resource `EditorFont`) at 15px when zoom is 100%. Window chrome stays Inter. Do not point the editor at `ChFontMono`. That file is a variable font, and Avalonia does not apply `FontWeight` to it.
- File tree on the left, Chisel `TreeView`, Inter, not the editor font. View → File Tree and Ctrl+B hide and show it for the session. It starts visible. A single left click or Enter opens a file. A double-click expands a folder; the twisty still toggles. The same path does not reload, it selects the tab that already has it. Hidden and system files are skipped. There is no `..`. Folders load when expanded. A directory keeps one placeholder child so the twisty stays.
- File → Open Folder sets the tree root and does not change the document. The picker starts in the current folder. Once a folder is the root, opening a file in a subfolder keeps that root and expands ancestors to the file. View → Follow Open File is off. Turning it on makes Open, Save As, and opening from the tree retarget to that file's folder. With no folder chosen yet, the first opened file still roots the tree at the file's parent. New keeps the current root. Save As of a new name relists only when follow is on or the tree has no folder yet. Same-folder save of an existing file does not rebuild the tree.
- View → Word Wrap is off. It sets `Editor.WordWrap` for the session. AvaloniaEdit wraps by turning the horizontal scrollbar off.
- While one menu is open, moving onto another header opens that menu. Top-level popups use margin `40,0,40,62` and vertical offset `0`, so the card sits flush under the header and does not cover the bar. Submenu, context, and flyout popups keep margin `40,18,40,62` and the old offsets. Do not put the top margin back on top-level menus, and do not shrink `ChElev5`.
- The logo is `resources/TextPlus2Logo.ico` and `resources/TextPlus2Logo.png`. The png is transparent. The title bar shows it at 22px on the main window and the unsaved-changes dialog. The ico is the Windows executable icon and `Window.Icon`. The svg is the source drawing and is not loaded. `releases/26_1.0.0` includes the logo. `releases/26_0.0.0` was built before the logo, so that drop still has the default icon.
- More than one file stays open. The strip is a Chisel `TabControl` with class `doc`, above the one shared editor. Each tab keeps its own `TextDocument`, so undo and the caret stay with that file. New adds a tab. The first open replaces the blank tab; later opens add tabs. File → Open can pick several files. File → Close and Ctrl+W close the current tab. The × on the tab does the same. Ctrl+Tab and Ctrl+Shift+Tab switch tabs; that shortcut is handled on the tunnel because the editor otherwise takes Tab. Closing the last tab leaves a new blank tab. An untouched blank tab is not closed when it is the only one. A dirty tab asks before it closes. Closing the window asks once if any tab is dirty. Save As onto a path that another tab has closes that other tab, and asks first when that tab is dirty.

Theme, zoom, tree visibility, follow, and word wrap are not saved across launches.

## Do not start unless asked

Replace, LSP, a markdown preview, show-hidden, and a polish pass. The user left LSP out of `26_1.0.0` and called the app mostly complete. Still do not open a polish pass, replace, or a markdown preview on your own. The app logo is already applied. Do not add other icons unless asked.

Do not restyle the Chisel `TreeView`, and do not restyle `Navigation.axaml`, `Surfaces.axaml`, `Inputs.axaml`, or `ChiselTheme.axaml.cs` beyond the menu-shadow and top-level popup offsets already there.

## Regressions that already cost a session

- Dirty state is the window's `_dirty` flag, not `TextEditor.IsModified`. Undo does not clear it. Newlines are stored as `\n` and written with `Environment.NewLine` on Windows. After a load, a `ContextIdle` post keyed by `_documentGeneration` clears a false dirty flag when the normalized text still matches.
- `TextEditor.Text` resets the caret and clears the undo stack. AvaloniaEdit's search panel is uninstalled on load so Ctrl+F stays the window find bar. Find is ordinal and case-sensitive.
- `AfterModalAsync` must not call `EnableWindow` synchronously inside a `ShowDialog` `Closed` handler. Keep the background, `ContextIdle`, and +250ms restore. A synchronous restore leaves the window dead and Windows plays the error sound.
- Ctrl+B does not flip `IsChecked`. A pointer click flips it before `Click`. `FileTree_Click`, `FollowOpenFile_Click`, and `WordWrap_Click` toggle their own flag and then write `IsChecked`.
- Folder-label double-click needs the tunnel snapshot of `IsExpanded` plus the bubble handler with `handledEventsToo`. Do not open files from the tunnel handler. During the tunnel, `e.Source` is not the row.
- `Path.GetFullPath("C:")` is the current directory on drive C, not `C:\`. Compare directories with `DirectoryKey`, which appends a trailing separator. Skip `DriveType.Network` and `NoRootDirectory` before `IsReady`.
- Do not paint the TextMate editor background. The editor background stays transparent so the Chisel dish shows through. The caret uses `ChTextBrush`. Line numbers use `ChTextMutedBrush`.
- `ApplyEditorFace` sets font family, SemiBold weight, and size with `TextElement` on the `TextView` and each left-margin control. Those are attached properties. The controls do not have `FontFamily` properties of their own.
- A failed tree open reselects the current document, including a file nested under the pinned root. Opening another file does not ask to discard the one on screen.

## Checking the UI

This is a desktop window, not a web app. On Windows, drive it with Windows PowerShell 5.1 (`powershell.exe`), UIAutomation, and user32. Call `SetProcessDpiAwarenessContext(-4)` before reading bounds or capturing. A useful window is about 1000×680, placed near the right edge of the screen.

The tree header and most status text are not exposed. `ZoomCell` has an automation name. Tree rows expose their file name. The Open Folder item's automation name is `Open Folder`, without the ellipsis. A missing name is not a product bug. Confirm behavior: open File, move to Edit, and Undo must be visible without dismissing first. For wrap, a long line is one row with a horizontal scrollbar, then several visual rows on line 1 after Word Wrap. For the tree, open a file under a subfolder and the root folder name must stay put until Follow Open File is checked.
