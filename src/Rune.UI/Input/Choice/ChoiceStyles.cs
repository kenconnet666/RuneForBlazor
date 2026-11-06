using Rune.Styles;

namespace Rune.UI;

/// <summary>勾选类控件（Checkbox、Radio、Switch，同 vue-tsx-pro 的 Choice 配方）的部件。</summary>
public enum ChoicePart
{
    /// <summary>外层（label）：控件与文字一行，整块可点。</summary>
    Root,

    /// <summary>控件格子：input 与标记叠在同一格里，与第一行文字垂直居中。</summary>
    Control,

    /// <summary>原生 input（去掉浏览器外观，画成方框、圆或轨道）。</summary>
    Input,

    /// <summary>标记：复选框的对勾、单选框的圆点、开关的滑块。</summary>
    Indicator,

    /// <summary>文字（有说明时文字在上、说明在下）。</summary>
    Label,

    /// <summary>文字下面的说明。</summary>
    Description,

    /// <summary>复选组、单选组的外层。</summary>
    Group,
}

/// <summary>勾选类控件的种类：决定 input 画成什么形状、标记怎么动。</summary>
public enum ChoiceKind
{
    /// <summary>复选框：方框加对勾。</summary>
    Checkbox,

    /// <summary>单选框：圆加圆点。</summary>
    Radio,

    /// <summary>开关：轨道加滑块。</summary>
    Switch,
}

/// <summary>勾选类控件的外观：交给 <see cref="ChoiceStyles.Of"/> 的档位组合。</summary>
/// <param name="Kind">种类。</param>
/// <param name="Size">字号档位（方框、圆、开关随之缩放）；null 时继承上下文字号。</param>
/// <param name="Tone">选中时的色调。</param>
/// <param name="ReadOnly">只读：外观照常（看得清选了什么），只是不像能点。</param>
public readonly record struct ChoiceAppearance(ChoiceKind Kind, Size? Size, Tone Tone, bool ReadOnly = false) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Kind, Size, Tone];
}

/// <summary>
/// 勾选类控件的样式（同 vue-tsx-pro 的 choiceRecipe）。选中、半选、不可用、校验未通过都按原生 input 的状态用 CSS 变化（:checked、:indeterminate、
/// :disabled、aria-invalid），点了立即变，不等往返。选中的颜色来自外层的两个变量：<c>--rune-tone-fill</c>（实底）与 <c>--rune-tone-on-fill</c>（其上的对勾、圆点、滑块）。
/// </summary>
public static class ChoiceStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<ChoicePart> Of(ChoiceAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("choice", appearance, styles, static (ChoiceAppearance a, ChoicePart part) => part switch
        {
            ChoicePart.Root => Sx.Of(Root, SharedStyles.ToneFill(a.Tone), a.ReadOnly ? ReadOnlyRoot : null, a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            ChoicePart.Control => Control,
            ChoicePart.Input => Sx.Of(
                Input,
                FocusRingTight,
                a.Kind switch
                {
                    ChoiceKind.Radio => Circle,
                    ChoiceKind.Switch => Track,
                    _ => Box,
                },
                Checked,
                a.Kind == ChoiceKind.Checkbox ? Indeterminate : null,
                Invalid),
            ChoicePart.Indicator => a.Kind switch
            {
                ChoiceKind.Radio => Dot,
                ChoiceKind.Switch => Thumb,
                _ => Check,
            },
            ChoicePart.Label => Label,
            ChoicePart.Description => Description,
            _ => Sx.Of(),
        });

    /// <summary>复选组、单选组外层的默认层：竖排或横排（放不下时换行）。</summary>
    public static PartLayers<ChoicePart> Group(Orientation orientation, ComponentStyles styles) =>
        Recipe.Of("choice", orientation, styles, static (Orientation orientation, ChoicePart part) =>
            part == ChoicePart.Group ? (orientation == Orientation.Horizontal ? HorizontalGroup : VerticalGroup) : Sx.Of());

    // 控件与文字一行，整块可点；点击区域不小于 24px（WCAG 2.5.8）
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Sm,
        MinHeight = Sizes.MinTarget,
        LineHeight = LineHeights.Normal,
        Color = Colors.Text,
        Cursor = Cursor.Pointer,
        UserSelect = UserSelect.None,
        [When.Has("input:disabled")] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    static Style ReadOnlyRoot => Css.Define(static () => new Style { Cursor = Cursor.Default });

    // input 与标记放在同一个网格格子里；与第一行文字垂直居中（行高 1.5em）
    static Style Control => Css.Define(static () => new Style
    {
        Display = Display.InlineGrid,
        PlaceItems = PlaceItems.Center,
        FlexShrink = 0,
        MinHeight = CssLength.Em(1.5),
    });

    // 去掉浏览器外观，由各种类给出形状
    static Style Input => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        Appearance = Appearance.None,
        BoxSizing = BoxSizing.BorderBox,
        Margin = 0,
        BorderWidth = BorderWidths.Sm,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderStrong,
        BackgroundColor = Colors.Surface,
        Cursor = CssWide.Inherit,
        Transition = $"background-color {Durations.Sm} {Easings.Standard}, border-color {Durations.Sm} {Easings.Standard}",
    });

    // 焦点环贴着边框外侧（同 vue-tsx-pro 的 focusRingStyles.tight）
    static Style FocusRingTight => Css.Define(static () => new Style
    {
        OutlineStyle = OutlineStyle.None,
        OutlineWidth = BorderWidths.Sm,
        OutlineColor = Colors.FocusRing,
        OutlineOffset = BorderWidths.Sm,
        [When.FocusVisible] = new() { OutlineStyle = OutlineStyle.Solid },
    });

    static Style Checked => Css.Define(static () => new Style
    {
        [When.Checked] = new() { BorderColor = SharedStyles.FillColor, BackgroundColor = SharedStyles.FillColor },
    });

    // 只给复选框：一组单选都没选时浏览器把每个 radio 都算作 :indeterminate
    static Style Indeterminate => Css.Define(static () => new Style
    {
        [When.Indeterminate] = new() { BorderColor = SharedStyles.FillColor, BackgroundColor = SharedStyles.FillColor },
    });

    /// <summary>单选组的标记：组校验未通过（aria-invalid）时里面的单选变红；不认外面别的控件的 aria-invalid。</summary>
    public static readonly Marker GroupMarker = Css.Marker("rune-radio-group");

    // 自己校验未通过，或在校验未通过的组（单选组的 aria-invalid）里
    static Style Invalid => Css.Define(static () => new Style
    {
        [When.Invalid] = new() { BorderColor = Tones.Danger.Solid, OutlineColor = Tones.Danger.Solid },
        [When.Inside(When.Attribute("""[aria-invalid="true"]"""), GroupMarker)] = new() { BorderColor = Tones.Danger.Solid, OutlineColor = Tones.Danger.Solid },
    });

    static Style Box => Css.Define(static () => new Style { Width = CssLength.Em(1.125), Height = CssLength.Em(1.125), BorderRadius = Radii.Sm });

    static Style Circle => Css.Define(static () => new Style { Width = CssLength.Em(1.125), Height = CssLength.Em(1.125), BorderRadius = Radii.Full });

    // 轨道：关着时是深一点的描边色
    static Style Track => Css.Define(static () => new Style
    {
        Width = CssLength.Em(2.25),
        Height = CssLength.Em(1.25),
        BorderRadius = Radii.Full,
        BorderColor = CssColors.Transparent,
        BackgroundColor = Colors.BorderStrong,
    });

    // 对勾（或半选的横线）跟在 input 后面：勾上时显示（用户点击勾上时 JS 初始化器把它一笔画出）
    static Style Check => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        PointerEvents = PointerEvents.None,
        Width = CssLength.Em(0.875),
        Height = CssLength.Em(0.875),
        Color = SharedStyles.OnFillColor,
        Visibility = Visibility.Hidden,
        [When.After(When.Checked)] = new() { Visibility = Visibility.Visible },
        [When.After(When.Indeterminate)] = new() { Visibility = Visibility.Visible },
    });

    static Style Dot => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        PointerEvents = PointerEvents.None,
        Width = CssLength.Em(0.45),
        Height = CssLength.Em(0.45),
        BorderRadius = Radii.Full,
        BackgroundColor = SharedStyles.OnFillColor,
        Transform = "scale(0)",
        Transition = $"transform {Durations.Sm} {Easings.Standard}",
        [When.After(When.Checked)] = new() { Transform = "none" },
    });

    // 滑块靠起始边，打开后移到结束边：用逻辑方向的外边距，从右到左排版自动反过来；百分比相对整条轨道
    static Style Thumb => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        PointerEvents = PointerEvents.None,
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        JustifySelf = JustifySelf.Start,
        Width = CssLength.Em(0.875),
        Height = CssLength.Em(0.875),
        MarginInlineStart = CssLength.Em(0.1875),
        BorderRadius = Radii.Full,
        BackgroundColor = Colors.Background,
        BoxShadow = Shadows.Sm,
        Transition = $"margin-inline-start {Durations.Sm} {Easings.Standard}",
        // 轨道里有文字时滑块与 input 之间隔着两段文字：按前面任一兄弟的勾选状态
        [When.AfterAny(When.Checked)] = new() { MarginInlineStart = "calc(100% - 1.0625em)", BackgroundColor = SharedStyles.OnFillColor },
    });

    /// <summary>开关轨道里有文字：宽度随文字（两段文字叠在同一格里，取较宽的，切换时宽度不跳）。</summary>
    internal static Style SwitchTrackWide => Css.Define(static () => new Style { Width = CssLength.Percent(100), MinWidth = CssLength.Em(2.25) });

    // 轨道里的文字：两段叠在 input 那一格里，用 flex 居中（按行内排会随行高落在基线上，比轨道中线偏下）
    static Style SwitchContent => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        JustifySelf = JustifySelf.Stretch,
        AlignSelf = AlignSelf.Center,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        WhiteSpace = WhiteSpace.Nowrap,
        PointerEvents = PointerEvents.None,
    });

    /// <summary>打开时的文字：在滑块的另一侧（起始边），按 input 的勾选状态显示。</summary>
    internal static Sx SwitchContentOn => Sx.Of(SwitchContent, SwitchContentOnStyle);

    /// <summary>关着时的文字：在滑块的另一侧（结束边），勾上后隐藏。</summary>
    internal static Sx SwitchContentOff => Sx.Of(SwitchContent, SwitchContentOffStyle);

    static Style SwitchContentOnStyle => Css.Define(static () => new Style
    {
        PaddingInlineStart = CssLength.Em(0.5),
        PaddingInlineEnd = CssLength.Em(1.375),
        Color = SharedStyles.OnFillColor,
        Visibility = Visibility.Hidden,
        [When.AfterAny(When.Checked)] = new() { Visibility = Visibility.Visible },
    });

    static Style SwitchContentOffStyle => Css.Define(static () => new Style
    {
        PaddingInlineStart = CssLength.Em(1.375),
        PaddingInlineEnd = CssLength.Em(0.5),
        Color = Colors.Text,
        [When.AfterAny(When.Checked)] = new() { Visibility = Visibility.Hidden },
    });

    /// <summary>轨道里文字的内层：字号缩在这层，外层的内边距按开关的字号算，给滑块留的位置才够。</summary>
    internal static Style SwitchContentText => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        FontSize = FontSizes.Xs,
        FontWeight = FontWeights.Semibold,
        LineHeight = 1,
    });

    /// <summary>切换中滑块里的转圈。</summary>
    internal static Style SwitchSpinner => Css.Define(static () => new Style { Width = CssLength.Em(0.75), Height = CssLength.Em(0.75), Color = SharedStyles.FillColor });

    // 有说明时文字在上、说明在下
    static Style Label => Css.Define(static () => new Style { MinWidth = 0, Display = Display.Grid });

    static Style Description => Css.Define(static () => new Style { Color = Colors.TextMuted, FontSize = CssLength.Em(0.875) });

    static Style VerticalGroup => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Xs });

    static Style HorizontalGroup => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexWrap = FlexWrap.Wrap,
        ColumnGap = Space.Lg,
        RowGap = Space.Xs,
    });
}

/// <summary>复选框组交给组里各个复选框的状态（经级联值）：每次渲染一个新实例，勾选变了时各项跟着重新渲染。</summary>
/// <param name="Checked">已勾选的值。</param>
/// <param name="Name">表单提交时各项共用的 name。</param>
/// <param name="State">整组的不可用、只读、校验未通过。</param>
/// <param name="Toggle">勾选或取消一个值。</param>
public sealed record CheckboxGroupState(IReadOnlyList<string> Checked, string? Name, InheritedControlState State, Func<string, Task> Toggle);
