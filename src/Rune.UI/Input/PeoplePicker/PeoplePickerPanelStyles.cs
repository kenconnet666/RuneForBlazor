using Rune.Styles;

namespace Rune.UI;

/// <summary>人员选择面板的部件。</summary>
public enum PeoplePickerPanelPart
{
    /// <summary>外层 div（容器查询的容器）。</summary>
    Root,

    /// <summary>左右两栏（窄时上下排）。</summary>
    Grid,

    /// <summary>左边：搜索、浏览方式、面包屑、列表。</summary>
    Browse,

    /// <summary>右边：已选的。</summary>
    Selected,

    /// <summary>已选一侧的表头（个数、清空）。</summary>
    Header,

    /// <summary>“全选”勾选框。</summary>
    SelectAll,

    /// <summary>两边的滚动区域。</summary>
    List,

    /// <summary>行首（勾选框与头像）。</summary>
    Start,

    /// <summary>加载中、没有内容的提示。</summary>
    Status,
}

/// <summary>人员选择面板的样式（同 vue-tsx-pro 的 peoplePickerPanelStyles）。</summary>
public static class PeoplePickerPanelStyles
{
    // 按自己的宽度决定左右排还是上下排
    static readonly Condition Wide = Containers.MinWidth(CssLength.Rem(36));

    /// <summary>各部件的默认层。</summary>
    public static PartLayers<PeoplePickerPanelPart> Of(ComponentStyles styles) =>
        Recipe.Of("people-picker-panel", styles, static (PeoplePickerPanelPart part) => part switch
        {
            PeoplePickerPanelPart.Root => Root,
            PeoplePickerPanelPart.Grid => Grid,
            PeoplePickerPanelPart.Browse => Browse,
            PeoplePickerPanelPart.Selected => Selected,
            PeoplePickerPanelPart.Header => Header,
            PeoplePickerPanelPart.SelectAll => SelectAll,
            PeoplePickerPanelPart.List => List,
            PeoplePickerPanelPart.Start => Start,
            PeoplePickerPanelPart.Status => Status,
            _ => Sx.Of((Style?)null),
        });

    /// <summary>列表的视口：进到下一层后焦点落在它上面，不画焦点环。</summary>
    public static Style ListViewport => Css.Define(static () => new Style { OutlineStyle = OutlineStyle.None });

    static Style Root => Css.Define(static () => new Style { ContainerType = ContainerType.InlineSize, MinWidth = 0 });

    static Style Grid => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        Gap = Space.Md,
        GridTemplateColumns = "minmax(0, 1fr)",
        [Wide] = new() { GridTemplateColumns = "minmax(0, 3fr) minmax(0, 2fr)" },
    });

    static Style Browse => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Gap = Space.Sm, MinWidth = 0 });

    // 左右不留：列表自己留（滚动条贴着边，行尾按钮的焦点环不被滚动区域裁掉），表头跟它对齐
    static Style Selected => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        MinWidth = 0,
        PaddingBlock = Space.Sm,
        BackgroundColor = Colors.SurfaceMuted,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
    });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.SpaceBetween,
        Gap = Space.Sm,
        MinHeight = CssLength.Em(2),
        PaddingInline = Space.Sm,
    });

    // 与列表里条目的勾选框对齐（可悬停的条目两侧留 sm）
    static Style SelectAll => Css.Define(static () => new Style { PaddingInline = Space.Sm });

    static Style List => Css.Define(static () => new Style { MinWidth = 0 });

    static Style Start => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Sm });

    static Style Status => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.Center,
        PaddingBlock = Space.Lg,
        Color = Colors.TextMuted,
    });
}
