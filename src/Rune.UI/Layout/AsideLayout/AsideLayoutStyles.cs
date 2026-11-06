using Rune.Styles;

namespace Rune.UI;

/// <summary>主栏 + 侧栏的样式（同 vue-tsx-pro 的 asideLayoutStyles）：外层是容器查询的容器，宽到 56rem 才分两栏、侧栏才粘住。</summary>
public static class AsideLayoutStyles
{
    /// <summary>分两栏的容器宽度。</summary>
    static readonly Condition Wide = Containers.MinWidth(CssLength.Rem(56));

    /// <summary>侧栏的宽度：经它写在网格上（类不随宽度变化）。</summary>
    internal static readonly CssVar<CssLength> AsideWidth = CssVar.Length("rune-aside-width", "26rem");

    internal static readonly Attrs Root = new() { Sx = RootStyle };

    /// <summary>两栏的网格：栏间距按档位。</summary>
    internal static Attrs Grid(SpaceScale gap) => Grids[(int)gap];

    /// <summary>一栏：里面的块按档位竖排；侧栏粘住时加 sticky。</summary>
    internal static Attrs Column(SpaceScale gap, bool sticky) => (sticky ? StickyColumns : Columns)[(int)gap];

    static readonly Attrs[] Grids = [.. Enum.GetValues<SpaceScale>().Select(gap => new Attrs { Sx = Sx.Of(GridStyle, ScaleStyles.Gap(gap)) })];
    static readonly Attrs[] Columns = [.. Enum.GetValues<SpaceScale>().Select(gap => new Attrs { Sx = Sx.Of(ColumnStyle, ScaleStyles.Gap(gap)) })];
    static readonly Attrs[] StickyColumns = [.. Enum.GetValues<SpaceScale>().Select(gap => new Attrs { Sx = Sx.Of(ColumnStyle, ScaleStyles.Gap(gap), StickyStyle) })];

    // 容器查询的容器：按自己的宽度决定叠不叠
    static Style RootStyle => Css.Define(static () => new Style { ContainerType = ContainerType.InlineSize, MinWidth = 0 });

    static Style GridStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        AlignItems = AlignItems.Start,
        GridTemplateColumns = "minmax(0, 1fr)",
        [Wide] = new() { GridTemplateColumns = $"minmax(0, 1fr) {AsideWidth}" },
    });

    static Style ColumnStyle => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, MinWidth = 0 });

    static Style StickyStyle => Css.Define(static () => new Style
    {
        Top = $"calc({LayoutVars.StickyOffset} + {Space.Md})",
        [Wide] = new() { Position = Position.Sticky },
    });
}
