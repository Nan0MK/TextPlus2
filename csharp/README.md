# TextPlus2

The app project is `TextPlus2.csproj`. It is an Avalonia 11 window on ChiselUI, with AvaloniaEdit for the document. The [root README](../README.md) has screenshots, the feature list, and the current release.

```powershell
dotnet run --project csharp
```

From the repo root, `run-csharp.ps1` runs that and forwards arguments. Pass a file to open it, or a folder to root the file tree there.

```powershell
dotnet run --project csharp -- path\to\file.txt
dotnet run --project csharp -- path\to\folder
```

`build-release.ps1` writes self-contained Windows and Linux builds into `releases/`. That folder is gitignored.
