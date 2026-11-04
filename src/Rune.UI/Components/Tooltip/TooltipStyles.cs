using Rune.Styles;

namespace Rune.UI;

/// <summary>提示的部件。</summary>
public enum TooltipPart
{
    /// <summary>包住触发元素的外层（span，锚点）。</summary>
    Root,

    /// <summary>提示本身（role="tooltip"）。</summary>
    Content,
}

/// <summary>
/// 提示的样式（配方）。显示与隐藏全在 CSS 里：外层悬停或里面有键盘焦点时显示（延迟一会儿，扫过时不闪），
/// 不经过 Blazor，Server 模式下也没有往返延迟；按 Esc 后 JS 给外层加 data-dismissed 隐藏。
/// </summary>
public static class TooltipStyles
{
    /// <summary>某个位置、某套定制下各部件的默认层。</summary>
    public static PartLayers<TooltipPart> Of(Placement placement, ComponentStyles styles) =>
        Recipe.Of("tooltip", placement, styles, static (Placement placement, TooltipPart part) => part switch
        {
            TooltipPart.Root => Root,
            _ => Sx.Of(Content, Anchor.Position(placement, alignStart: false), SharedStyles.ReducedMotion),
        });

    // 外层不改变触发元素的排版：行内、按内容收缩
    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        VerticalAlign = VerticalAlign.Middle,
    });

    // 外层处于“要显示”的状态：没被 Esc 关掉，并且悬停或里面有键盘焦点
    static readonly Condition shown = When.Parent(new Condition(":not([data-dismissed]):is(:hover, :has(:focus-visible))"));

    static Style Content => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        ZIndex = ZIndices.Tooltip,
        Width = "max-content",
        MaxWidth = "min(20em, calc(100vw - 16px))",
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Sm,
        BackgroundColor = Tones.Neutral.Solid,
        Color = Tones.Neutral.Contrast,
        FontSize = FontSizes.Xs,
        FontWeight = FontWeights.Normal,
        LineHeight = LineHeights.Tight,
        TextAlign = TextAlign.Start,
        WhiteSpace = WhiteSpace.Normal,
        Visibility = "hidden",
        Opacity = 0,
        // 隐藏：立即淡出，淡出结束再 visibility: hidden
        Transition = $"opacity {Durations.Fast}, visibility 0s linear {Durations.Fast}",
        [shown] = new()
        {
            Visibility = "visible",
            Opacity = 1,
            // 显示：停留 400ms 再出现
            Transition = $"opacity {Durations.Fast} 400ms, visibility 0s 400ms",
        },
    });
}
