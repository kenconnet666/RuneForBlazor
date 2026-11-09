using Rune.Styles;

namespace Rune.UI;

/// <summary>上传列表的样子。</summary>
public enum UploadListType
{
    /// <summary>一行一个文件（默认）。</summary>
    Text,

    /// <summary>图片墙：方形缩略图，选文件的区域是最后一格。</summary>
    Picture,
}

/// <summary>
/// 上传的样式（同 vue-tsx-pro 的 uploadStyles）：选文件的区域在上，文件一行一个列在下面；
/// 图片墙是一排同样大小的格子（边长是 --rune-upload-thumbnail），拖动排序的位移由 JS 写在格子上（同 Sortable）。
/// </summary>
internal static class UploadStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssLength> ThumbnailVar = CssVar.Length("rune-upload-thumbnail", "104px");
    static readonly Marker WallMarker = Css.Marker("rune-upload-wall");
    static readonly Condition Lifted = When.Attribute("[data-dragging],[data-grabbed]");
    static readonly Condition Dragging = When.Attribute("[data-dragging]");
    static readonly Condition Settling = When.Inside(When.Attribute("[data-settling]"), WallMarker);
    static readonly Condition Failed = When.Attribute("""[data-state="error"]""");

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

    internal static readonly Attrs Wall = new() { Sx = WallStyle };
    internal static readonly Attrs WallList = Attrs.Layer(WallMarker.Attrs, new Attrs { Sx = WallListStyle });
    internal static readonly Attrs Card = new() { Sx = CardStyle };
    internal static readonly Attrs Placeholder = new() { Sx = PlaceholderStyle };
    internal static readonly Attrs Overlay = new() { Sx = OverlayStyle };
    internal static readonly Attrs Actions = new() { Sx = ActionsStyle };
    internal static readonly Attrs Action = new() { Sx = ActionStyle };
    internal static readonly Attrs Handle = new() { Sx = Sx.Of(ActionStyle, HandleStyle) };
    internal static readonly Attrs AddCard = new() { Sx = AddCardStyle };
    internal static readonly Attrs Hint = new() { Sx = MetaStyle };

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

    // 图片墙：缩略图与“添加图片”一样大，排成一行、放不下换行
    static Style WallStyle => Css.Define(static () => new Style { Display = Display.Flex, FlexWrap = FlexWrap.Wrap, Gap = Space.Sm });

    // 列表里只有缩略图（“添加图片”不是列表项）：自己不占盒子，缩略图仍和“添加图片”排在一行
    static Style WallListStyle => Css.Define(static () => new Style { Display = Display.Contents });

    // 拖动时其余格子让位带过渡，被拖着的不带过渡、浮在上面；顺序跟上后（data-settling）去掉位移不要过渡
    static Style CardStyle => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        BoxSizing = BoxSizing.BorderBox,
        FlexShrink = 0,
        Width = ThumbnailVar,
        Height = ThumbnailVar,
        BorderWidth = BorderWidths.Xs,
        BorderStyle = BorderStyle.Solid,
        BorderColor = Colors.BorderMuted,
        BorderRadius = Radii.Md,
        Overflow = Overflow.Hidden,
        BackgroundColor = Colors.Fill,
        TransitionProperty = "transform, box-shadow",
        TransitionDuration = Durations.Sm,
        TransitionTimingFunction = Easings.Standard,
        [Failed] = new() { BorderColor = Tones.Danger.Solid },
        [Lifted] = new() { ZIndex = 1, BoxShadow = Shadows.Md },
        [Dragging] = new() { TransitionProperty = "box-shadow" },
        [Settling] = new() { TransitionProperty = TransitionProperty.None },
        [Media.ReducedMotion] = new() { TransitionProperty = TransitionProperty.None },
    });

    // 不是图片（或没有地址）：图标与文件名
    static Style PlaceholderStyle => Css.Define(static () => new Style
    {
        BoxSizing = BoxSizing.BorderBox,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        AlignItems = AlignItems.Center,
        JustifyContent = JustifyContent.Center,
        Gap = Space.Xs,
        Width = CssLength.Percent(100),
        Height = CssLength.Percent(100),
        Padding = Space.Xs,
        Color = Colors.TextMuted,
        FontSize = FontSizes.Xs,
        TextAlign = TextAlign.Center,
        OverflowWrap = OverflowWrap.Anywhere,
    });

    // 上传中、失败：盖在缩略图下部
    static Style OverlayStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInline = 0,
        Bottom = 0,
        Display = Display.Flex,
        FlexDirection = FlexDirection.Column,
        Gap = Space.Xs,
        Padding = Space.Xs,
        BackgroundColor = Colors.Scrim,
        Color = Colors.OnScrim,
        FontSize = FontSizes.Xs,
    });

    // 右上角的把手、重试、移除：半透明的表面色底上的小按钮（深色主题里也看得清）
    static Style ActionsStyle => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        Top = Space.Xs,
        InsetInlineEnd = Space.Xs,
        Display = Display.Flex,
        Gap = Space.Xxs,
    });

    static Style ActionStyle => Css.Define(static () => new Style
    {
        BackgroundColor = $"color-mix(in oklab, {Colors.Surface} 85%, transparent)",
        [When.Hover] = new() { BackgroundColor = Colors.Surface },
    });

    static Style HandleStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        Width = CssLength.Em(1.75),
        Height = CssLength.Em(1.75),
        BorderRadius = Radii.Sm,
        Color = Colors.Text,
        Cursor = Cursor.Grab,
        TouchAction = TouchAction.None,
        [When.Active] = new() { Cursor = Cursor.Grabbing },
    });

    // 选文件的那一格：同样大小，图标与文字居中
    static Style AddCardStyle => Css.Define(static () => new Style
    {
        FlexShrink = 0,
        Width = ThumbnailVar,
        Height = ThumbnailVar,
        MinHeight = 0,
        Padding = Space.Xs,
        Gap = Space.Xs,
        TextAlign = TextAlign.Center,
        FontSize = FontSizes.Sm,
    });
}
