using Rune.Styles;

namespace Rune.UI;

/// <summary>气泡确认的部件（浮层本身是 Popover，按 <see cref="PopoverPart"/> 定制）。</summary>
public enum PopconfirmPart
{
    /// <summary>图标与文字的一行。</summary>
    Body,

    /// <summary>标题前的图标。</summary>
    Icon,

    /// <summary>标题（要确认的问题）。</summary>
    Title,

    /// <summary>说明。</summary>
    Description,

    /// <summary>按钮区。</summary>
    Actions,
}

/// <summary>气泡确认的样式（配方）。</summary>
public static class PopconfirmStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<PopconfirmPart> Of(ComponentStyles styles) =>
        Recipe.Of("popconfirm", styles, static (PopconfirmPart part) => part switch
        {
            PopconfirmPart.Body => Body,
            PopconfirmPart.Icon => Icon,
            PopconfirmPart.Title => Title,
            PopconfirmPart.Description => Description,
            _ => Actions,
        });

    static Style Body => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.FlexStart,
        Gap = Space.Sm,
        MarginBlockEnd = Space.Md,
    });

    // 图标与标题的第一行对齐（行高 1.5em）
    static Style Icon => Css.Define(static () => new Style { FlexShrink = 0, MarginBlock = "0.25em" });

    static Style Title => Css.Define(static () => new Style { FontWeight = FontWeights.Semibold });

    static Style Description => Css.Define(static () => new Style
    {
        MarginBlockStart = Space.Xs,
        Color = Colors.TextMuted,
    });

    static Style Actions => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Xs,
    });
}
