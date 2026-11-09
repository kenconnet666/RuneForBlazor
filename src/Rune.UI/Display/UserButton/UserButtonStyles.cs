using Rune.Styles;

namespace Rune.UI;

/// <summary>用户按钮的部件（同 vue-tsx-pro 的 userButtonRecipe）。</summary>
public enum UserButtonPart
{
    /// <summary>按钮（ButtonBase）。</summary>
    Root,

    /// <summary>头像（Avatar）。</summary>
    Avatar,

    /// <summary>名字与说明的外层。</summary>
    Text,

    /// <summary>名字。</summary>
    Name,

    /// <summary>名字下面的说明。</summary>
    Description,

    /// <summary>结束边的箭头（Icon）。</summary>
    Indicator,
}

/// <summary>用户按钮的默认值（UIConfig.Components 里配置）。</summary>
public sealed record UserButtonDefaults : IComponentDefaults<UserButtonDefaults>
{
    /// <summary>只显示头像。</summary>
    public bool? Compact { get; init; }

    /// <inheritdoc/>
    public UserButtonDefaults Over(UserButtonDefaults outer) => new() { Compact = Compact ?? outer.Compact };
}

/// <summary>
/// 用户按钮的样式（配方，同 vue-tsx-pro 的 userButtonRecipe）：只显示头像（compact）与有没有说明是档位。
/// 没有说明时是一颗圆角的胶囊（顶栏）；有说明且不是 compact 时是占满宽度的一行（侧栏底部）。
/// </summary>
public static class UserButtonStyles
{
    /// <summary>某种档位下各部件的默认层。</summary>
    public static PartLayers<UserButtonPart> Of(bool compact, bool described, ComponentStyles styles) =>
        Recipe.Of("user-button", (compact, described), styles, static ((bool Compact, bool Described) a, UserButtonPart part) => part switch
        {
            UserButtonPart.Root => Sx.Of(Root, a.Compact ? CompactRoot : a.Described ? Row : null),
            UserButtonPart.Avatar => Avatar,
            UserButtonPart.Text => Text,
            UserButtonPart.Name => Name,
            UserButtonPart.Description => Description,
            _ => Indicator,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Sm,
        MaxWidth = CssLength.Percent(100),
        PaddingBlock = Space.Xs,
        PaddingInlineStart = Space.Xs,
        PaddingInlineEnd = Space.Sm,
        BorderRadius = Radii.Full,
        Color = Colors.Text,
        TextAlign = TextAlign.Start,
        BackgroundColor = CssColors.Transparent,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });

    static Style CompactRoot => Css.Define(static () => new Style { Padding = Space.Xs });

    // 有说明：占满宽度的一行（侧栏底部），圆角随普通的行
    static Style Row => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        Width = CssLength.Percent(100),
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Md,
    });

    static Style Avatar => Css.Define(static () => new Style { FlexShrink = 0 });

    static Style Text => Css.Define(static () => new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, FlexGrow = 1, MinWidth = 0 });

    static Style Name => Css.Define(static () => new Style
    {
        FontWeight = FontWeights.Medium,
        FontSize = CssLength.Em(0.93),
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    static Style Description => Css.Define(static () => new Style
    {
        Color = Colors.TextMuted,
        FontSize = CssLength.Em(0.86),
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
        WhiteSpace = WhiteSpace.Nowrap,
    });

    static Style Indicator => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted });
}
