using Rune.Styles;

namespace Rune.UI;

/// <summary>认证页版式的部件。</summary>
public enum AuthLayoutPart
{
    /// <summary>外层 div。</summary>
    Root,

    /// <summary>品牌区。</summary>
    Hero,

    /// <summary>表单区（main）。</summary>
    Main,

    /// <summary>结束边上角的工具区。</summary>
    Tools,
}

/// <summary>认证页版式的样式（同 vue-tsx-pro 的 authLayoutStyles）。</summary>
public static class AuthLayoutStyles
{
    /// <summary>各部件的默认层；<paramref name="split"/>：有品牌区，分两栏（窄屏两行）。</summary>
    public static PartLayers<AuthLayoutPart> Of(bool split, ComponentStyles styles) =>
        Recipe.Of("auth-layout", split, styles, static (bool split, AuthLayoutPart part) => part switch
        {
            AuthLayoutPart.Root => split ? Sx.Of(Root, Split) : Sx.Of(Root),
            AuthLayoutPart.Hero => Hero,
            AuthLayoutPart.Main => Main,
            _ => Tools,
        });

    // 工具区按整页定位：宽屏时整页的上角就是表单区的上角，窄屏时在品牌区那一条上
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        Display = Display.Grid,
        GridTemplateColumns = "minmax(0, 1fr)",
        GridTemplateRows = "minmax(0, 1fr)",
        MinHeight = CssLength.Dvh(100),
        BackgroundColor = Colors.Background,
        Color = Colors.Text,
    });

    // 有品牌区：窄屏上下两行（品牌区按内容高、表单占剩下的），宽屏左右两栏
    static Style Split => Css.Define(static () => new Style
    {
        GridTemplateRows = "auto minmax(0, 1fr)",
        [Breakpoints.Lg] = new() { GridTemplateColumns = "minmax(0, 1.15fr) minmax(0, 1fr)", GridTemplateRows = "minmax(0, 1fr)" },
    });

    static Style Hero => Css.Define(static () => new Style
    {
        MinHeight = CssLength.Auto,
        [Breakpoints.Lg] = new() { MinHeight = CssLength.Dvh(100) },
    });

    static Style Main => Css.Define(static () => new Style
    {
        Display = Display.Flex,
        AlignItems = AlignItems.FlexStart,
        JustifyContent = JustifyContent.Center,
        BoxSizing = BoxSizing.BorderBox,
        Padding = Space.Lg,
        PaddingTop = Space.Xl,
        [Breakpoints.Lg] = new() { AlignItems = AlignItems.Center, PaddingTop = Space.Lg },
    });

    static Style Tools => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = Space.Md,
        InsetInlineEnd = Space.Md,
        ZIndex = 1,
    });
}
