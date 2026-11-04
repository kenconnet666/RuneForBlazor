using Rune.Styles;

namespace Rune.UI;

/// <summary>输入框的样式（同 vue-tsx-pro 的 ControlVariants）。</summary>
public enum InputVariant
{
    /// <summary>表面色加描边（默认）。</summary>
    Surface,

    /// <summary>浅色底，没有描边：表单很密、或放在有底色的区域里。</summary>
    Soft,
}

/// <summary>输入框外壳的部件（TextField、Textarea、NumberField 共用，同 vue-tsx-pro 的 Input 配方）。</summary>
public enum InputPart
{
    /// <summary>外壳（div）：边框、底色、焦点环。</summary>
    Root,

    /// <summary>原生 input / textarea。</summary>
    Input,

    /// <summary>前后的图标。</summary>
    Icon,

    /// <summary>前后缀文字（https://、元），是指向输入框的 label。</summary>
    Affix,

    /// <summary>框里的小按钮（清空、显示密码、加减）。</summary>
    Button,
}

/// <summary>输入框的默认值（TextField、Textarea、NumberField、NativeSelect 共用，同 vue-tsx-pro 的 defaults.Input）。</summary>
public sealed record InputDefaults : IComponentDefaults<InputDefaults>
{
    /// <summary>样式。</summary>
    public InputVariant? Variant { get; init; }

    /// <summary>圆角。</summary>
    public Radius? Radius { get; init; }

    /// <summary>文本框每次输入都更新值（默认失去焦点或回车时更新）。</summary>
    public bool? Immediate { get; init; }

    /// <inheritdoc/>
    public InputDefaults Over(InputDefaults outer) => new()
    {
        Variant = Variant ?? outer.Variant,
        Radius = Radius ?? outer.Radius,
        Immediate = Immediate ?? outer.Immediate,
    };
}

/// <summary>输入框外壳的外观：交给 <see cref="InputStyles.Of"/> 的档位组合。</summary>
/// <param name="Variant">样式。</param>
/// <param name="Size">字号档位；null 时继承上下文字号。</param>
/// <param name="Radius">圆角。</param>
/// <param name="ReadOnly">只读：底色变浅，不显示悬停。</param>
/// <param name="Multiline">多行（Textarea）：内容从顶部开始，上下也留内边距；Rows、MaxRows、Fixed 是多行时的行数与能否拖动。</param>
/// <param name="Rows">多行时最少几行高。</param>
/// <param name="MaxRows">多行时最多几行高（超过时滚动）。</param>
/// <param name="Fixed">多行时固定高度，可以拖动改变。</param>
/// <param name="Picker">下拉选择（Select）：整个外壳都能点开（指针），在 ControlGroup 里也按内容宽。</param>
public readonly record struct InputAppearance(
    InputVariant Variant,
    Size? Size,
    Radius Radius,
    bool ReadOnly = false,
    bool Multiline = false,
    int Rows = 0,
    int? MaxRows = null,
    bool Fixed = false,
    bool Picker = false)
{
    /// <summary>外观：组件上写的 &gt; 配置的默认值（<see cref="InputDefaults"/>）&gt; 表面色、中圆角。</summary>
    public static InputAppearance Resolve(InputVariant? variant, Size? size, Radius? radius, bool readOnly, UIConfig config)
    {
        var defaults = config.Defaults.Get<InputDefaults>();
        return new InputAppearance(variant ?? defaults?.Variant ?? InputVariant.Surface, size, radius ?? defaults?.Radius ?? UI.Radius.Md, readOnly);
    }
}

/// <summary>
/// 输入框外壳的样式（同 vue-tsx-pro 的 inputRecipe）。校验未通过、不可用按外壳里输入元素的状态（aria-invalid、:disabled）用 :has 变化，
/// 不必把状态再写到外壳上；只看直接子元素：数字框到了上限时不可用的加减按钮、下拉里不可用的占位选项都不算。
/// </summary>
public static class InputStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<InputPart> Of(InputAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("input", appearance, styles, static (InputAppearance a, InputPart part) => part switch
        {
            // 焦点环排在前面：按部件定制能盖过它；档位的先后就是覆盖的先后：只读、出错依次盖过样式的描边与底色
            InputPart.Root => Sx.Of(
                FocusRingWithin,
                Root,
                a.Variant == InputVariant.Soft ? Soft : null,
                a.ReadOnly ? ReadOnlyRoot : null,
                StateRoot,
                a.Picker ? PickerRoot : null,
                a.Multiline ? MultilineRoot : null,
                ScaleStyles.Radius(a.Radius),
                a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            InputPart.Input => a.Multiline
                ? Sx.Of(
                    InputElement,
                    TextareaStyles.Textarea,
                    a.Fixed ? TextareaStyles.Resizable : null,
                    TextareaStyles.MinRows(a.Rows),
                    !a.Fixed && a.MaxRows is { } max ? TextareaStyles.MaxRows(max) : null)
                : InputElement,
            InputPart.Icon => Icon,
            InputPart.Affix => Affix,
            _ => Button,
        });

    /// <summary>键盘或指针聚焦到里面时的焦点环（外壳上，画在边框上面）。</summary>
    internal static Style FocusRingWithin => Css.Define(static () => new Style
    {
        OutlineStyle = OutlineStyle.None,
        OutlineWidth = BorderWidths.Sm,
        OutlineColor = Colors.FocusRing,
        OutlineOffset = $"calc(-1 * {BorderWidths.Xs})",
        [When.FocusWithin] = new() { OutlineStyle = OutlineStyle.Solid },
    });

    /// <summary>外壳本身（下拉框 NativeSelect 也用）。</summary>
    internal static Style Root => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MinWidth = 0,
        MinHeight = $"max({Sizes.Control}, {Sizes.MinTarget})",
        PaddingInline = Space.Md,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BackgroundColor = Colors.Surface,
        Color = Colors.Text,
        LineHeight = LineHeights.Normal,
        Cursor = Cursor.Text,
        Transition = $"border-color {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { BorderColor = Colors.BorderStrong },
    });

    /// <summary>浅色底、没有描边（Soft 样式）。下拉框也用。</summary>
    internal static Style Soft => Css.Define(static () => new Style
    {
        BorderColor = CssColors.Transparent,
        BackgroundColor = Colors.Fill,
        [When.Hover] = new() { BorderColor = CssColors.Transparent },
    });

    /// <summary>只读：底色变浅，不显示悬停。下拉框也用。</summary>
    internal static Style ReadOnlyRoot => Css.Define(static () => new Style
    {
        BorderColor = Colors.BorderMuted,
        BackgroundColor = Colors.Fill,
        [When.Hover] = new() { BorderColor = Colors.BorderMuted },
    });

    /// <summary>校验未通过、不可用（按里面输入元素的状态）。下拉框也用。</summary>
    internal static Style StateRoot => Css.Define(static () => new Style
    {
        [invalid] = new()
        {
            BorderColor = Tones.Danger.Solid,
            OutlineColor = Tones.Danger.Solid,
            [When.Hover] = new() { BorderColor = Tones.Danger.Solid },
        },
        [disabled] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    static Style PickerRoot => Css.Define(static () => new Style { Cursor = Cursor.Pointer, FlexGrow = 0 });

    static Style MultilineRoot => Css.Define(static () => new Style { AlignItems = AlignItems.Stretch, PaddingBlock = Space.Xs });

    /// <summary>
    /// 原生 input / textarea：透明、无边框，占满剩余宽度，字体继承外壳。参与基线对齐、高度撑满外壳：
    /// 外壳的基线取输入框文字的基线（而不是前面图标的底边），起始边的标签（按基线对齐）才与输入的文字在一条线上。
    /// </summary>
    internal static Style InputElement => Css.Define(static () => new Style
    {
        AlignSelf = AlignSelf.Baseline,
        BoxSizing = BoxSizing.BorderBox,
        FlexGrow = 1,
        MinWidth = 0,
        MinHeight = $"calc(max({Sizes.Control}, {Sizes.MinTarget}) - 2 * {BorderWidths.Xs})",
        Margin = 0,
        PaddingBlock = Space.Xs,
        PaddingInline = 0,
        BorderWidth = 0,
        OutlineStyle = OutlineStyle.None,
        BackgroundColor = CssColors.Transparent,
        Color = CssWide.Inherit,
        Font = CssWide.Inherit,
        Cursor = CssWide.Inherit,
        [Pseudo.Placeholder] = new() { Color = Colors.TextPlaceholder, Opacity = 1 },
    });

    /// <summary>前后的图标：次要颜色。下拉框的箭头也用。</summary>
    internal static Style Icon => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted });

    static Style Affix => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted, WhiteSpace = WhiteSpace.Nowrap });

    /// <summary>框里的小按钮（清空、显示密码、加减）：比独立的图标按钮紧凑，仍不小于 24px 的点击区域。下拉框也用。</summary>
    internal static Style Button => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Width = $"max(1.5em, {Sizes.MinTarget})",
        MinHeight = $"max(1.5em, {Sizes.MinTarget})",
        MarginInline = $"calc(-1 * {Space.Xs})",
        Color = Colors.TextMuted,
    });

    static readonly Condition invalid = When.Has("> :is(input, select, textarea, [role=combobox])[aria-invalid=\"true\"]");
    static readonly Condition disabled = When.Has("> :is(input, select, textarea, [role=combobox]):disabled");
}
