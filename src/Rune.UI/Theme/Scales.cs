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

    /// <summary>18px。</summary>
    Xl,

    /// <summary>24px。</summary>
    Xxl,

    /// <summary>30px。</summary>
    Xxxl,
}

/// <summary>间距档位（对应 <see cref="Space"/> 的 token，em，随字号缩放）：布局的间隙、卡片的内边距。</summary>
public enum SpaceScale
{
    /// <summary>0。</summary>
    None,

    /// <summary>0.25em：图标与文字之间、紧凑的元素之间。</summary>
    Xs,

    /// <summary>0.5em：控件内部、相关的元素之间。</summary>
    Sm,

    /// <summary>0.75em：一组内容的元素之间。</summary>
    Md,

    /// <summary>1em：表单项之间、段落之间。</summary>
    Lg,

    /// <summary>1.5em：区块之间、卡片与对话框的内边距。</summary>
    Xl,
}

/// <summary>圆角档位（对应 <see cref="Radii"/> 的 token，em，随字号缩放）。</summary>
public enum Radius
{
    /// <summary>直角：表格、密集列表里与直线对齐。</summary>
    None,

    /// <summary>0.15em。</summary>
    Xs,

    /// <summary>0.25em。</summary>
    Sm,

    /// <summary>0.4em：控件与按钮的默认。</summary>
    Md,

    /// <summary>0.7em：卡片、面板。</summary>
    Lg,

    /// <summary>1em。</summary>
    Xl,

    /// <summary>胶囊形（宽高相等时是圆形）。</summary>
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

    /// <summary>左右内边距档位（容器）。</summary>
    public static Style PaddingInline(SpaceScale scale) => Css.Define(scale, static scale => new Style { PaddingInline = SpaceOf(scale) });

    /// <summary>间距档位对应的 token。</summary>
    public static CssLength SpaceOf(SpaceScale scale) => scale switch
    {
        SpaceScale.None => Space.None,
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
            UI.Radius.None => Radii.None,
            UI.Radius.Xs => Radii.Xs,
            UI.Radius.Sm => Radii.Sm,
            UI.Radius.Lg => Radii.Lg,
            UI.Radius.Xl => Radii.Xl,
            UI.Radius.Full => Radii.Full,
            _ => Radii.Md,
        },
    });
}
