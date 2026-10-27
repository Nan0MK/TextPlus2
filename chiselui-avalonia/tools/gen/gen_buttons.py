# Generates src/ChiselUI.Avalonia/Styles/Buttons.axaml (Button, ToggleButton, RepeatButton share one template + class set)
import os
OUT = os.path.join(os.path.dirname(__file__), '../../src/ChiselUI.Avalonia/Styles/Buttons.axaml')

def template():
    return '''    <Setter Property="Template">
      <ControlTemplate>
        <Panel>
          <Border x:Name="PART_Surface" Background="{TemplateBinding Background}" CornerRadius="{TemplateBinding CornerRadius}" BoxShadow="{DynamicResource ChButtonShadow}"/>
          <Grid x:Name="PART_GlossHost" IsHitTestVisible="False" RowDefinitions="46*,54*" Margin="2,1,2,0">
            <Border x:Name="PART_Gloss" CornerRadius="3,3,2,2" Background="{DynamicResource ChGlossCapBrush}"/>
          </Grid>
          <Border x:Name="PART_Focus" IsHitTestVisible="False" IsVisible="False" CornerRadius="{TemplateBinding CornerRadius}" BoxShadow="{DynamicResource ChButtonFocusShadow}"/>
          <ContentPresenter x:Name="PART_ContentPresenter" Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}"
                            Padding="{TemplateBinding Padding}" Foreground="{TemplateBinding Foreground}"
                            HorizontalContentAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalContentAlignment="{TemplateBinding VerticalContentAlignment}"
                            RecognizesAccessKey="True"/>
        </Panel>
      </ControlTemplate>
    </Setter>
'''

def st(sel, *setters, part=None):
    s = sel + (f' /template/ {part}' if part else '')
    body = ''.join(f'<Setter Property="{p}" Value="{v}"/>' for p, v in setters)
    return f'    <Style Selector="{s}">{body}</Style>\n'

SURF = 'Border#PART_Surface'; GLOSS = 'Border#PART_Gloss'; FOCUS = 'Border#PART_Focus'
R = lambda k: '{DynamicResource %s}' % k

def states(checked):
    o = ''
    o += st('^:pointerover', ('Background', R('ChButtonHoverBrush')))
    o += st('^:pointerover', ('BoxShadow', R('ChButtonHoverShadow')), part=SURF)
    o += st('^:pressed', ('Background', R('ChButtonPressedBrush')))
    o += st('^:pressed', ('BoxShadow', R('ChButtonPressedShadow')), part=SURF)
    o += st('^:pressed', ('Opacity', '0.35'), part=GLOSS)
    if checked:
        # checked toggle = sunken accent-tinted (web .is-pressed / aria-pressed)
        o += st('^:checked', ('Background', R('ChAccentSoftPressedBrush')), ('Foreground', R('ChAccentTextBrush')))
        o += st('^:checked', ('BoxShadow', R('ChButtonPressedShadow')), part=SURF)
        o += st('^:checked', ('Opacity', '0.30'), part=GLOSS)
    o += st('^:focus-visible', ('IsVisible', 'True'), part=FOCUS)
    # gel variants
    for cls, g in (('primary', 'Gel'), ('danger', 'DangerGel')):
        o += st(f'^.{cls}', ('Background', R(f'Ch{g}Brush')), ('Foreground', 'White'), ('FontWeight', 'SemiBold'))
        o += st(f'^.{cls}', ('BoxShadow', R(f'Ch{g}Shadow')), part=SURF)
        o += st(f'^.{cls}', ('Background', R('ChGelGlossBrush')), ('Opacity', '0.55'), part=GLOSS)
        o += st(f'^.{cls}:pointerover', ('Background', R(f'Ch{g}HoverBrush')))
        o += st(f'^.{cls}:pointerover', ('BoxShadow', R(f'Ch{g}HoverShadow')), part=SURF)
        o += st(f'^.{cls}:pressed', ('Background', R(f'Ch{g}PressedBrush')))
        o += st(f'^.{cls}:pressed', ('BoxShadow', R(f'Ch{g}PressedShadow')), part=SURF)
        o += st(f'^.{cls}:pressed', ('Opacity', '0.2'), part=GLOSS)
        if checked:
            o += st(f'^.{cls}:checked', ('Background', R(f'Ch{g}PressedBrush')))
            o += st(f'^.{cls}:checked', ('BoxShadow', R(f'Ch{g}PressedShadow')), part=SURF)
    # sizes
    o += st('^.sm', ('MinHeight', '22'), ('Padding', '10,0'), ('MinWidth', '0'), ('FontSize', '12'))
    o += st('^.icon', ('MinWidth', '26'), ('Width', '26'), ('Padding', '0'))
    # toolbar button (web .tb-btn): flat until hover
    o += st('^.tool', ('Background', 'Transparent'), ('MinWidth', '28'), ('Height', '26'), ('Padding', '5,0'))
    o += st('^.tool', ('BoxShadow', 'none'), part=SURF)
    o += st('^.tool', ('IsVisible', 'False'), part='Grid#PART_GlossHost')
    o += st('^.tool:pointerover', ('Background', R('ChChromeHoverGelSoftBrush')))
    o += st('^.tool:pointerover', ('BoxShadow', R('ChToolHoverShadow')), part=SURF)
    o += st('^.tool:pressed', ('Background', R('ChAccentSoftPressedBrush')))
    o += st('^.tool:pressed', ('BoxShadow', R('ChToolPressedShadow')), part=SURF)
    if checked:
        o += st('^.tool:checked', ('Background', R('ChAccentSoftPressedBrush')), ('Foreground', R('ChAccentTextBrush')))
        o += st('^.tool:checked', ('BoxShadow', R('ChToolPressedShadow')), part=SURF)
    # flat (dock close "x", link-ish)
    o += st('^.flat', ('Background', 'Transparent'), ('MinWidth', '0'), ('MinHeight', '0'), ('Padding', '3'), ('Foreground', R('ChTextMutedBrush')))
    o += st('^.flat', ('BoxShadow', 'none'), part=SURF)
    o += st('^.flat', ('IsVisible', 'False'), part='Grid#PART_GlossHost')
    o += st('^.flat:pointerover', ('Background', R('ChHoverWashBrush')), ('Foreground', R('ChTextBrush')))
    o += st('^.flat:pointerover', ('BoxShadow', 'none'), part=SURF)
    o += st('^.flat:pressed', ('BoxShadow', 'none'), part=SURF)
    # orb: round accent gel (web .btn-orb)
    o += st('^.orb', ('Width', '30'), ('Height', '30'), ('MinWidth', '0'), ('Padding', '0'), ('CornerRadius', '999'),
            ('Background', R('ChAccentGelBrush')), ('Foreground', 'White'))
    o += st('^.orb', ('BoxShadow', R('ChGelShadow')), part=SURF)
    o += st('^.orb', ('Margin', '3,1,3,0'), ('CornerRadius', '999,999,40,40'), ('Background', R('ChGelGlossBrush')), ('Opacity', '0.5'), part=GLOSS)
    o += st('^.orb:pointerover', ('Background', R('ChGelHoverBrush')))
    o += st('^.orb:pointerover', ('BoxShadow', R('ChGelHoverShadow')), part=SURF)
    o += st('^.orb:pressed', ('Background', R('ChGelPressedBrush')))
    o += st('^.orb:pressed', ('BoxShadow', R('ChGelPressedShadow')), part=SURF)
    # segmented control buttons (inside a Border.segmented well)
    o += st('^.segment', ('Background', 'Transparent'), ('MinWidth', '0'), ('MinHeight', '22'), ('Padding', '12,0'), ('FontSize', '12'), ('Foreground', R('ChTextSecondaryBrush')))
    o += st('^.segment', ('BoxShadow', 'none'), part=SURF)
    o += st('^.segment', ('IsVisible', 'False'), part='Grid#PART_GlossHost')
    o += st('^.segment:pointerover', ('Background', R('ChHoverWashBrush')), ('Foreground', R('ChTextBrush')))
    o += st('^.segment:pointerover', ('BoxShadow', 'none'), part=SURF)
    o += st('^.segment:pressed', ('BoxShadow', 'none'), part=SURF)
    if checked:
        o += st('^.segment:checked', ('Background', R('ChAccentGelBrush')), ('Foreground', 'White'), ('FontWeight', 'SemiBold'))
        o += st('^.segment:checked', ('BoxShadow', R('ChSegmentOnShadow')), part=SURF)
    # disabled last
    o += st('^:disabled', ('Background', R('ChButtonDisabledBrush')), ('Foreground', R('ChTextDisabledBrush')))
    o += st('^:disabled', ('BoxShadow', R('ChButtonDisabledShadow')), part=SURF)
    o += st('^:disabled', ('Opacity', '0.5'), part=GLOSS)
    o += st('^:disabled', ('Foreground', R('ChTextDisabledBrush')), part='ContentPresenter#PART_ContentPresenter')
    return o

def theme(target, key=None, checked=False, based=None):
    key = key or '{x:Type %s}' % target
    head = f'  <ControlTheme x:Key="{key}" TargetType="{target}">\n'
    setters = '''    <Setter Property="Background" Value="{DynamicResource ChButtonBrush}"/>
    <Setter Property="Foreground" Value="{DynamicResource ChTextBrush}"/>
    <Setter Property="FontWeight" Value="Medium"/>
    <Setter Property="Padding" Value="14,0"/>
    <Setter Property="MinHeight" Value="26"/>
    <Setter Property="MinWidth" Value="76"/>
    <Setter Property="CornerRadius" Value="4"/>
    <Setter Property="HorizontalContentAlignment" Value="Center"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="ClipToBounds" Value="False"/>
'''
    return head + setters + template() + states(checked) + '  </ControlTheme>\n\n'

CAPTION = r'''
  <!-- Caption capsule buttons (GlassFrame / ChiselWindow). Separate theme: no chrome, white gel. -->
  <ControlTheme x:Key="ChCaptionButton" TargetType="Button">
    <Setter Property="Width" Value="28"/>
    <Setter Property="Height" Value="20"/>
    <Setter Property="Padding" Value="0"/>
    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Background" Value="{DynamicResource ChCaptionBrush}"/>
    <Setter Property="Focusable" Value="False"/>
    <Setter Property="Template">
      <ControlTemplate>
        <Border x:Name="PART_Surface" Background="{TemplateBinding Background}" BoxShadow="{DynamicResource ChCaptionSepShadow}">
          <ContentPresenter Content="{TemplateBinding Content}" HorizontalContentAlignment="Center" VerticalContentAlignment="Center"/>
        </Border>
      </ControlTemplate>
    </Setter>
    <Style Selector="^.first /template/ Border#PART_Surface"><Setter Property="BoxShadow" Value="{DynamicResource ChCaptionShadow}"/></Style>
    <Style Selector="^:pointerover"><Setter Property="Background" Value="{DynamicResource ChCaptionHoverBrush}"/></Style>
    <Style Selector="^:pressed"><Setter Property="Opacity" Value="0.85"/></Style>
    <Style Selector="^.close"><Setter Property="Width" Value="44"/><Setter Property="Background" Value="{DynamicResource ChCloseBrush}"/></Style>
    <Style Selector="^.close:pointerover"><Setter Property="Background" Value="{DynamicResource ChCloseHoverBrush}"/></Style>
    <Style Selector="^.close:pointerover /template/ Border#PART_Surface"><Setter Property="BoxShadow" Value="{DynamicResource ChCloseHoverShadow}"/></Style>
  </ControlTheme>
'''

SWATCH = r'''
  <!-- Accent swatch (AccentPicker): 18px gel circle, selected = surface ring + dark ring -->
  <ControlTheme x:Key="ChSwatchButton" TargetType="Button">
    <Setter Property="Width" Value="18"/>
    <Setter Property="Height" Value="18"/>
    <Setter Property="ClipToBounds" Value="False"/>
    <Setter Property="Template">
      <ControlTemplate>
        <Panel>
          <Ellipse x:Name="PART_Ring" Margin="-4" Fill="{DynamicResource ChSurfaceFloatBrush}" Stroke="{DynamicResource SwatchRing}" StrokeThickness="1.2" IsVisible="False"/>
          <Border x:Name="PART_Dot" CornerRadius="999" Background="{DynamicResource SwatchBrush}" BoxShadow="inset 0 0 0 1 #40000000, 0 1 2 0 #59000000"/>
        </Panel>
      </ControlTemplate>
    </Setter>
    <Style Selector="^:pointerover /template/ Border#PART_Dot"><Setter Property="BoxShadow" Value="inset 0 0 0 1 #40000000, 0 0 9 0 #66FFFFFF"/></Style>
    <Style Selector="^.selected /template/ Ellipse#PART_Ring"><Setter Property="IsVisible" Value="True"/></Style>
  </ControlTheme>
'''

with open(OUT, 'w') as f:
    f.write('<!-- GENERATED by tools/gen/gen_buttons.py -->\n<Styles xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">\n<Styles.Resources>\n\n')
    f.write(theme('Button'))
    f.write(theme('ToggleButton', checked=True))
    f.write(theme('RepeatButton'))
    f.write(CAPTION)
    f.write(SWATCH)
    f.write('</Styles.Resources>\n')
    f.write('  <Style Selector="Button.swatch"><Setter Property="Theme" Value="{StaticResource ChSwatchButton}"/></Style>\n')
    f.write('</Styles>\n')
print('ok', OUT)
