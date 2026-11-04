using Rune.Styles;

namespace Rune.UI;

/// <summary>消息提示区域的部件。</summary>
public enum ToasterPart
{
    /// <summary>区域（section，固定在页面的结束边下角）。</summary>
    Root,

    /// <summary>每条提示（Alert）。</summary>
    Toast,
}

/// <summary>消息提示的样式（配方）。</summary>
public static class ToastStyles
{
    /// <summary>某套定制下各部件的默认层。</summary>
    public static PartLayers<ToasterPart> Of(ComponentStyles styles) =>
        Recipe.Of("toaster", styles, static (ToasterPart part) => part switch
        {
            ToasterPart.Root => Root,
            _ => Sx.Of(Toast, SharedStyles.ReducedMotion),
        });

    // 区域本身不挡点击，只有提示能点
    static Style Root => Css.Define(static () => new Style
    {
        Position = Position.Fixed,
        InsetBlockEnd = Space.Lg,
        InsetInlineEnd = Space.Lg,
        ZIndex = ZIndices.Toast,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Sm,
        Width = "min(24em, calc(100vw - 32px))",
        PointerEvents = "none",
    });

    // 新提示从下方滑入；表面换成页面底色，叠在内容上也看得清
    static Style Toast => Css.Define(static () => new Style
    {
        BackgroundColor = Colors.Background,
        BoxShadow = Shadows.Md,
        PointerEvents = "auto",
        Transition = $"opacity {Durations.Normal}, translate {Durations.Normal} {Easings.Enter}",
        [When.Starting] = new() { Opacity = 0, Translate = "0 8px" },
    });
}
