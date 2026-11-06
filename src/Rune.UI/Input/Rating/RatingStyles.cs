using Rune.Styles;

namespace Rune.UI;

/// <summary>评分的部件。</summary>
public enum RatingPart
{
    /// <summary>外层（可编辑时 role="radiogroup"，只读时 role="img"）。</summary>
    Root,

    /// <summary>每颗星（span）。</summary>
    Star,

    /// <summary>星里的图标（空心打底的一层与填色的一层）。</summary>
    Icon,
}

/// <summary>评分的外观：选中的颜色、字号档位、只读。</summary>
/// <param name="Tone">选中的颜色。</param>
/// <param name="Size">字号档位；null 时继承。</param>
/// <param name="ReadOnly">只读（不可点，悬停不放大）。</param>
public readonly record struct RatingAppearance(Tone Tone, Size? Size, bool ReadOnly) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Tone, Size];
}

/// <summary>
/// 评分的样式（配方，同 vue-tsx-pro 的 ratingStyles）。每颗星是空心图标打底，填色的一层按 <see cref="Fill"/> 裁出：
/// 可编辑时由 CSS 按勾上的 radio 算（这颗之前的满、这颗按勾的是整颗还是前半颗），悬停预览时 JS 在星上写行内的值盖过它；只读时组件直接写。
/// </summary>
public static class RatingStyles
{
    /// <summary>这颗星填了多少（0%、50%、100%）。</summary>
    public static readonly CssVar<CssLength> Fill = CssVar.Length("rune-rating-fill");

    /// <summary>星的标记：勾选的分数在后面哪颗星里，由兄弟关系判断。</summary>
    public static readonly Marker StarMarker = Css.Marker("rune-rating-star");

    // 每颗星的边长
    static readonly CssLength StarSize = CssLength.Em(1.35);

    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<RatingPart> Of(RatingAppearance appearance, ComponentStyles styles) =>
        Recipe.Of("rating", appearance, styles, static (RatingAppearance a, RatingPart part) => part switch
        {
            RatingPart.Root => Sx.Of(Root, a.Size is { } size ? ScaleStyles.FontSize(size) : null),
            RatingPart.Star => a.ReadOnly ? Sx.Of(Star, ReadOnlyStar) : Sx.Of(Star, CheckedFill, FocusWithin),
            _ => Sx.Of(Icon, Empty),
        });

    /// <summary>填色的那层（裁出 Fill 的宽度）。</summary>
    internal static Style Clip => Css.Define(static () => new Style
    {
        GridArea = "1 / 1",
        Width = $"var({Fill.Name}, 0%)",
        Overflow = Overflow.Hidden,
        PointerEvents = PointerEvents.None,
    });

    /// <summary>填色那层里的图标：按整颗星的大小画，不随裁出的宽度缩小；用实色填满。</summary>
    internal static Style ClipIcon(Tone tone) => Css.Define(tone, static tone => new Style
    {
        Display = Display.Block,
        Width = StarSize,
        Height = StarSize,
        MaxWidth = CssLength.None,
        Fill = CssColors.CurrentColor,
        // 实心与空心靠形状区分，不只靠颜色；Windows 高对比下用系统高亮色
        Color = tone == Tone.Neutral ? Colors.TextMuted : Tones.Of(tone).Solid,
        [Media.ForcedColors] = new() { Color = CssColors.Highlight },
    });

    /// <summary>点击区域：整颗；半星时起始边一半、结束边一半。radio 本身视觉上隐藏。</summary>
    internal static Style HitArea(string half) => Css.Define(half, static half => half switch
    {
        "start" => new Style { Position = Position.Absolute, InsetBlock = 0, InsetInlineStart = 0, Width = CssLength.Percent(50) },
        "end" => new Style { Position = Position.Absolute, InsetBlock = 0, InsetInlineEnd = 0, Width = CssLength.Percent(50) },
        _ => new Style { Position = Position.Absolute, Inset = 0 },
    });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = CssLength.Em(0.15),
        LineHeight = 1,
        [When.Attribute("[data-disabled]")] = new() { Opacity = Opacities.Disabled, Cursor = Cursor.NotAllowed },
    });

    static Style Star => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.InlineGrid,
        Width = StarSize,
        Height = StarSize,
        BorderRadius = Radii.Sm,
        Cursor = Cursor.Pointer,
        Transition = $"scale {Durations.Sm} {Easings.Standard}",
        [When.Hover] = new() { Scale = 1.12 },
        [When.Inside(When.Attribute("[data-disabled]"), RatingMarker)] = new() { Cursor = Cursor.NotAllowed, Scale = 1 },
    });

    /// <summary>外层的标记：不可用时星不放大、不像能点。</summary>
    public static readonly Marker RatingMarker = Css.Marker("rune-rating");

    static Style ReadOnlyStar => Css.Define(static () => new Style { Cursor = Cursor.Default, [When.Hover] = new() { Scale = 1 } });

    // 勾上的分数：后面某颗星里有勾上的 radio 时这颗满；勾上的在自己里面时，整颗或结束边的一半是满、起始边的一半是半颗。三条互斥
    static Style CheckedFill => Css.Define(static () => new Style
    {
        [When.Has($"~ .{StarMarker.ClassName} input:checked")] = new() { [Fill.Name] = "100%" },
        [When.Has("""input:checked:not([data-half="start"])""")] = new() { [Fill.Name] = "100%" },
        [When.Has("""input[data-half="start"]:checked""")] = new() { [Fill.Name] = "50%" },
    });

    // 键盘聚焦里面的 radio 时整颗星画焦点环
    static Style FocusWithin => Css.Define(static () => new Style
    {
        [When.Has(":focus-visible")] = new() { Outline = $"{BorderWidths.Sm} solid {Colors.FocusRing}", OutlineOffset = BorderWidths.Sm },
    });

    static Style Icon => Css.Define(static () => new Style { GridArea = "1 / 1", Width = CssLength.Percent(100), Height = CssLength.Percent(100) });

    static Style Empty => Css.Define(static () => new Style { Color = Colors.BorderStrong });
}
