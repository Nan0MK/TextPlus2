using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using static ChiselUI.Avalonia.ChiselColor;

namespace ChiselUI.Avalonia;

/// <summary>
/// Builds every ChiselUI design token (colours, brushes, BoxShadows, metrics) for one theme variant
/// and one accent, mirroring build/light.css, build/dark.css and build/shared.css of the web build.
/// All resource keys start with "Ch".
/// </summary>
public sealed class ChiselTokens
{
    /// <summary>CSS blur radius → Skia/Avalonia blur. CSS uses sigma = blur/2; Avalonia uses ~0.29·blur+0.5.</summary>
    public const double BlurScale = 1.55;

    public bool IsDark { get; }
    public Color Accent { get; }
    public double DropK { get; }
    public double UiK { get; }
    public double UiDropK => DropK * UiK;
    public Color Drop { get; }

    readonly Dictionary<string, object> _r = new();
    public IReadOnlyDictionary<string, object> Resources => _r;

    public ChiselTokens(bool dark, Color accent)
    {
        IsDark = dark;
        Accent = accent;
        DropK = dark ? 2.2 : 1.15;
        UiK = dark ? 1.15 : 1.26;
        Drop = dark ? Color.FromRgb(0, 0, 0) : Color.FromRgb(18, 30, 52);
        Build();
    }

    Color D(double a) => WithAlpha(Drop, System.Math.Min(1, a));
    static Color Blk(double a) => Rgba(0, 0, 0, a);
    static Color Wht(double a) => Rgba(255, 255, 255, a);

    static BoxShadow S(double x, double y, double blur, double spread, Color c, bool inset = false)
        => new() { OffsetX = x, OffsetY = y, Blur = blur * BlurScale, Spread = spread, Color = c, IsInset = inset };
    static BoxShadow I(double x, double y, double blur, double spread, Color c) => S(x, y, blur, spread, c, true);
    static BoxShadows Sh(params BoxShadow[] s) => s.Length == 0 ? default : new BoxShadows(s[0], s.Skip(1).ToArray());

    static IBrush B(Color c) => new ImmutableSolidColorBrush(c);
    static IBrush V(params (double o, Color c)[] stops) => Grad(new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(0, 1, RelativeUnit.Relative), stops);
    static IBrush H(params (double o, Color c)[] stops) => Grad(new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(1, 0, RelativeUnit.Relative), stops);
    static IBrush Grad(RelativePoint s, RelativePoint e, (double o, Color c)[] stops)
        => new ImmutableLinearGradientBrush(stops.Select(t => new ImmutableGradientStop(t.o, t.c)).ToArray(), 1, null, null, GradientSpreadMethod.Pad, s, e);

    void Put(string key, object v) => _r[key] = v;
    void Col(string name, Color c) { Put("Ch" + name, c); Put("Ch" + name + "Brush", B(c)); }

    // gel cap composited onto a vertical body gradient (single brush, no extra layers)
    static IBrush GelOver(Color top, Color bottom, bool soft = false, Color? glow = null)
    {
        double lip = soft ? .40 : .85, a = soft ? .22 : .55, b = soft ? .06 : .22;
        Color Body(double t)
        {
            var c = Lerp(top, bottom, t);
            if (glow is { } g && t > .6) c = Over(WithAlpha(g, (t - .6) / .4 * (g.A / 255.0) * .55), c);
            return c;
        }
        return V((0, Over(Wht(lip), Body(0))), (.04, Over(Wht(a), Body(.04))), (.475, Over(Wht(b), Body(.475))),
                 (.48, Body(.48)), (.8, Body(.8)), (1, Body(1)));
    }

    void Build()
    {
        bool dk = IsDark;
        var acc = Accent;
        var white = Colors.White; var black = Colors.Black;

        // ---- surfaces & text -------------------------------------------------
        Col("SurfaceWell", Hex(dk ? "#131519" : "#dde1e8"));
        Col("SurfaceBase", Hex(dk ? "#1f2228" : "#e6e9ee"));
        Col("SurfacePanel", Hex(dk ? "#282b32" : "#f1f3f6"));
        Col("SurfaceRaised", Hex(dk ? "#353943" : "#f8f9fb"));
        Col("SurfaceFloat", Hex(dk ? "#3a3e48" : "#ffffff"));
        Col("SurfaceField", Hex(dk ? "#15171b" : "#ffffff"));
        Col("SurfaceWindow", Hex(dk ? "#3e424c" : "#ffffff"));
        Col("SurfaceDialog", Hex(dk ? "#474b56" : "#ffffff"));
        Col("SurfacePopup", Hex(dk ? "#50555f" : "#ffffff"));
        Col("SurfaceCheck", Hex(dk ? "#3b4049" : "#ffffff"));
        Col("ClientDialog", Hex(dk ? "#262930" : "#e9ecf1"));
        var field = Hex(dk ? "#15171b" : "#ffffff");
        var well = Hex(dk ? "#131519" : "#dde1e8");
        var check = Hex(dk ? "#3b4049" : "#ffffff");
        var raised = Hex(dk ? "#353943" : "#f8f9fb");
        var panel = Hex(dk ? "#282b32" : "#f1f3f6");
        var popup = Hex(dk ? "#50555f" : "#ffffff");

        Col("Text", Hex(dk ? "#e5e8ee" : "#1a1f27"));
        Col("TextSecondary", Hex(dk ? "#b9c0cb" : "#3e4653"));
        Col("TextMuted", Hex(dk ? "#8a929f" : "#667080"));
        Col("TextDisabled", Hex(dk ? "#5c636f" : "#a0a7b2"));
        Col("Success", Hex(dk ? "#4cc27a" : "#1f8a4c"));
        Col("Warning", Hex(dk ? "#e3a83a" : "#b57800"));
        Col("Danger", Hex(dk ? "#f0605a" : "#d0392f"));

        // ---- accent family -----------------------------------------------------
        var accHi = Mix(acc, .55, white);
        var accLo = Mix(acc, .72, black);
        var accEdge = Mix(acc, .60, black);
        var accGlow = Mix(acc, .45, Transparent);
        var softA = Mix(acc, .10, field);
        var softB = Mix(acc, .24, field);
        var softRing = Mix(acc, .45, Transparent);
        Col("Accent", acc);
        Col("AccentHi", accHi);
        Col("AccentLo", accLo);
        Col("AccentEdge", accEdge);
        Col("AccentGlow", accGlow);
        Col("AccentText", dk ? Mix(acc, .55, white) : Mix(acc, .78, black));
        Col("AccentFill", dk ? Mix(acc, .88, black) : Mix(acc, .92, black));
        Col("AccentSoftA", softA);
        Col("AccentSoftB", softB);
        Col("AccentSoftRing", softRing);
        Col("Selection", dk ? Mix(acc, .30, Hex("#1a1d22")) : Mix(acc, .16, white));
        Col("FocusRing", Mix(acc, .35, Transparent));

        // ---- chrome / depth primitives ----------------------------------------
        var chromeTop = Hex(dk ? "#464b56" : "#ffffff");
        var chromeBot = Hex(dk ? "#31353c" : "#e4e8ee");
        var hovTop = dk ? Mix(acc, .14, Hex("#424852")) : white;
        var hovBot = Mix(acc, .10, Hex(dk ? "#30343c" : "#e9edf3"));
        var spec = dk ? Wht(.13) : Wht(.95);
        var specSoft = dk ? Wht(.06) : Wht(.55);
        var shade = dk ? Blk(.36) : Rgba(18, 30, 52, .13);
        var edge = dk ? Blk(.55) : Rgba(18, 30, 52, .16);
        var edgeStrong = dk ? Blk(.75) : Rgba(18, 30, 52, .28);
        var edgeFaint = Mix(edge, .60, Transparent);
        var rim = dk ? Blk(.62) : Rgba(18, 30, 52, .24);
        var neoDark = dk ? Rgba(0, 0, 0, .62) : Rgba(28, 44, 74, .23);
        double[] lips = dk ? new[] { .085, .19, .23, .24, .28 } : new[] { 1.0, 1, 1, 1, 1 };
        var wellCatch = Wht(dk ? .05 : .68);
        var wellLip = Wht(dk ? .11 : .95);
        double dishTop = dk ? .25 : .125;
        var dishGlow = Wht(dk ? .075 : .55);
        var selA = dk ? Mix(acc, .40, Hex("#2c3038")) : softA;
        var selB = dk ? Mix(acc, .28, Hex("#252830")) : softB;
        var selLip = dk ? Wht(.12) : Wht(.85);
        var selRim = Mix(acc, .30, Transparent);
        Col("ChromeTop", chromeTop); Col("ChromeBottom", chromeBot);
        Col("Spec", spec); Col("SpecSoft", specSoft); Col("Shade", shade);
        Col("Edge", edge); Col("EdgeStrong", edgeStrong); Col("EdgeFaint", edgeFaint); Col("Rim", rim);
        Col("NeoDark", neoDark);
        Col("GrooveDark", dk ? Blk(.45) : Rgba(18, 30, 52, .12));
        Col("GrooveLight", dk ? Wht(.06) : Wht(.9));
        Put("ChChromeBrush", V((0, chromeTop), (1, chromeBot)));
        Put("ChChromeHoverBrush", V((0, hovTop), (1, hovBot)));
        Put("ChChromeSoftBrush", V((0, Mix(chromeBot, .6, chromeTop)), (1, chromeBot)));
        Put("ChChromeGelSoftBrush", GelOver(chromeTop, chromeBot, soft: true));
        Put("ChChromeHoverGelSoftBrush", GelOver(hovTop, hovBot, soft: true));
        Put("ChDockActiveHeaderBrush", GelOver(Mix(acc, .22, chromeTop), Mix(acc, .34, chromeBot), soft: true));
        Put("ChSelectBandBrush", GelOver(selA, selB, soft: true));
        Put("ChSelectBand", Sh(I(0, 1, 0, 0, selLip), I(0, 0, 0, 1, selRim)));
        Put("ChHoverWashBrush", B(Mix(acc, .08, Transparent)));
        Put("ChRowHoverBrush", B(Mix(acc, .07, Transparent)));
        Put("ChRowAltBrush", B(Mix(well, .45, Transparent)));
        Put("ChAccentSoftBrush", V((0, softA), (1, softB)));
        Put("ChAccentSoftPressedBrush", V((0, softB), (1, softA)));

        // ---- dish (concave fill) ---------------------------------------------------
        IBrush Dish(Color fill, double topK = 1, double midStop = .55, bool tail = true)
        {
            var top = Over(D(dishTop * topK * UiDropK), fill);
            var bot = Over(WithAlpha(dishGlow, dishGlow.A / 255.0 * .55), tail ? Over(D(dishTop * .25 * UiDropK), fill) : fill);
            return V((0, top), (midStop, fill), (.8, fill), (1, bot));
        }
        IBrush DishLg(Color fill)
        {
            var top = Over(D(dishTop * .9 * UiDropK), fill);
            return Grad(new RelativePoint(0, 0, RelativeUnit.Absolute), new RelativePoint(0, 18, RelativeUnit.Absolute), new[] { (0.0, top), (1.0, fill) });
        }
        Put("ChDishFieldBrush", Dish(field));
        Put("ChDishWellBrush", Dish(well));
        Put("ChDishCheckBrush", Dish(check, .45, .6, false));
        Put("ChDishLgFieldBrush", DishLg(field));
        var editor = Hex(dk ? "#181a1f" : "#ffffff");
        var console = Hex(dk ? "#14161a" : "#fbfcfd");
        Put("ChDishLgEditorBrush", DishLg(editor));
        Put("ChDishLgConsoleBrush", DishLg(console));

        // ---- elevation scale --------------------------------------------------------
        double u = UiDropK, k = DropK;
        Put("ChElevSunken", Sh(I(0, 2, 2, -1, D(.22 * u)), I(0, 7, 12, -7, D(.34 * u)), I(0, 0, 0, 1, edgeFaint), I(0, -1, 0, 0, wellCatch), S(0, 1, 0, 0, wellLip)));
        Put("ChElevSunkenSm", Sh(I(0, 1, 2, 0, D(.20 * u)), I(0, 4, 6, -4, D(.30 * u)), I(0, 0, 0, 1, edge), I(0, -1, 0, 0, wellCatch), S(0, 1, 0, 0, wellLip)));
        Put("ChElev1", Sh(I(0, 1, 0, 0, Wht(lips[0])), S(0, 0, 0, 1, edgeFaint), S(0, 1, 2, 0, D(.16 * u)), S(0, 5, 12, -4, D(.26 * u))));
        Put("ChElev2", Sh(I(0, 1, 0, 0, Wht(lips[1])), I(0, -1, 0, 0, shade), S(0, 0, 0, 1, rim), S(0, 1, 2, 0, D(.22 * u)), S(0, 7, 16, -5, D(.40 * u)), S(0, 14, 28, -14, D(.30 * u))));
        Put("ChElev3", Sh(I(0, 1, 0, 0, Wht(lips[2])), S(0, 0, 0, 1, rim), S(0, 3, 8, 0, D(.24 * k)), S(0, 28, 60, -12, Blk(.62)), S(0, 60, 120, -40, Blk(.50))));
        Put("ChElev4", Sh(I(0, 1, 0, 0, Wht(lips[3])), S(0, 0, 0, 1, rim), S(0, 4, 10, 0, D(.28 * k)), S(0, 36, 76, -14, Blk(.70))));
        Put("ChElev5", Sh(I(0, 1, 0, 0, Wht(lips[4])), S(0, 0, 0, 1, rim), S(0, 4, 8, 0, D(.24 * u)), S(0, 22, 44, -8, Blk(.55))));
        var hdr = Sh(I(0, 1, 0, 0, Wht(lips[1])), S(0, 1, 0, 0, D(.10 * u)), S(0, 2, 4, -1, neoDark));
        Put("ChHeaderShadow", hdr);

        // ---- glass frame ---------------------------------------------------------------
        var glassSide = dk ? Mix(acc, .72, black) : Mix(acc, .82, Hex("#071640"));
        var frameGlow = Mix(acc, .36, Transparent);
        Color fTint, fTint2, fSolid, fSolid2;
        if (dk)
        {
            fTint = Mix(Mix(acc, .44, Hex("#3a4458")), .34, Transparent);
            fTint2 = Mix(Mix(acc, .32, Hex("#222a38")), .44, Transparent);
            fSolid = Mix(Mix(acc, .42, Hex("#3a4458")), .44, Transparent);
            fSolid2 = Mix(Mix(acc, .30, Hex("#222a38")), .54, Transparent);
        }
        else
        {
            fTint = Mix(Mix(acc, .16, white), .30, Transparent);
            fTint2 = Mix(Mix(acc, .30, Hex("#eef4ff")), .38, Transparent);
            fSolid = Mix(Mix(acc, .14, white), .40, Transparent);
            fSolid2 = Mix(Mix(acc, .28, Hex("#eef4ff")), .48, Transparent);
        }
        var wash = Wht(dk ? .08 : .12);
        Col("GlassFrameSide", glassSide);
        Col("GlassFrameGlow", frameGlow);
        Put("ChGlassFrameBrush", V((0, Over(wash, fSolid)), (.12, fSolid), (1, fSolid2)));
        Put("ChGlassFrameTintBrush", V((0, Over(wash, fTint)), (.12, fTint), (1, fTint2)));
        // Opaque fallback for real windows when no OS blur is available: the frame as it reads over the aurora desktop
        var deskA = dk ? Mix(acc, .42, Hex("#0b1526")) : Mix(acc, .70, Hex("#1b3d6b"));
        var deskB = dk ? Mix(acc, .12, Hex("#04070d")) : Mix(acc, .35, Hex("#0d1b33"));
        var behind = Mix(deskA, .5, deskB);
        var frost = dk ? Rgba(40, 46, 58, .55) : Rgba(236, 242, 252, .62);
        Put("ChGlassFrameFallbackBrush", V((0, Over(wash, Over(fSolid, Over(frost, Mix(behind, .8, white))))), (.12, Over(fSolid, Over(frost, Mix(behind, .85, white)))), (1, Over(fSolid2, Over(frost, behind)))));
        // Over real OS acrylic the slab must be a touch lighter than the CSS (acrylic already darkens)
        Put("ChGlassFrameAcrylicBrush", V((0, Over(wash, fTint)), (.12, fTint), (1, fTint2)));
        Put("ChGlassFrameShadow", Sh(S(0, 3, 0, 0, glassSide), S(0, 8, 18, -8, frameGlow), S(0, 2, 6, 0, D(.16 * k)), S(0, 20, 44, -12, Blk(.36)), S(0, 44, 90, -40, Blk(.30))));
        Put("ChGlassFrameShadow4", Sh(S(0, 3, 0, 0, glassSide), S(0, 10, 28, -8, frameGlow), S(0, 3, 8, 0, D(.18 * k)), S(0, 26, 56, -14, Blk(.44))));
        Put("ChGlassFrameShadowLite", Sh(S(0, 3, 0, 0, glassSide), S(0, 8, 18, -8, frameGlow), S(0, 2, 6, 0, D(.16 * k))));
        var contentLip = Wht(dk ? .07 : .9);
        Put("ChContentLift", Sh(I(0, 1, 0, 0, contentLip), S(0, 0, 3, 0, D(.10 * u)), S(0, 1, 2, 0, D(.22 * u)), S(0, 4, 8, -2, D(.24 * u)), S(0, 9, 16, -7, D(.34 * u))));
        Col("GlassText", Hex(dk ? "#f2f6ff" : "#10213d"));
        Col("GlassTextGlow", dk ? Blk(.75) : Wht(.95));
        var glassTint = dk ? Mix(acc, .30, Rgba(22, 28, 40, .55)) : Mix(acc, .34, Rgba(236, 243, 255, .55));
        var glassTint2 = dk ? Mix(acc, .14, Rgba(18, 22, 30, .62)) : Mix(acc, .18, Rgba(255, 255, 255, .62));
        Put("ChGlassCardBrush", V((0, glassTint), (1, glassTint2)));
        Put("ChGlassSheenBrush", Grad(new RelativePoint(0, 0, RelativeUnit.Relative), new RelativePoint(1, .4, RelativeUnit.Relative),
            new[] { (0.0, Transparent), (.25, Transparent), (.33, Wht(.18)), (.44, Transparent), (1.0, Transparent) }));
        Put("ChGlassCardShadow", Sh(I(0, 1, 0, 0, Wht(dk ? .14 : .65)), I(0, 0, 0, 1, Wht(.12)), S(0, 8, 24, -8, Blk(.45))));

        // ---- desktop (aurora) ------------------------------------------------------------
        Col("DesktopBaseA", deskA);
        Col("DesktopBaseB", deskB);
        Col("DesktopAurora1", dk ? Mix(acc, .45, Hex("#1fa395")) : Mix(acc, .40, Hex("#6ee7d8")));
        Col("DesktopAurora2", dk ? Mix(acc, .45, Hex("#6a3fc4")) : Mix(acc, .45, Hex("#b07cff")));
        Col("DesktopAurora3", Wht(dk ? .10 : .45));
        Col("DesktopText", Hex(dk ? "#eef2f8" : "#ffffff"));

        // ---- buttons ----------------------------------------------------------------------
        double uk = u;
        Put("ChButtonBrush", V((0, chromeTop), (.8, chromeBot), (1, Over(WithAlpha(specSoft, specSoft.A / 255.0 * .5), chromeBot))));
        Put("ChButtonHoverBrush", V((0, hovTop), (.8, hovBot), (1, Over(WithAlpha(specSoft, specSoft.A / 255.0 * .5), hovBot))));
        Put("ChButtonPressedBrush", V((0, chromeBot), (1, Mix(chromeBot, .8, chromeTop))));
        Put("ChButtonDisabledBrush", V((0, raised), (1, panel)));
        Put("ChGlossCapBrush", V((0, Wht(dk ? .20 : .95)), (1, Wht(dk ? .05 : .35))));
        Put("ChGelGlossBrush", V((0, Wht(.72)), (1, Wht(.14))));
        Put("ChButtonShadow", Sh(I(0, -1, 0, 0, shade), S(0, 0, 0, 1, edge), S(0, 1, 2, 0, D(.22 * uk)), S(0, 4, 9, -3, D(.32 * uk))));
        Put("ChButtonHoverShadow", Sh(I(0, -1, 0, 0, shade), S(0, 0, 0, 1, softRing), S(0, 1, 2, 0, neoDark), S(0, 0, 10, -2, accGlow)));
        Put("ChButtonPressedShadow", Sh(I(0, 2, 4, 0, neoDark), I(0, 0, 0, 1, edge)));
        Put("ChButtonFocusShadow", Sh(S(0, 0, 0, 1, acc), S(0, 0, 0, 3, Mix(acc, .35, Transparent))));
        Put("ChButtonDisabledShadow", Sh(S(0, 0, 0, 1, edgeFaint)));
        void Gel(string name, Color g, Color lo, Color hi, Color edgeC)
        {
            Put($"Ch{name}Brush", V((0, Mix(g, .88, white)), (.5, g), (.82, Lerp(g, lo, .64)), (1, Over(WithAlpha(hi, .45), lo))));
            Put($"Ch{name}HoverBrush", V((0, Mix(g, .80, white)), (.75, g), (1, Over(WithAlpha(hi, .55), g))));
            Put($"Ch{name}PressedBrush", V((0, lo), (1, g)));
            Put($"Ch{name}Shadow", Sh(I(0, -1, 0, 0, Blk(.18)), I(0, 0, 0, 1, Wht(.10)), S(0, 0, 0, 1, edgeC), S(0, 1, 1, 0, Blk(.18)), S(0, 2, 6, -1, Mix(g, .55, Transparent))));
            Put($"Ch{name}HoverShadow", Sh(S(0, 0, 0, 1, edgeC), S(0, 0, 14, -2, Mix(g, .70, Transparent))));
            Put($"Ch{name}PressedShadow", Sh(I(0, 2, 5, 0, Blk(.35)), S(0, 0, 0, 1, edgeC)));
        }
        Gel("Gel", acc, accLo, accHi, accEdge);
        Gel("DangerGel", Hex("#dc4437"), Hex("#9e1f16"), Hex("#ff9b84"), Hex("#7c1710"));
        Put("ChAccentGelBrush", GelOver(acc, accLo, glow: accHi));
        Put("ChAccentGelVBrush", V((0, accHi), (.6, acc), (1, accLo)));
        Put("ChCheckOnShadow", Sh(I(0, 1, 0, 0, Wht(.4)), S(0, 0, 0, 1, accEdge), S(0, 1, 3, 0, accGlow)));
        Put("ChSwitchOnShadow", Sh(I(0, 1, 3, 0, Blk(.25)), S(0, 0, 0, 1, accEdge), S(0, 0, 8, -2, accGlow)));
        Put("ChFocusRingShadow", Sh(I(0, 0, 0, 1, acc), S(0, 0, 0, 3, Mix(acc, .35, Transparent))));
        Put("ChKnobBrush", GelOver(chromeTop, chromeBot, soft: true));
        Put("ChKnobShadow", Sh(I(0, 1, 0, 0, spec), S(0, 0, 0, 1, edge), S(0, 2, 4, 0, neoDark)));
        Put("ChThumbBrush", GelOver(chromeTop, chromeBot, glow: accHi));
        Put("ChThumbHoverShadow", Sh(S(0, 0, 0, 1, acc), S(0, 0, 10, 0, accGlow)));
        Put("ChFieldFocusShadow", Sh(I(0, 1, 2, 0, D(.20 * u)), I(0, 4, 6, -4, D(.30 * u)), I(0, 0, 0, 1, acc), S(0, 0, 0, 3, Mix(acc, .35, Transparent))));
        var danger = Hex(dk ? "#f0605a" : "#d0392f");
        Put("ChFieldInvalidShadow", Sh(I(0, 4, 6, -4, D(.30 * u)), I(0, 0, 0, 1, danger), S(0, 0, 0, 3, Mix(danger, .22, Transparent))));
        Put("ChToolHoverShadow", Sh(I(0, 1, 0, 0, spec), S(0, 0, 0, 1, softRing), S(0, 1, 3, 0, neoDark)));
        Put("ChToolPressedShadow", Sh(I(0, 1, 3, 0, neoDark), I(0, 0, 0, 1, softRing)));
        Put("ChMenuBarHoverShadow", Sh(I(0, 0, 0, 1, softRing), I(0, 1, 0, 0, spec)));
        Put("ChProgressBarBrush", GelOver(accHi, accLo));
        Put("ChProgressShadow", Sh(I(0, 0, 0, 1, Mix(accEdge, .7, Transparent)), S(0, 0, 8, -1, accGlow)));
        var ok = Hex("#2fa85a");
        Put("ChProgressSuccessBrush", GelOver(Mix(ok, .55, white), Mix(ok, .72, black)));
        Put("ChStripeBrush", new ImmutableLinearGradientBrush(new[] {
                new ImmutableGradientStop(0, Wht(.20)), new ImmutableGradientStop(.5, Wht(.20)), new ImmutableGradientStop(.5, Transparent), new ImmutableGradientStop(1, Transparent) },
            1, null, null, GradientSpreadMethod.Repeat, new RelativePoint(0, 0, RelativeUnit.Absolute), new RelativePoint(9.9, -9.9, RelativeUnit.Absolute)));

        // ---- tabs --------------------------------------------------------------------------
        var editorBg = editor;
        Put("ChTabSelectedShadow", Sh(I(0, 2, 0, 0, acc), I(0, 3, 6, -3, accGlow), S(0, -2, 6, -2, neoDark)));
        Put("ChTabShadow", Sh(I(0, 1, 0, 0, specSoft), I(0, 0, 0, 1, edgeFaint)));
        Put("ChSheetTabShadow", Sh(I(0, 1, 0, 0, spec), I(0, 0, 0, 1, edgeFaint)));
        Put("ChSheetTabSelectedShadow", Sh(I(0, 1, 0, 0, spec), I(0, 2, 0, 0, acc), S(0, -3, 8, -4, neoDark)));
        Put("ChSegmentOnShadow", Sh(S(0, 0, 0, 1, accEdge), S(0, 1, 3, 0, accGlow)));

        // ---- popups & tooltip ----------------------------------------------------------------
        Put("ChPopupBrush", V((0, popup), (1, Mix(popup, .88, raised))));
        Put("ChTooltipBrush", V((0, Hex(dk ? "#3f3a28" : "#fffdf0")), (1, Hex(dk ? "#35311f" : "#fff4c9"))));
        Col("TooltipText", Hex(dk ? "#f4ecc8" : "#3a321a"));
        Put("ChTooltipShadow", Sh(I(0, 1, 0, 0, specSoft), I(0, 1, 0, 0, Wht(lips[4])), S(0, 0, 0, 1, rim), S(0, 4, 8, 0, D(.24 * u)), S(0, 22, 44, -8, Blk(.55))));

        // ---- editor & syntax -------------------------------------------------------------------
        Col("Editor", editorBg);
        Col("Console", console);
        Col("Gutter", Hex(dk ? "#1d2026" : "#eef1f5"));
        Col("GutterText", Hex(dk ? "#626a77" : "#8b93a1"));
        Col("CurrentLine", Hex(dk ? "#2a2920" : "#fff7d1"));
        Col("SynKw", Hex(dk ? "#7aa5ff" : "#1f45c4"));
        Col("SynType", Hex(dk ? "#4fc6dc" : "#0b7590"));
        Col("SynStr", Hex(dk ? "#f08f86" : "#b3261e"));
        Col("SynNum", Hex(dk ? "#d69cf3" : "#a0228f"));
        Col("SynCom", Hex(dk ? "#6fc27a" : "#2f8a3c"));
        Col("SynPp", Hex(dk ? "#e2b65d" : "#8a5a00"));
        Put("ChEditorTopShadow", Sh(I(0, 2, 2, -1, D(.20 * u)), I(0, 8, 12, -8, D(.30 * u))));

        // ---- scrollbars ---------------------------------------------------------------------------
        var thumb = Mix(Hex(dk ? "#e5e8ee" : "#1a1f27"), .30, chromeBot);
        Put("ChScrollThumbBrush", H((0, Over(Wht(.40), thumb)), (.5, Over(Wht(.08), thumb)), (1, Over(Blk(.06), thumb))));
        Put("ChScrollThumbHBrush", V((0, Over(Wht(.45), thumb)), (.55, Over(Wht(.05), thumb)), (1, thumb)));
        Put("ChScrollTrackShadow", Sh(I(1, 0, 2, 0, neoDark)));
        Put("ChScrollTrackHShadow", Sh(I(0, 1, 2, 0, neoDark)));
        Put("ChScrollThumbHoverBrush", H((0, Over(Wht(.40), acc)), (.5, Over(Wht(.08), acc)), (1, Over(Blk(.06), acc))));

        // ---- caption capsule (Vista/7 joined, red gel close) -------------------------------------
        Put("ChCaptionBrush", GelOver(Wht(.20), Wht(.04)));
        Put("ChCaptionHoverBrush", GelOver(Over(Wht(.30), Rgba(80, 150, 230, .10)), Rgba(80, 150, 230, .35), glow: Rgba(160, 220, 255, .9)));
        Put("ChCloseBrush", GelOver(Hex("#e0574a"), Hex("#a8231a"), glow: Hex("#ff9a7a")));
        Put("ChCloseHoverBrush", GelOver(Hex("#f06a5c"), Hex("#c02a20"), glow: Hex("#ffc0a0")));
        Put("ChCaptionShadow", Sh(I(0, 0, 0, 1, Wht(.28))));
        Put("ChCaptionSepShadow", Sh(I(0, 0, 0, 1, Wht(.28)), S(-1, 0, 0, 0, Blk(.25))));
        Put("ChCloseHoverShadow", Sh(I(0, 0, 0, 1, Wht(.28)), S(0, 0, 12, 0, Hex("#ff6a50"))));
        Put("ChCaptionGroupShadow", Sh(S(0, 0, 0, 1, Blk(.30)), S(0, 1, 0, 0, Wht(.35))));
        // real window chrome: the 3px depth band is drawn inside the window bottom; top wash line
        Put("ChWindowChromeShadow", Sh(I(0, -3, 0, 0, glassSide), I(0, 1, 0, 0, Wht(dk ? .10 : .45))));

        // ---- Fluent bridge: make stock Fluent bits that we don't template pick up ChiselUI colours ----
        Put("SystemAccentColor", acc);
        Put("SystemAccentColorLight1", Mix(acc, .8, white));
        Put("SystemAccentColorLight2", Mix(acc, .6, white));
        Put("SystemAccentColorLight3", Mix(acc, .4, white));
        Put("SystemAccentColorDark1", Mix(acc, .8, black));
        Put("SystemAccentColorDark2", Mix(acc, .6, black));
        Put("SystemAccentColorDark3", Mix(acc, .4, black));
        Put("TextControlSelectionHighlightColor", B(Mix(acc, .45, Transparent)));
        // DataGrid (Fluent template) colours
        Put("DataGridRowSelectedBackgroundBrush", B(Mix(acc, .22, Transparent)));
        Put("DataGridRowSelectedHoveredBackgroundBrush", B(Mix(acc, .28, Transparent)));
        Put("DataGridRowSelectedUnfocusedBackgroundBrush", B(Mix(acc, .16, Transparent)));
        Put("DataGridRowSelectedHoveredUnfocusedBackgroundBrush", B(Mix(acc, .22, Transparent)));
        Put("DataGridRowSelectedBackgroundOpacity", 1.0);
        Put("DataGridRowSelectedHoveredBackgroundOpacity", 1.0);
        Put("DataGridRowSelectedUnfocusedBackgroundOpacity", 1.0);
        Put("DataGridRowSelectedHoveredUnfocusedBackgroundOpacity", 1.0);
        Put("DataGridRowHoveredBackgroundColor", B(Mix(acc, .07, Transparent)));
        Put("DataGridColumnHeaderForegroundBrush", B(Hex(dk ? "#b9c0cb" : "#3e4653")));
        Put("DataGridColumnHeaderBackgroundBrush", V((0, chromeTop), (1, chromeBot)));
        Put("DataGridColumnHeaderHoveredBackgroundBrush", V((0, Mix(chromeTop, .7, acc)), (1, chromeBot)));
        Put("DataGridColumnHeaderPressedBackgroundBrush", B(Mix(acc, .14, Transparent)));
        Put("DataGridGridLinesBrush", B(Mix(edge, .5, Transparent)));
        Put("DataGridCellFocusVisualPrimaryBrush", B(Transparent));
        Put("DataGridCellFocusVisualSecondaryBrush", B(Transparent));
        Put("DataGridCurrencyVisualPrimaryBrush", B(Transparent));
        Put("DataGridRowBackgroundBrush", B(Transparent));
    }

    public void ApplyTo(IResourceDictionary dict)
    {
        foreach (var kv in _r) dict[kv.Key] = kv.Value;
    }
}
