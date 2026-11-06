using Rune.Styles;

namespace Rune.UI;

/// <summary>通知显示在视口的哪个位置。</summary>
public enum ToastPlacement
{
    /// <summary>右下（默认；从右到左排版时左下）。</summary>
    BottomEnd,

    /// <summary>底部居中。</summary>
    Bottom,

    /// <summary>左下（从右到左排版时右下）。</summary>
    BottomStart,

    /// <summary>右上（从右到左排版时左上）。</summary>
    TopEnd,

    /// <summary>顶部居中。</summary>
    Top,

    /// <summary>左上（从右到左排版时右上）。</summary>
    TopStart,
}

/// <summary>通知区域的部件。</summary>
public enum ToasterPart
{
    /// <summary>区域（section，固定在视口的一角）。</summary>
    Root,

    /// <summary>列表（ol，aria-live）。</summary>
    List,

    /// <summary>每一项（li）。</summary>
    Item,

    /// <summary>每条通知（Alert）。</summary>
    Toast,

    /// <summary>剩余时间条。</summary>
    Progress,
}

/// <summary>
/// 通知区域的样式（同 vue-tsx-pro 的 toastStyles）：贴着视口的一角，区域本身不挡点击；
/// 新通知从边缘淡入，离场时高度收起；叠放时只露出最新的一条，更早的缩小叠在后面。
/// </summary>
public static class ToastStyles
{
    /// <summary>某个位置、某套定制下各部件的默认层。</summary>
    public static PartLayers<ToasterPart> Of(ToastPlacement placement, ComponentStyles styles) =>
        Recipe.Of("toaster", placement, styles, static (ToastPlacement placement, ToasterPart part) => part switch
        {
            ToasterPart.Root => Sx.Of(Region, PlacementStyle(placement)),
            ToasterPart.List => FromTop(placement) ? ListFromTop : ListFromBottom,
            ToasterPart.Item => Sx.Of(Item, FromTop(placement) ? ItemFromTop : ItemFromBottom),
            ToasterPart.Toast => ToastStyle,
            _ => Countdown,
        });

    /// <summary>在顶部：最新的通知在最上面，进场从上方滑入。</summary>
    internal static bool FromTop(ToastPlacement placement) => placement is ToastPlacement.Top or ToastPlacement.TopStart or ToastPlacement.TopEnd;

    /// <summary>正在离场的一项：淡出（高度由 Collapse 收起）。</summary>
    internal static Style ItemClosing => Css.Define(static () => new Style
    {
        Opacity = 0,
        Transition = $"opacity {Durations.Sm} {Easings.Exit}",
    });

    /// <summary>每一项里放间距的一层（Collapse 的内容层）：离场收起高度时间距一起收掉；剩余时间条按它定位。</summary>
    internal static Style Gap => Css.Define(static () => new Style { Position = Position.Relative, PaddingBlock = Space.Xs });

    /// <summary>剩余时间条暂停（悬停、聚焦时计时也停着）。</summary>
    internal static Style CountdownPaused => Css.Define(static () => new Style { AnimationPlayState = AnimationPlayState.Paused });

    /// <summary>叠放的一层：定位、按深度排层叠顺序，位移与缩放过渡。</summary>
    internal static Style Stacked => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Transition = $"opacity {Durations.Md} {Easings.Standard}, translate {Durations.Md} {Easings.Standard}, scale {Durations.Md} {Easings.Standard}",
    });

    /// <summary>叠在最新一条后面的（更早的）：绝对定位贴着最新那条的边缘。</summary>
    internal static Style StackedBehind(bool fromTop) => fromTop ? StackedBehindTop : StackedBehindBottom;

    /// <summary>叠放时第三层之后的隐藏。</summary>
    internal static Style StackedHidden => Css.Define(static () => new Style { Opacity = 0 });

    static Style StackedBehindTop => Css.Define(static () => new Style { Position = Position.Absolute, InsetInline = 0, Top = 0 });

    static Style StackedBehindBottom => Css.Define(static () => new Style { Position = Position.Absolute, InsetInline = 0, Bottom = 0 });

    // 原生 popover 那样贴着视口的一角；区域本身不挡下面页面的点击，只有通知能点
    static Style Region => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        ZIndex = ZIndices.Toast,
        Margin = 0,
        Padding = Space.Lg,
        BoxSizing = BoxSizing.BorderBox,
        Width = "min(26em, 100vw)",
        MaxHeight = CssLength.Dvh(100),
        PointerEvents = PointerEvents.None,
        OutlineStyle = OutlineStyle.None,
    });

    static Style PlacementStyle(ToastPlacement placement) => Css.Define(placement, static placement => placement switch
    {
        ToastPlacement.TopStart => new Style { Top = 0, InsetInlineStart = 0 },
        ToastPlacement.Top => new Style { Top = 0, InsetInline = 0, MarginInline = CssLength.Auto },
        ToastPlacement.TopEnd => new Style { Top = 0, InsetInlineEnd = 0 },
        ToastPlacement.BottomStart => new Style { Bottom = 0, InsetInlineStart = 0 },
        ToastPlacement.Bottom => new Style { Bottom = 0, InsetInline = 0, MarginInline = CssLength.Auto },
        _ => new Style { Bottom = 0, InsetInlineEnd = 0 },
    });

    // 新通知靠近所在的边缘：在顶部时最新的在最上面
    static Style ListFromBottom => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Margin = 0, Padding = 0, ListStyle = ListStyle.None });

    static Style ListFromTop => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.ColumnReverse, Margin = 0, Padding = 0, ListStyle = ListStyle.None });

    // 纵向仍可滚动页面，横向交给滑动关闭
    static Style Item => Css.Define(static () => new Style
    {
        PointerEvents = PointerEvents.Auto,
        TouchAction = TouchAction.PanY,
        Opacity = 1,
        Transition = $"opacity {Durations.Md} {Easings.Enter}, translate {Durations.Md} {Easings.Enter}",
        [When.Starting] = new() { Opacity = 0 },
    });

    static Style ItemFromBottom => Css.Define(static () => new Style { [When.Starting] = new() { Translate = "0 0.75em" } });

    static Style ItemFromTop => Css.Define(static () => new Style { [When.Starting] = new() { Translate = "0 -0.75em" } });

    static Style ToastStyle => Css.Define(static () => new Style { BoxShadow = Shadows.Lg });

    // 贴着通知底边的细条，从满到空；时长是这条通知的计时，经行内的 animation-duration 给出
    static Style Countdown => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInline = Space.Md,
        Bottom = $"calc({Space.Xs} + {BorderWidths.Xs})",
        Height = BorderWidths.Sm,
        BorderRadius = Radii.Full,
        BackgroundColor = $"color-mix(in srgb, {Colors.TextMuted} 50%, transparent)",
        TransformOrigin = TransformOrigin.Left,
        Keyframes = Shrink,
        AnimationTimingFunction = AnimationTimingFunction.Linear,
        AnimationFillMode = AnimationFillMode.Forwards,
        PointerEvents = PointerEvents.None,
        [When.Rtl] = new() { TransformOrigin = TransformOrigin.Right },
    });

    static Keyframes Shrink => Css.Define(static () => new Keyframes
    {
        ["from"] = new() { Transform = "scaleX(1)" },
        ["to"] = new() { Transform = "scaleX(0)" },
    });
}
