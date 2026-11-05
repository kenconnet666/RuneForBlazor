using Rune.Styles;

namespace Rune.UI;

/// <summary>滑块的部件。</summary>
public enum SliderPart
{
    /// <summary>外层（轨道与滑块的容器，接住指针）。</summary>
    Root,

    /// <summary>轨道。</summary>
    Track,

    /// <summary>已选的一段（轨道上着色的部分）。</summary>
    Range,

    /// <summary>滑块（role="slider"，范围时有两个）。</summary>
    Thumb,

    /// <summary>轨道里的刻度点。</summary>
    Mark,

    /// <summary>刻度下方（竖排时右侧）的文字。</summary>
    MarkLabel,
}

/// <summary>滑块的外观：方向、刻度有没有文字（要留出位置）、色调、字号档位。</summary>
/// <param name="Orientation">方向。</param>
/// <param name="Labelled">有带文字的刻度。</param>
/// <param name="Tone">已选范围与滑块描边的色调。</param>
/// <param name="Size">字号档位；null 时继承。</param>
public readonly record struct SliderAppearance(Orientation Orientation, bool Labelled, Tone Tone, Size? Size);

/// <summary>
/// 滑块的样式（配方，同 vue-tsx-pro 的 sliderStyles）。位置一直在变：外层的 <see cref="Start"/>、<see cref="End"/>
/// （已选一段的两端，百分比）与刻度上的 <see cref="MarkAt"/> 是 CSS 变量，由组件写在元素上、拖动时 JS 当场改写。
/// </summary>
public static class SliderStyles
{
    /// <summary>已选一段的起点（单值时是 0%）。</summary>
    public static readonly CssVar<CssLength> Start = CssVar.Length("rune-slider-start");

    /// <summary>已选一段的终点（单值时就是滑块的位置）。</summary>
    public static readonly CssVar<CssLength> End = CssVar.Length("rune-slider-end");

    /// <summary>刻度点、刻度文字的位置。</summary>
    public static readonly CssVar<CssLength> MarkAt = CssVar.Length("rune-slider-mark");

    // 滑块的直径；点击区域至少 24px（WCAG 2.5.8）
    static readonly string ThumbSize = $"calc({Space.Lg} + {Space.Xs})";

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<SliderPart> Of(SliderAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("slider", appearance, styles, static (SliderAppearance a, SliderPart part) =>
        {
            var vertical = a.Orientation == Orientation.Vertical;
            return part switch
            {
                SliderPart.Root => Sx.Of(
                    Root,
                    vertical ? Vertical : Horizontal,
                    a.Labelled ? (vertical ? LabelledVertical : LabelledHorizontal) : null,
                    SharedStyles.ToneFill(a.Tone),
                    a.Size is { } size ? ScaleStyles.FontSize(size) : null),
                SliderPart.Track => Sx.Of(Track, vertical ? TrackVertical : TrackHorizontal),
                SliderPart.Range => Sx.Of(Range, vertical ? RangeVertical : RangeHorizontal),
                SliderPart.Thumb => Sx.Of(Thumb, SharedStyles.FocusRing, vertical ? ThumbVertical : ThumbHorizontal),
                SliderPart.Mark => Sx.Of(Mark, vertical ? MarkVertical : MarkHorizontal),
                _ => Sx.Of(MarkLabel, vertical ? MarkLabelVertical : MarkLabelHorizontal),
            };
        });

    /// <summary>滑块对准已选一段的哪一端：单值与范围的高端在终点，范围的低端在起点。</summary>
    internal static Style ThumbAt(bool start, Orientation orientation) => Css.Define((start, orientation), static k =>
    {
        var position = k.start ? Start : End;
        return k.orientation == Orientation.Vertical ? new Style { Bottom = position } : new Style { InsetInlineStart = position };
    });

    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        // 触摸拖动不触发页面滚动
        TouchAction = TouchAction.None,
        UserSelect = UserSelect.None,
        Cursor = Cursor.Pointer,
        [When.Attribute("[data-readonly]")] = new() { Cursor = Cursor.Default },
        [When.Attribute("[data-disabled]")] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    // 滑块的中心对准取值，两端各留半个滑块的外边距：滑块在最小、最大值时不伸出组件的盒子，不压到旁边的控件
    static Style Horizontal => Css.Define(static () => new Style
    {
        Width = $"calc(100% - {ThumbSize})",
        MarginInline = $"calc({ThumbSize} / 2)",
        MinHeight = Sizes.MinTarget,
    });

    static Style Vertical => Css.Define(static () => new Style
    {
        FlexDirection = FlexDirection.Column,
        Height = CssLength.Em(10),
        MarginBlock = $"calc({ThumbSize} / 2)",
        MinWidth = Sizes.MinTarget,
    });

    // 刻度文字放在轨道下方（竖排时右侧），给它留出位置
    static Style LabelledHorizontal => Css.Define(static () => new Style { PaddingBlockEnd = CssLength.Em(1.5) });

    static Style LabelledVertical => Css.Define(static () => new Style { PaddingInlineEnd = CssLength.Em(3) });

    static Style Track => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        FlexGrow = 1,
        BorderRadius = Radii.Full,
        BackgroundColor = Colors.Fill,
        Overflow = Overflow.Hidden,
    });

    static Style TrackHorizontal => Css.Define(static () => new Style { Height = $"calc({Space.Xs} * 1.5)" });

    static Style TrackVertical => Css.Define(static () => new Style { Width = $"calc({Space.Xs} * 1.5)" });

    static Style Range => Css.Define(static () => new Style { Position = Position.Absolute, BackgroundColor = SharedStyles.FillColor });

    static Style RangeHorizontal => Css.Define(static () => new Style
    {
        InsetBlock = 0,
        InsetInlineStart = Start,
        Width = $"calc({End.Reference} - {Start.Reference})",
    });

    static Style RangeVertical => Css.Define(static () => new Style
    {
        InsetInline = 0,
        Bottom = Start,
        Height = $"calc({End.Reference} - {Start.Reference})",
    });

    static Style Thumb => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        BoxSizing = BoxSizing.BorderBox,
        Width = ThumbSize,
        Height = ThumbSize,
        BorderWidth = BorderWidths.Sm,
        BorderStyle = BorderStyle.Solid,
        BorderColor = SharedStyles.FillColor,
        BorderRadius = Radii.Full,
        BackgroundColor = Colors.Surface,
        BoxShadow = Shadows.Sm,
        Cursor = Cursor.Grab,
    });

    // 用逻辑方向的外边距把滑块的中心对准取值位置：从右到左排版时也对
    static Style ThumbHorizontal => Css.Define(static () => new Style { MarginInlineStart = $"calc({ThumbSize} / -2)" });

    static Style ThumbVertical => Css.Define(static () => new Style { MarginBottom = $"calc({ThumbSize} / -2)" });

    // 轨道里的小圆点；在已选范围里（data-active）时换成反色
    static Style Mark => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Width = Space.Xs,
        Height = Space.Xs,
        BorderRadius = Radii.Full,
        BackgroundColor = Colors.BorderStrong,
        PointerEvents = PointerEvents.None,
        [When.Attribute("[data-active]")] = new() { BackgroundColor = SharedStyles.OnFillColor },
    });

    static Style MarkHorizontal => Css.Define(static () => new Style
    {
        Top = CssLength.Percent(50),
        InsetInlineStart = MarkAt,
        Translate = "-50% -50%",
        [When.Rtl] = new() { Translate = "50% -50%" },
    });

    static Style MarkVertical => Css.Define(static () => new Style
    {
        Left = CssLength.Percent(50),
        Bottom = MarkAt,
        Translate = "-50% 50%",
    });

    static Style MarkLabel => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        FontSize = FontSizes.Sm,
        LineHeight = LineHeights.Tight,
        Color = Colors.TextMuted,
        WhiteSpace = WhiteSpace.Nowrap,
        PointerEvents = PointerEvents.None,
    });

    static Style MarkLabelHorizontal => Css.Define(static () => new Style
    {
        Top = $"calc(50% + {Space.Sm})",
        InsetInlineStart = MarkAt,
        Translate = "-50% -0.75em",
        [When.Rtl] = new() { Translate = "50% -0.75em" },
    });

    static Style MarkLabelVertical => Css.Define(static () => new Style
    {
        Left = $"calc(50% + {Space.Md})",
        Bottom = MarkAt,
        Translate = "0 50%",
    });
}
