using Rune.Styles;

namespace Rune.UI;

/// <summary>吸底操作栏的部件。</summary>
public enum ActionBarPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>起始边的说明。</summary>
    Start,

    /// <summary>结束边的按钮区。</summary>
    Actions,
}

/// <summary>吸底操作栏的样式（同 vue-tsx-pro 的 actionBarStyles）。</summary>
public static class ActionBarStyles
{
    /// <summary>各部件的默认层；<paramref name="sticky"/>：贴在滚动区域的底边。</summary>
    public static PartLayers<ActionBarPart> Of(bool sticky, ComponentStyles styles) =>
        Recipe.Of("action-bar", sticky, styles, static (bool sticky, ActionBarPart part) => part switch
        {
            ActionBarPart.Root => sticky ? Sx.Of(Root, Sticky) : Sx.Of(Root),
            ActionBarPart.Start => Start,
            _ => Actions,
        });

    static Style Root => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        FlexWrap = FlexWrap.Wrap,
        Gap = Space.Md,
        PaddingBlock = Space.Md,
        PaddingInline = Space.Lg,
        BorderBlockStartWidth = BorderWidths.Xs,
        BorderBlockStartStyle = BorderStyle.Solid,
        BorderBlockStartColor = Colors.BorderMuted,
        BackgroundColor = Colors.Surface,
        Transition = $"box-shadow {Durations.Md} {Easings.Standard}",
    });

    // 往下多出一条线宽：贴着底边时有 1px 在滚动区域外，交叉比例小于 1，JS 据此写 data-stuck（盖着下面的内容）。
    // 贴着视口底边时让开手机的安全区（主屏幕指示条），按钮不被它压住；盖着内容时阴影提示还能往下滚
    static Style Sticky => Css.Define(static () => new Style
    {
        Position = Position.Sticky,
        InsetBlockEnd = $"calc(-1 * {BorderWidths.Xs})",
        ZIndex = 1,
        PaddingBlockEnd = $"calc({Space.Md} + env(safe-area-inset-bottom, 0px))",
        [When.Attribute("[data-stuck]")] = new() { BoxShadow = Shadows.Md, BorderBlockStartColor = Colors.Border },
    });

    // 说明文字比所在文字小一档
    static Style Start => Css.Define(static () => new Style
    {
        FlexGrow = 1,
        MinWidth = 0,
        Color = Colors.TextMuted,
        FontSize = CssLength.Em(0.93),
    });

    static Style Actions => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.Center,
        FlexWrap = FlexWrap.Wrap,
        Gap = Space.Sm,
        MarginInlineStart = CssLength.Auto,
    });
}
