using Rune.Styles;

namespace Rune.UI;

/// <summary>网络图的样式（同 vue-tsx-pro 的 graphChartStyles）：选中时不相连的节点、连线写 data-faded 变淡（C# 渲染）。</summary>
internal static class GraphStyles
{
    // 平移缩放的视口：拖动不让页面滚动
    internal static Style Viewport => Css.Define(static () => new Style { Position = Position.Absolute, Inset = 0, TouchAction = TouchAction.None, Overflow = Overflow.Hidden });

    internal static Style Background => Css.Define(static () => new Style { Fill = CssColors.Transparent });

    internal static Style Panel => Css.Define(static () => new Style { Fill = Colors.SurfaceMuted, Stroke = ChartStyles.Color, StrokeWidth = 1 });

    internal static Style PanelLabel => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs, FontWeight = FontWeights.Medium });

    // 节点、连线：选中别的节点时不相连的变淡
    internal static Style Item => Css.Define(static () => new Style
    {
        Transition = $"opacity {Durations.Sm} {Easings.Standard}",
        [When.Attribute("[data-faded]")] = new() { Opacity = 0.25 },
    });

    internal static Style Link => Css.Define(static () => new Style { Fill = Fill.None, Stroke = ChartStyles.Color });

    internal static Style Dashed => Css.Define(static () => new Style { StrokeDasharray = "5 4" });

    internal static Style Arrow => Css.Define(static () => new Style { Fill = ChartStyles.Color });

    internal static Style LinkLabelBox => Css.Define(static () => new Style { Fill = Colors.Fill });

    internal static Style LinkLabel => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = FontSizes.Xs });

    internal static Style Clickable => Css.Define(static () => new Style { Cursor = Cursor.Pointer });

    // 选中的节点：外面一圈强调色
    internal static Style NodeShape => Css.Define(static () => new Style
    {
        Fill = ChartStyles.Color,
        Stroke = Colors.Surface,
        StrokeWidth = 2,
        [When.Attribute("[data-selected]")] = new() { Stroke = Tones.Accent.Solid, StrokeWidth = 3 },
    });

    internal static Style NodeLabel => Css.Define(static () => new Style { Fill = Colors.Text, FontSize = FontSizes.Xs, FontWeight = FontWeights.Medium });

    internal static Style NodeSublabel => Css.Define(static () => new Style { Fill = Colors.TextMuted, FontSize = FontSizes.Xs });

    // 只按滚轮（没按 Ctrl）时浮出的提示
    internal static Style WheelHint => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = Space.Sm,
        InsetInlineEnd = Space.Sm,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.SurfaceRaised,
        BoxShadow = Shadows.Sm,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
        PointerEvents = PointerEvents.None,
        Display = Display.None,
        [When.Attribute("[data-open]")] = new() { Display = Display.Block },
    });
}
