using Rune.Styles;

namespace Rune.UI;

/// <summary>穿梭框的部件。</summary>
public enum TransferPart
{
    /// <summary>外层（div，role="group"）：两边与中间的按钮三栏；窄屏时上下排。</summary>
    Root,

    /// <summary>每一边（section）。</summary>
    Panel,

    /// <summary>每一边的头部：全选框、标题、计数。</summary>
    Header,

    /// <summary>标题。</summary>
    Title,

    /// <summary>搜索框那一层。</summary>
    Search,

    /// <summary>列表（Listbox）：占满剩下的高度，自己滚动。</summary>
    List,

    /// <summary>中间的按钮那一组。</summary>
    Actions,

    /// <summary>按钮里的箭头：窄屏时转成上下。</summary>
    ActionIcon,
}

/// <summary>穿梭框的样式（同 vue-tsx-pro 的 transferStyles）。</summary>
public static class TransferStyles
{
    /// <summary>各部件的默认层。</summary>
    public static PartLayers<TransferPart> Of(ComponentStyles styles) =>
        Recipe.Of("transfer", styles, static (TransferPart part) => part switch
        {
            TransferPart.Root => Root,
            TransferPart.Panel => Panel,
            TransferPart.Header => Header,
            TransferPart.Title => Title,
            TransferPart.Search => Search,
            TransferPart.List => List,
            TransferPart.Actions => Actions,
            _ => ActionIcon,
        });

    /// <summary>搜索框占满宽度。</summary>
    public static Style SearchField => Css.Define(static () => new Style { Width = CssLength.Percent(100) });

    // 窄屏时上下排，按钮换成上下箭头
    static readonly Condition Narrow = Media.Query("(max-width: 40rem)");

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        GridTemplateColumns = "minmax(0, 1fr) auto minmax(0, 1fr)",
        AlignItems = AlignItems.Stretch,
        Gap = Space.Md,
        [Narrow] = new() { GridTemplateColumns = "minmax(0, 1fr)" },
    });

    static Style Panel => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        MinWidth = 0,
        Height = CssLength.Em(20),
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.Border,
        BorderRadius = Radii.Md,
        BackgroundColor = Colors.Surface,
        Overflow = Overflow.Hidden,
    });

    static Style Header => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderBlockEndWidth = BorderWidths.Xs,
        BorderBlockEndStyle = BorderBlockEndStyle.Solid,
        BorderBlockEndColor = Colors.BorderMuted,
    });

    static Style Title => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0, FontWeight = FontWeights.Medium });

    static Style Search => Css.Define(static () => new Style { Padding = Space.Xs, FlexShrink = 0 });

    // 列表占满剩下的高度，自己滚动（虚拟滚动要有确定的高度）
    static Style List => Css.Define(static () => new Style { FlexGrow = 1, MinHeight = 0, BorderWidth = 0, BorderRadius = 0 });

    static Style Actions => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Sm,
        [Narrow] = new() { FlexDirection = FlexDirection.Row },
    });

    static Style ActionIcon => Css.Define(static () => new Style { [Narrow] = new() { Rotate = CssValue.Deg(90) } });
}
