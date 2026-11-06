using Rune.Styles;

namespace Rune.UI;

/// <summary>上传的样式（同 vue-tsx-pro 的 uploadStyles）：选文件的区域在上，文件一行一个列在下面。</summary>
public static class UploadStyles
{
    internal static readonly Attrs Root = new() { Sx = RootStyle };
    internal static readonly Attrs Item = new() { Sx = Sx.Of(ItemStyles.Row, ItemStyle) };
    internal static readonly Attrs Body = new() { Sx = BodyStyle };
    internal static readonly Attrs Name = new() { Sx = NameStyle };

    /// <summary>文件图标：按状态是文件、对勾（完成）或警告（失败）的颜色。</summary>
    internal static Attrs Icon(UploadStatus status) => status switch
    {
        UploadStatus.Error => IconError,
        UploadStatus.Done => IconDone,
        _ => IconPlain,
    };

    /// <summary>文件名下面一行：大小，失败时是错误信息。</summary>
    internal static Attrs Meta(bool failed) => failed ? MetaError : MetaPlain;

    internal static readonly Attrs Progress = new() { Sx = ProgressStyle };

    static readonly Attrs IconPlain = new() { Sx = IconStyle };
    static readonly Attrs IconError = new() { Sx = Sx.Of(IconStyle, ErrorText) };
    static readonly Attrs IconDone = new() { Sx = Sx.Of(IconStyle, DoneText) };
    static readonly Attrs MetaPlain = new() { Sx = MetaStyle };
    static readonly Attrs MetaError = new() { Sx = Sx.Of(MetaStyle, ErrorText) };

    static Style RootStyle => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Sm });

    static Style ItemStyle => Css.Define(static () => new Style
    {
        Cursor = Cursor.Default,
        Gap = Space.Sm,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
    });

    static Style IconStyle => Css.Define(static () => new Style { FlexShrink = 0, Color = Colors.TextMuted });

    static Style BodyStyle => Css.Define(static () => new Style { Display = Display.Grid, Gap = Space.Xs, FlexGrow = 1, MinWidth = 0 });

    static Style NameStyle => Css.Define(static () => new Style { Overflow = Overflow.Hidden, TextOverflow = TextOverflow.Ellipsis, WhiteSpace = WhiteSpace.Nowrap });

    static Style MetaStyle => Css.Define(static () => new Style { FontSize = FontSizes.Sm, Color = Colors.TextMuted });

    static Style ErrorText => Css.Define(static () => new Style { Color = Tones.Danger.Text });

    static Style DoneText => Css.Define(static () => new Style { Color = Tones.Success.Text });

    static Style ProgressStyle => Css.Define(static () => new Style { Width = CssLength.Percent(100) });
}
