using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 字号档位：组件根元素按它设字号，内部尺寸（内边距、图标、高度）用 em 跟着缩放。
/// 组件上不写时继承上下文字号。
/// </summary>
public enum Size
{
    /// <summary>12px。</summary>
    Xs,

    /// <summary>13px。</summary>
    Sm,

    /// <summary>14px。</summary>
    Md,

    /// <summary>16px。</summary>
    Lg,

    /// <summary>20px。</summary>
    Xl,
}

/// <summary>圆角档位。</summary>
public enum Radius
{
    /// <summary>直角。</summary>
    None,

    /// <summary>小圆角。</summary>
    Sm,

    /// <summary>中圆角（默认）。</summary>
    Md,

    /// <summary>大圆角。</summary>
    Lg,

    /// <summary>胶囊形（两端全圆）。</summary>
    Full,
}

/// <summary>档位对应的样式：每个档位一段，组件把它放进部件的 Sx。</summary>
public static class ScaleStyles
{
    /// <summary>字号档位的样式。</summary>
    public static Style FontSize(Size size) => Css.Define(size, static size => new Style
    {
        FontSize = size switch
        {
            Size.Xs => FontSizes.Xs,
            Size.Sm => FontSizes.Sm,
            Size.Lg => FontSizes.Lg,
            Size.Xl => FontSizes.Xl,
            _ => FontSizes.Md,
        },
    });

    /// <summary>圆角档位的样式。</summary>
    public static Style Radius(Radius radius) => Css.Define(radius, static radius => new Style
    {
        BorderRadius = radius switch
        {
            UI.Radius.None => 0,
            UI.Radius.Sm => Radii.Sm,
            UI.Radius.Lg => Radii.Lg,
            UI.Radius.Full => Radii.Full,
            _ => Radii.Md,
        },
    });
}
