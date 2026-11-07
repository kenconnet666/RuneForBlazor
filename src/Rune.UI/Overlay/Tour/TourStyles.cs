using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>引导的一步。</summary>
/// <param name="Title">标题（对话框的名称）。</param>
/// <param name="Content">说明。</param>
/// <param name="Target">高亮的目标（CSS 选择器，如 <c>#search</c>）；不写或找不到时卡片居中、整页遮罩。</param>
/// <param name="Placement">卡片在目标的哪一侧，默认下方；空间不够时翻转。</param>
/// <param name="Cover">标题上方的封面：插图、截图、短视频。</param>
public sealed record TourStep(string Title, string? Content = null, string? Target = null, Placement Placement = Placement.Bottom, RenderFragment? Cover = null);

/// <summary>新手引导的部件。</summary>
public enum TourPart
{
    /// <summary>整屏透明的 dialog：遮罩由镂空的阴影画。</summary>
    Root,

    /// <summary>镂空（目标处）；没有目标时整页遮罩。</summary>
    Spotlight,

    /// <summary>卡片。</summary>
    Panel,

    /// <summary>封面。</summary>
    Cover,

    /// <summary>标题（对话框的名称）。</summary>
    Title,

    /// <summary>说明（对话框的描述）。</summary>
    Content,

    /// <summary>底部一行（步数与按钮）。</summary>
    Footer,

    /// <summary>步数（第几步 / 共几步）。</summary>
    Progress,
}

/// <summary>新手引导的样式（配方，同 vue-tsx-pro 的 tourStyles）。镂空的位置、卡片的位置由 JS 写（js/overlay/tour.js）。</summary>
public static class TourStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<TourPart> Of(ComponentStyles styles) =>
        Recipe.Of("tour", styles, static (TourPart part) => part switch
        {
            TourPart.Root => Root,
            TourPart.Spotlight => Spotlight,
            TourPart.Panel => Sx.Of(FloatingStyles.Anchored, FloatingStyles.Surface, Panel),
            TourPart.Cover => Cover,
            TourPart.Title => Title,
            TourPart.Content => Content,
            TourPart.Footer => Footer,
            _ => Progress,
        });

    static string Move(string property) => $"{property} {Durations.Md} {Easings.Standard}";

    // 整屏透明的 dialog：遮罩由镂空的阴影画，原生 ::backdrop 不用
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        Inset = 0,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        MaxWidth = MaxWidth.None,
        MaxHeight = MaxHeight.None,
        Margin = 0,
        Padding = 0,
        BorderWidth = 0,
        BackgroundColor = CssColors.Transparent,
        Overflow = Overflow.Hidden,
        [Pseudo.Backdrop] = new() { BackgroundColor = CssColors.Transparent },
    });

    // 一个盒子画成镂空：外面一圈巨大的阴影就是遮罩；没有目标时整页遮罩
    static Style Spotlight => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        BorderRadius = Radii.Md,
        BoxShadow = $"0 0 0 100vmax {Colors.Overlay}",
        PointerEvents = PointerEvents.None,
        Transition = $"{Move("left")}, {Move("top")}, {Move("width")}, {Move("height")}",
        [When.Attribute("[data-dimmed]")] = new() { Inset = 0, BorderRadius = 0, BackgroundColor = Colors.Overlay, BoxShadow = BoxShadow.None },
    });

    static Style Panel => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Sm,
        Width = "min(22rem, calc(100vw - 2rem))",
        Padding = Space.Lg,
        LineHeight = LineHeights.Normal,
        OutlineStyle = OutlineStyle.None,
        [When.Attribute("[data-centered]")] = new() { Left = CssLength.Percent(50), Top = CssLength.Percent(50), Transform = "translate(-50%, -50%)" },
    });

    // 封面贴着卡片的上、左、右边（盖住内边距），圆角随卡片
    static Style Cover => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Overflow = Overflow.Hidden,
        MarginBlockStart = $"calc(-1 * {Space.Lg})",
        MarginInline = $"calc(-1 * {Space.Lg})",
        MarginBlockEnd = Space.Xs,
        BorderStartStartRadius = CssWide.Inherit,
        BorderStartEndRadius = CssWide.Inherit,
    });

    static Style Title => Css.Define(static () => new Style { Margin = 0 });

    static Style Content => Css.Define(static () => new Style { Color = Colors.TextMuted });

    static Style Footer => Css.Define(static () => new Style { MarginBlockStart = Space.Xs });

    static Style Progress => Css.Define(static () => new Style { MarginInlineEnd = CssLength.Auto });
}
