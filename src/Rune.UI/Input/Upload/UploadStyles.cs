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

/// <summary>上传的部件（同 vue-tsx-pro 的 uploadRecipe）。</summary>
public enum UploadPart
{
    /// <summary>外层。</summary>
    Root,

    /// <summary>选文件的区域（FileDrop；图片墙里是“添加图片”那一格）。</summary>
    Drop,

    /// <summary>图片墙下面的提示。</summary>
    Hint,

    /// <summary>文件列表（GridList）。</summary>
    List,

    /// <summary>列表的一项。</summary>
    Item,

    /// <summary>文件图标（按状态着色）。</summary>
    Icon,

    /// <summary>文件名与下面一行。</summary>
    Body,

    /// <summary>文件名。</summary>
    Name,

    /// <summary>文件名的链接（有 Url 时）。</summary>
    Link,

    /// <summary>上传进度条。</summary>
    Progress,

    /// <summary>文件名下面一行：大小或错误信息。</summary>
    Meta,

    /// <summary>重试按钮（IconButton）。</summary>
    Retry,

    /// <summary>移除按钮（IconButton）。</summary>
    Remove,

    /// <summary>图片墙：缩略图与添加的那一格。</summary>
    Wall,

    /// <summary>图片墙的缩略图列表。</summary>
    WallList,

    /// <summary>图片墙的一格。</summary>
    Card,

    /// <summary>缩略图（Image，能看大图的按钮）。</summary>
    Image,

    /// <summary>没有缩略图时的占位（图标与文件名）。</summary>
    Placeholder,

    /// <summary>缩略图上的进度、错误信息。</summary>
    Overlay,

    /// <summary>右上角的把手、重试、移除。</summary>
    Actions,

    /// <summary>拖动排序的把手。</summary>
    Handle,
}

/// <summary>
/// 上传的样式（同 vue-tsx-pro 的 uploadStyles）：选文件的区域在上，文件一行一个列在下面；
/// 图片墙是一排同样大小的格子（边长是 --rune-upload-thumbnail），拖动排序的位移由 JS 写在格子上（同 Sortable）。
/// </summary>
public static class UploadStyles
{
    // 放在最前：下面的层在静态初始化时就生成样式，要用到它们
    internal static readonly CssVar<CssLength> ThumbnailVar = CssVar.Length("rune-upload-thumbnail", "104px");
    static readonly Marker WallMarker = Css.Marker("rune-upload-wall");
    static readonly Condition Lifted = When.Attribute("[data-dragging],[data-grabbed]");
    static readonly Condition Dragging = When.Attribute("[data-dragging]");
    static readonly Condition Settling = When.Inside(When.Attribute("[data-settling]"), WallMarker);
    static readonly Condition Failed = When.Attribute("""[data-state="error"]""");

    /// <summary>列表或图片墙时各部件的默认层。</summary>
    public static PartLayers<UploadPart> Of(bool wall, ComponentStyles styles) =>
        Recipe.Of("upload", wall, styles, static (bool wall, UploadPart part) => part switch
        {
            UploadPart.Root => RootStyle,
            UploadPart.Drop => wall ? AddCardStyle : Sx.Of(),
            UploadPart.Hint => MetaStyle,
            UploadPart.Item => Sx.Of(ItemStyles.Row, ItemStyle),
            UploadPart.Icon => IconStyle,
            UploadPart.Body => BodyStyle,
            UploadPart.Name => NameStyle,
            UploadPart.Link => NameLinkStyle,
            UploadPart.Progress => ProgressStyle,
            UploadPart.Meta => MetaStyle,
            // 图片墙上的重试、移除：半透明底，悬停叠状态层
            UploadPart.Retry or UploadPart.Remove => wall ? Sx.Of(ActionStyle, SharedStyles.StateLayerOverlay) : Sx.Of(),
            UploadPart.Wall => WallStyle,
            UploadPart.WallList => WallListStyle,
            UploadPart.Card => CardStyle,
            // 缩略图贴着格子的圆角：焦点环画在里面
            UploadPart.Image => SharedStyles.FocusRingInset,
            UploadPart.Placeholder => PlaceholderStyle,
            UploadPart.Overlay => OverlayStyle,
            UploadPart.Actions => ActionsStyle,
            UploadPart.Handle => Sx.Of(ActionStyle, SharedStyles.StateLayerOverlay, HandleStyle),
            _ => Sx.Of(),
        });

    /// <summary>图片墙列表的标记：拖动排序时各格据此过渡。</summary>
    internal static readonly Attrs WallMarkerLayer = WallMarker.Attrs;

    /// <summary>文件名是链接时：省略号放在链接上、外层不裁，链接的焦点环不被裁掉。</summary>
    internal static readonly Attrs NameLinked = new() { Sx = NameLinkedStyle };

    /// <summary>文件图标随状态的颜色：对勾（完成）、警告（失败）。</summary>
    internal static Attrs? IconStatus(UploadStatus status) => status switch
    {
        UploadStatus.Error => ErrorLayer,
        UploadStatus.Done => DoneLayer,
        _ => null,
    };

    /// <summary>失败时文件名下面一行是错误信息的颜色。</summary>
    internal static Attrs? MetaStatus(bool failed) => failed ? ErrorLayer : null;

    static readonly Attrs ErrorLayer = new() { Sx = ErrorText };
    static readonly Attrs DoneLayer = new() { Sx = DoneText };

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

    static Style NameLinkedStyle => Css.Define(static () => new Style { Overflow = Overflow.Visible });

    static Style NameLinkStyle => Css.Define(static () => new Style
    {
        Display = Display.InlineBlock,
        MaxWidth = CssLength.Percent(100),
        VerticalAlign = VerticalAlign.Top,
        Overflow = Overflow.Hidden,
        TextOverflow = TextOverflow.Ellipsis,
    });

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

    // 悬停叠状态层（换成实底在图片上几乎看不出来）
    static Style ActionStyle => Css.Define(static () => new Style
    {
        BackgroundColor = $"color-mix(in oklab, {Colors.Surface} 85%, transparent)",
    });

    static Style HandleStyle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        Width = CssLength.Em(1.75),
        Height = CssLength.Em(1.75),
        BorderRadius = Radii.Sm,
        Color = Colors.Text,
        Cursor = Cursors.Grab,
        TouchAction = TouchAction.None,
        [When.Active] = new() { Cursor = Cursors.Grabbing },
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
