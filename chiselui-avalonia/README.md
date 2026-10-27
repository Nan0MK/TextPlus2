# ChiselUI for Avalonia

ChiselUI is a design system that gives an app the feel of 2000s desktop software (Aero glass, Aqua gel, dense IDE layouts) built with modern depth. This repo is a port of the web version (`chiselui.css` / `tokens.json`) to **Avalonia 11.3 / .NET 8**.

```
ChiselUI.sln
├─ src/ChiselUI.Avalonia/      the library: theme, tokens, ControlThemes, ChiselWindow, GlassFrame, AuroraBackground, AccentPicker, GroupBox
├─ samples/ChiselUI.Demo/      demo app: showcase (IDE + dialog, component gallery, elevation row) and the file explorer
├─ tools/ChiselUI.Shots/       headless Skia renderer that writes the light/dark screenshots to ./shots
├─ shots/                      rendered screenshots
└─ ChiselUI-Demo.exe           self-contained win-x64 single-file build (no .NET install needed)
```

## Run the demo
Double-click `ChiselUI-Demo.exe` on Windows 10/11 x64. To build it yourself (requires the .NET 8 SDK):

```
dotnet run --project samples/ChiselUI.Demo -c Release
dotnet publish samples/ChiselUI.Demo -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The publish uses ReadyToRun for faster startup. Native Skia/HarfBuzz are extracted inside the single file.

## Use the library
1. Reference `src/ChiselUI.Avalonia` (a project reference, or `dotnet pack` it). Dependencies: Avalonia, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter and Avalonia.Controls.DataGrid.
2. Add the theme in `App.axaml`. It replaces `<FluentTheme/>`, which it already includes along with the DataGrid theme.

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:chisel="clr-namespace:ChiselUI.Avalonia;assembly=ChiselUI.Avalonia"
             x:Class="MyApp.App" RequestedThemeVariant="Light">
  <Application.Styles>
    <chisel:ChiselTheme Accent="#2f6bd8"/>
  </Application.Styles>
</Application>
```

3. In `Program.cs` call `.UseChiselUI()` on the AppBuilder. It registers the Inter font.
4. For the frosted frame, derive windows from `ChiselWindow`:

```xml
<c:ChiselWindow xmlns:c="clr-namespace:ChiselUI.Avalonia.Controls;assembly=ChiselUI.Avalonia"
                Title="My tool" Width="1200" Height="800">
  ...
</c:ChiselWindow>
```

`ChiselWindow` uses `ExtendClientAreaToDecorationsHint` and draws its own chrome:
- A 10px frosted frame with a 3px accent depth band and no outlines.
- The content area lifted off the frame.
- Gel min/max/close buttons, a draggable title bar, and resize grips on every edge.

It requests `AcrylicBlur → Mica → Blur`. If the OS refuses all three, it falls back to an opaque tinted frame. Properties:
- `IconContent`
- `TitleBarContent`
- `IsDialogFrame` (close button only)
- `UseOsGlass` (set it to false to force the tinted fallback)

### Runtime switching
```csharp
ChiselTheme.ToggleTheme();                       // or ChiselTheme.SetTheme(ThemeVariant.Dark)
ChiselTheme.SetAccent(Color.Parse("#139a92"));   // any colour; the 7 curated ones are in ChiselTheme.Swatches
```
Light and dark are standard Avalonia `ThemeVariant`s. When the accent changes, every derived token is rebuilt: the gels, focus rings, selection, aurora desktop and frame tint. Drop `<c:AccentPicker/>` anywhere to get the 7-swatch picker.

### Controls
Standard Avalonia controls get ChiselUI ControlThemes automatically:
- Button, RepeatButton, ToggleButton, CheckBox, RadioButton, ToggleSwitch
- TextBox, NumericUpDown, ComboBox, Slider, ProgressBar
- TabControl, TabItem, Menu, ContextMenu, MenuItem, ToolTip
- ListBox, TreeView, DataGrid, ScrollViewer, ScrollBar, Expander

The library also adds these controls:

| Control | Purpose |
|---|---|
| `ChiselWindow` | Real window with the glass frame |
| `GlassFrame` | In-app window frame (title, icon, caption buttons, `IsDialog`, `IsInactive`), for MDI-style or showcase layouts |
| `AuroraBackground` | Vista-style aurora desktop drawn with gradients (a Decorator) |
| `AccentPicker` | The 7 gel swatches |
| `GroupBox` | Header + sunken well |

### Style classes (mirroring the CSS)
| Element | Classes |
|---|---|
| `Button` | `primary`, `danger`, `sm`, `tool`, `segment`, `icon`, `orb`, `swatch` |
| `TabControl` | `doc` (document tabs). Property-sheet tabs are the default. |
| `Border` | `well`, `wellsm`, `sunken`, `panel`, `field`, `chrome`, `toolbar`, `statuscell`, `dock`, `dockheader`, `card`, `cardheader`, `glasscard`, `editor`, `console`, `badge` (+ `success` / `warning` / `danger` / `neutral` / `soft`), `popup`, `tooltip`, `elev1`…`elev5` |
| `TextBlock` | `h2`, `sm`, `xs`, `muted`, `secondary`, `danger`, `glass` (text on glass), `desktop` (text on the aurora) |
| `ListBox`, `TreeView` | `flat` |
| `TextBox` | `search`, `mono`, `invalid` |

### Tokens
All tokens are resources in the `ThemeDictionaries`, so use `{DynamicResource …}`. Each colour token `ChX` exists as both a `Color` (`ChX`) and a `SolidColorBrush` (`ChXBrush`).

| Group | Keys |
|---|---|
| Surfaces | `ChSurfaceWell/Base/Panel/Raised/Float/Field/Window/Dialog/Popup` |
| Text | `ChText`, `ChTextSecondary`, `ChTextMuted`, `ChTextDisabled`, `ChGlassText`, `ChDesktopText` |
| Accent | `ChAccent`, `ChAccentHi`, `ChAccentLo`, `ChAccentEdge`, `ChAccentGlow`, `ChAccentText`, `ChSelection`, `ChFocusRing`, `ChAccentGelBrush` |
| Status | `ChSuccess`, `ChWarning`, `ChDanger` |
| Elevation (BoxShadows, including inset) | `ChElevSunken`, `ChElevSunkenSm`, `ChElev1` … `ChElev5`, `ChButtonShadow`, `ChFieldFocusShadow`, `ChFocusRingShadow`, … |
| Concave dishes | `ChDishWellBrush`, `ChDishFieldBrush`, `ChDishLgEditorBrush`, `ChDishLgConsoleBrush` |
| Glass | `ChGlassFrameBrush`, `ChGlassFrameTintBrush`, `ChGlassFrameFallbackBrush`, `ChGlassFrameShadow`, `ChContentLift`, `ChGlassCardBrush` |
| Desktop | `ChDesktopBaseA/B`, `ChDesktopAurora1/2/3` |
| Editor | `ChEditor`, `ChGutter`, `ChGutterText`, `ChCurrentLine`, `ChSynKw/Type/Str/Num/Com/Pp`, `ChConsole` |
| Fonts | `ChFontSans` (Inter → Segoe UI), `ChFontMono` (JetBrains Mono, bundled → Cascadia Mono → Consolas) |

The token values are computed in `ChiselTokens.cs` with the same `color-mix` formulas as `chiselui.css`, which keeps the accent swap exact.

## Performance notes
- Depth comes from `BoxShadows` (outer and inset) plus linear and radial gradients. There is no per-element blur and there are no `Effect`s, so everything stays on the Skia fast path.
- Brushes and shadows are built once per theme/accent change and shared as immutable resources.
- The aurora renders once per size change: four radial glows plus three linear passes.
- ControlThemes are keyed by type with compiled bindings. There is no reflection binding in the demo.
- The animations are the toggle-knob slide and hover transitions only.

## Faithful vs approximated
**Faithful (same tokens and formulas as the web build)**
- The colour system: surfaces, text, status colours and the 7 accents, with light/dark switching through ThemeVariant.
- The elevation scale −1…5, using outer plus inset BoxShadows with the same offsets, blur and alpha.
- Gel buttons and accent gels: gloss cap, bottom glow and edge ring. Concave dish wells, fields and the editor/console.
- The frosted window frame: 10px frame, 3px accent depth band, no outlines, the content lift, and the gel caption buttons with the red close button.
- The tabs (document tabs and property sheets), segmented controls, toolbars and status cells.
- The menus/popups at elevation 5, the tooltip and the scrollbars (sunken track with a gel thumb).
- The DataGrid (header and banding), the tree, badges and alerts.
- The aurora desktop: same layers, positions and radii as the CSS `background-image`.
- The layout of the demo showcase and the explorer.

**Approximated**
- **Backdrop blur:** in-app `GlassFrame`s (inside a window) use a translucent tint without blurring what's behind them. Avalonia has no per-element backdrop filter. Real `ChiselWindow`s get true OS acrylic/Mica on Windows 10/11, with a tinted fallback elsewhere.
- **No film grain or noise** overlay.
- **No `text-shadow`:** the glow behind title text on glass is dropped.
- **Progress bar stripes are static.** The web version animates them.
- **CSS radial gradients:** these are approximated with Skia radial brushes, which use slightly different colour interpolation.
- **Window corners** are the OS's rounded corners (Windows 11) rather than the CSS radius. Drop shadows on real windows come from the OS. ChiselUI only draws them on in-app frames.
- **Menu/tooltip gallery card:** in the demo this is a static preview (`Border.popup` / `Border.tooltip`). The real `ContextMenu`/`ToolTip` popups use the same theme.
- **Fonts:** Inter is used. The web build's system-font stack may render slightly differently.

## Screenshots
Generated headlessly by `tools/ChiselUI.Shots`:

```
dotnet run --project tools/ChiselUI.Shots -c Release -- ./shots
```

The tool exercises the controls (clicks, combo drop-downs, toggles, tab switching) and fails the run on any Avalonia warning. Use `--accent` to also render the showcase with the teal accent (#139a92), or `--quick` to skip the control exercise.
