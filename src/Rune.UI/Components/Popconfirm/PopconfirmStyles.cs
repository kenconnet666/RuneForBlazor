using Rune.Styles;

namespace Rune.UI;

/// <summary>气泡确认的部件（浮层本身是 Popover，按 <see cref="PopoverPart"/> 定制）。</summary>
public enum PopconfirmPart
{
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
            PopconfirmPart.Description => Description,
            _ => Actions,
        });

    static Style Description => Css.Define(static () => new Style
    {
        MarginBlockEnd = Space.Md,
        Color = Colors.TextMuted,
    });

    static Style Actions => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        JustifyContent = JustifyContent.FlexEnd,
        Gap = Space.Xs,
    });
}
