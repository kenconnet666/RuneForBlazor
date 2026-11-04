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

    /// <summary>24px。</summary>
    Xxl,

    /// <summary>30px。</summary>
    Xxxl,
}

/// <summary>间距档位（对应 <see cref="Space"/> 的 token）：布局的间隙、卡片的内边距。</summary>
public enum SpaceScale
{
    /// <summary>0。</summary>
    None,

    /// <summary>2px。</summary>
    Xxs,

    /// <summary>4px。</summary>
    Xs,

    /// <summary>8px。</summary>
    Sm,

    /// <summary>12px。</summary>
    Md,

    /// <summary>16px。</summary>
    Lg,

    /// <summary>24px。</summary>
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
            Size.Xxl => FontSizes.Xxl,
            Size.Xxxl => FontSizes.Xxxl,
            _ => FontSizes.Md,
        },
    });

    /// <summary>间隙（gap）。</summary>
    public static Style Gap(SpaceScale scale) => Css.Define(scale, static scale => new Style { Gap = SpaceOf(scale) });

    /// <summary>行间隙（row-gap），盖过 <see cref="Gap"/> 的行方向。</summary>
    public static Style RowGap(SpaceScale scale) => Css.Define(scale, static scale => new Style { RowGap = SpaceOf(scale) });

    /// <summary>列间隙（column-gap），盖过 <see cref="Gap"/> 的列方向。</summary>
    public static Style ColumnGap(SpaceScale scale) => Css.Define(scale, static scale => new Style { ColumnGap = SpaceOf(scale) });

    /// <summary>内边距。</summary>
    public static Style Padding(SpaceScale scale) => Css.Define(scale, static scale => new Style { Padding = SpaceOf(scale) });

    /// <summary>间距档位对应的 token。</summary>
    public static CssLength SpaceOf(SpaceScale scale) => scale switch
    {
        SpaceScale.None => 0,
        SpaceScale.Xxs => Space.Xxs,
        SpaceScale.Xs => Space.Xs,
        SpaceScale.Sm => Space.Sm,
        SpaceScale.Md => Space.Md,
        SpaceScale.Lg => Space.Lg,
        _ => Space.Xl,
    };

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
