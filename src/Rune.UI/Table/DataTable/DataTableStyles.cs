using Rune.Styles;

namespace Rune.UI;

/// <summary>数据表格的部件（同 vue-tsx-pro 的 dataTableRecipe）。</summary>
public enum DataTablePart
{
    /// <summary>外层（ScrollArea 组件，两个方向滚动）：表格的高度在这里给，默认最高 32em。</summary>
    Root,

    /// <summary>table。</summary>
    Table,

    /// <summary>表头（thead，吸顶）。</summary>
    Head,

    /// <summary>表头的行。</summary>
    HeadRow,

    /// <summary>列标题（th）。</summary>
    HeaderCell,

    /// <summary>勾选列的列标题，叠在 <see cref="HeaderCell"/> 上。</summary>
    SelectHeaderCell,

    /// <summary>分组标题（th，跨组里的几列），叠在 <see cref="HeaderCell"/> 上。</summary>
    GroupHeaderCell,

    /// <summary>详情列的列标题（文字只给读屏），叠在 <see cref="HeaderCell"/> 上。</summary>
    DetailHeaderCell,

    /// <summary>有表头筛选、列菜单按钮的列标题里的一行：文字（或排序按钮）占满剩下的宽度，按钮在结尾。</summary>
    HeaderContent,

    /// <summary>上面一行里的文字（或排序按钮）。</summary>
    HeaderMain,

    /// <summary>可排序列标题里的排序按钮。</summary>
    SortButton,

    /// <summary>排序按钮里的文字。</summary>
    SortLabel,

    /// <summary>排序按钮里的箭头。</summary>
    SortIcon,

    /// <summary>多列排序时的次序（1、2…）。</summary>
    SortPriority,

    /// <summary>表头的全选框。</summary>
    SelectAll,

    /// <summary>表体（tbody）。</summary>
    Body,

    /// <summary>一行（tr）。</summary>
    Row,

    /// <summary>单元格（td）。</summary>
    Cell,

    /// <summary>勾选列的格，叠在 <see cref="Cell"/> 上。</summary>
    SelectCell,

    /// <summary>每行的勾选框（多选）。</summary>
    SelectCheckbox,

    /// <summary>每行的单选框（单选）。</summary>
    SelectRadio,

    /// <summary>每行展开按钮所在的格，叠在 <see cref="Cell"/> 上。</summary>
    DetailToggleCell,

    /// <summary>展开按钮。</summary>
    DetailToggle,

    /// <summary>详情行（tr）。</summary>
    DetailRow,

    /// <summary>详情所在的格（跨全部列），叠在 <see cref="Cell"/> 上。</summary>
    DetailCell,

    /// <summary>详情的内容：贴着视口的起始边，宽度不超过视口，横向滚动时不跟着滚走。</summary>
    DetailContent,

    /// <summary>加载中的骨架条。</summary>
    Skeleton,

    /// <summary>空状态所在的格（跨全部列），叠在 <see cref="Cell"/> 上。</summary>
    EmptyCell,

    /// <summary>虚拟滚动上下的占位格。</summary>
    Spacer,

    /// <summary>表尾（tfoot，合计行，吸底）。</summary>
    Foot,

    /// <summary>合计行的格，叠在 <see cref="Cell"/> 上。</summary>
    SummaryCell,
}

/// <summary>数据表格的默认外观（UIConfig.Defaults 里配置，同 vue-tsx-pro 的 defaults.DataTable）。</summary>
public sealed record DataTableDefaults : IComponentDefaults<DataTableDefaults>
{
    /// <summary>密度。</summary>
    public TableDensity? Density { get; init; }

    /// <summary>样式。</summary>
    public TableVariant? Variant { get; init; }

    /// <summary>斑马纹。</summary>
    public bool? Striped { get; init; }

    /// <inheritdoc/>
    public DataTableDefaults Over(DataTableDefaults outer) => new()
    {
        Density = Density ?? outer.Density,
        Variant = Variant ?? outer.Variant,
        Striped = Striped ?? outer.Striped,
    };
}

/// <summary>数据表格的外观档位：交给 <see cref="DataTableStyles.Of"/>。</summary>
/// <param name="Density">密度。</param>
/// <param name="Variant">样式。</param>
/// <param name="Striped">这一行是斑马纹的浅色底（按行号隔行：虚拟滚动里 :nth-child 数不准）。</param>
public readonly record struct DataTableLook(TableDensity Density, TableVariant Variant, bool Striped = false) : IAppearance
{
    IEnumerable<Enum?> IAppearance.Variants => [Density, Variant];
}

/// <summary>
/// 数据表格的样式（配方，同 vue-tsx-pro 的 dataTableRecipe 与 dataTableStateStyles）：表头、单元格与静态 Table 共用一份（TableStyles）；
/// 对齐、固定列、已选这些按列、按状态的样式叠在部件层之后。
/// </summary>
public static class DataTableStyles
{
    /// <summary>某种外观、某套定制下各部件的默认层。</summary>
    public static PartLayers<DataTablePart> Of(DataTableLook look, ComponentStyles styles) =>
        Recipe.Of("data-table", look, styles, static (DataTableLook a, DataTablePart part) =>
        {
            var padding = TableStyles.PaddingOf(a.Density);
            return part switch
            {
                DataTablePart.Root => Sx.Of(Root, a.Variant == TableVariant.Ghost ? TableStyles.GhostRoot : TableStyles.SurfaceRoot),
                DataTablePart.Table => Sx.Of(Table, a.Density == TableDensity.Compact ? TableStyles.CompactTable : null),
                DataTablePart.Head => Head,
                DataTablePart.HeaderCell => Sx.Of(TableStyles.HeaderCell, padding),
                DataTablePart.SelectHeaderCell or DataTablePart.DetailHeaderCell or DataTablePart.DetailToggleCell => ControlCell,
                DataTablePart.GroupHeaderCell => GroupHeaderCell,
                DataTablePart.DetailToggle => DetailToggle,
                DataTablePart.DetailCell => DetailCell,
                DataTablePart.DetailContent => DetailContent,
                DataTablePart.HeaderContent => HeaderContent,
                DataTablePart.HeaderMain => HeaderMain,
                DataTablePart.SortButton => SortButton,
                DataTablePart.SortLabel => Ellipsis,
                DataTablePart.SortIcon => SortIcon,
                DataTablePart.SortPriority => SortPriority,
                DataTablePart.SelectAll or DataTablePart.SelectCheckbox or DataTablePart.SelectRadio => InlineControl,
                DataTablePart.Row => Sx.Of(a.Variant == TableVariant.Ghost ? TableStyles.GhostRow : TableStyles.SurfaceRow, a.Striped ? StripedRow : null),
                DataTablePart.Cell => Sx.Of(TableStyles.Cell, padding),
                DataTablePart.SelectCell => ControlCell,
                DataTablePart.Skeleton => Skeleton,
                DataTablePart.EmptyCell => EmptyCell,
                DataTablePart.Spacer => Spacer,
                DataTablePart.Foot => Foot,
                DataTablePart.SummaryCell => SummaryCell,
                _ => Sx.Of(),
            };
        });

    /// <summary>放在竖排的弹性容器里：按行数自己的高度，最多占到剩下的高度，再多在表格里滚动。</summary>
    internal static Style Flexible => Css.Define(static () => new Style { FlexGrow = 0, FlexShrink = 1, FlexBasis = CssLength.Auto, MaxHeight = MaxHeight.None, MinHeight = 0 });

    /// <summary>已选的行。</summary>
    internal static Style Selected => Css.Define(static () => new Style
    {
        BackgroundColor = Tones.Accent.Soft,
        [When.Hover] = new() { BackgroundColor = Tones.Accent.SoftHover },
    });

    /// <summary>主从视图里正在看的一行：同已选的底色，起始边的竖线画在第一个格上（<see cref="ActiveEdge"/>）。</summary>
    internal static Style Active => Selected;

    /// <summary>正在看的一行起始边的竖线（浏览器不给 tr 画阴影）。</summary>
    internal static Style ActiveEdge => Css.Define(static () => new Style
    {
        BoxShadow = $"inset 3px 0 0 {Tones.Accent.Solid}",
        [When.Rtl] = new() { BoxShadow = $"inset -3px 0 0 {Tones.Accent.Solid}" },
    });

    /// <summary>整行能点（打开详情）。</summary>
    internal static Style Clickable => Css.Define(static () => new Style { Cursor = Cursor.Pointer });

    /// <summary>行的色调：不透明的浅色底（固定列也盖得住），悬停加深。</summary>
    internal static Style ToneOf(Tone tone) => tone switch
    {
        Tone.Info => InfoRow,
        Tone.Success => SuccessRow,
        Tone.Warning => WarningRow,
        Tone.Danger => DangerRow,
        _ => Selected,
    };

    /// <summary>固定列（偏移写在 inset-inline-start / end 上）：在普通单元格之上，表头在它之上。</summary>
    internal static Style Sticky => Css.Define(static () => new Style { Position = Position.Sticky, ZIndex = 1 });

    /// <summary>起始边固定列里最靠里的一列：与滚动的列之间一条竖线。</summary>
    internal static Style StickyStartEdge => Css.Define(static () => new Style
    {
        BorderInlineEndWidth = BorderWidths.Xs,
        BorderInlineEndStyle = BorderStyle.Solid,
        BorderInlineEndColor = Colors.BorderMuted,
    });

    /// <summary>结束边固定列里最靠里的一列。</summary>
    internal static Style StickyEndEdge => Css.Define(static () => new Style
    {
        BorderInlineStartWidth = BorderWidths.Xs,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderInlineStartColor = Colors.BorderMuted,
    });

    /// <summary>有起始边的固定列时，勾选列也固定在最前面。</summary>
    internal static Style StickySelect => Css.Define(static () => new Style { Position = Position.Sticky, ZIndex = 1, InsetInlineStart = 0 });

    /// <summary>分组与相邻的列之间一条竖线（只在表头里）：看得出哪几列属于一组。</summary>
    internal static Style GroupDivider => Css.Define(static () => new Style
    {
        BorderInlineStartWidth = BorderWidths.Xs,
        BorderInlineStartStyle = BorderStyle.Solid,
        BorderInlineStartColor = Colors.BorderMuted,
    });

    /// <summary>靠结束边的列（数字）：文字靠结束边，按钮仍在最后。</summary>
    internal static Style HeaderMainEnd => Css.Define(static () => new Style { TextAlign = TextAlign.End });

    /// <summary>单元格导航：聚焦的格画一圈内描边（不占位、不被相邻格盖住）。</summary>
    internal static Style GridCell => Css.Define(static () => new Style
    {
        OutlineStyle = OutlineStyle.None,
        [When.FocusVisible] = new() { BoxShadow = $"inset 0 0 0 {BorderWidths.Sm} {Colors.FocusRing}" },
    });

    /// <summary>单击就能编辑的格。</summary>
    internal static Style ClickToEdit => Css.Define(static () => new Style { Cursor = Cursor.Text });

    /// <summary>编辑中的格：编辑器贴满格子，上下内边距收小；提交被拒的原因浮在下面。</summary>
    internal static Style EditingCell => Css.Define(static () => new Style { PaddingBlock = 0, Overflow = Overflow.Visible });

    /// <summary>校验没过的格：红色内描边；聚焦时仍是焦点环。</summary>
    internal static Style InvalidCell => Css.Define(static () => new Style
    {
        BoxShadow = $"inset 0 0 0 {BorderWidths.Xs} {Tones.Danger.Solid}",
        [When.FocusVisible] = new() { BoxShadow = $"inset 0 0 0 {BorderWidths.Sm} {Colors.FocusRing}" },
    });

    /// <summary>编辑器的外层：贴着格子，错误提示以它定位。</summary>
    internal static Style Editor => Css.Define(static () => new Style { Position = Position.Relative, MinWidth = 0 });

    /// <summary>默认编辑器占满格子。</summary>
    internal static Style EditorControl => Css.Define(static () => new Style { Width = CssLength.Percent(100) });

    /// <summary>提交被拒的原因：浮在编辑器下面（不撑高这一行），按文字宽度，靠起始边。</summary>
    internal static Style EditorError => Css.Define(static () => new Style
    {
        Position = Position.Absolute,
        InsetInlineStart = 0,
        Top = CssLength.Percent(100),
        MarginTop = BorderWidths.Sm,
        ZIndex = 3,
        Width = Width.MaxContent,
        MaxWidth = CssLength.Rem(20),
        TextAlign = TextAlign.Start,
        PaddingBlock = Space.Xs,
        PaddingInline = Space.Sm,
        BorderRadius = Radii.Sm,
        BackgroundColor = Tones.Danger.Solid,
        Color = Tones.Danger.Contrast,
        FontSize = FontSizes.Sm,
        WhiteSpace = WhiteSpace.Normal,
        BoxShadow = Shadows.Md,
    });

    /// <summary>能拖动的行：让位与滑到落点的位移过渡；拿起时浮在别的行上面。</summary>
    internal static Style SortableRow => Css.Define(static () => new Style
    {
        Position = Position.Relative,
        TransitionProperty = "transform, box-shadow",
        TransitionDuration = Durations.Sm,
        TransitionTimingFunction = Easings.Standard,
        [SortableStyles.Lifted] = new() { ZIndex = 1, BoxShadow = Shadows.Md },
        [SortableStyles.Dragging] = new() { TransitionProperty = "box-shadow" },
        [SortableStyles.Settling] = new() { TransitionProperty = TransitionProperty.None },
        [Media.ReducedMotion] = new() { TransitionProperty = TransitionProperty.None },
    });

    /// <summary>第一个数据列里把手与内容的外层。</summary>
    internal static Style Leading => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs, MinWidth = 0 });

    /// <summary>第一个数据列里内容的包装：放不下时省略。</summary>
    internal static Style LeadingContent => Css.Define(static () => new Style { MinWidth = 0, Overflow = Overflow.Hidden, TextOverflow = TextOverflow.Ellipsis });

    /// <summary>行拖动的把手：在第一个数据列的最前面，按下拖动，或聚焦后用键盘。</summary>
    internal static Style RowHandle => Css.Define(static () => new Style
    {
        Display = Display.Grid,
        PlaceItems = PlaceItems.Center,
        FlexShrink = 0,
        Width = CssLength.Em(1.5),
        Height = CssLength.Em(1.5),
        MarginBlock = CssLength.Em(-0.25),
        BorderRadius = Radii.Sm,
        BackgroundColor = CssColors.Transparent,
        Color = Colors.TextMuted,
        Cursor = Cursor.Grab,
        // 触摸拖动时不滚动页面
        TouchAction = TouchAction.None,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
        [When.Active] = new() { Cursor = Cursor.Grabbing },
    });

    /// <summary>排序中的列的箭头。</summary>
    internal static Style SortActive => Css.Define(static () => new Style { Color = Tones.Accent.Text });

    // contain: inline-size：宽表格不把外层的 grid / flex 容器撑宽，放不下的部分在表格里横向滚动；也是容器查询的容器（100cqi）
    static Style Root => Css.Define(static () => new Style { MaxHeight = CssLength.Em(32), MinWidth = 0, ContainerType = ContainerType.InlineSize });

    static Style Table => Css.Define(static () => new Style
    {
        Width = CssLength.Percent(100),
        TableLayout = TableLayout.Fixed,
        BorderCollapse = BorderCollapse.Separate,
        BorderSpacing = 0,
        Color = Colors.Text,
        LineHeight = LineHeights.Normal,
    });

    static Style Head => Css.Define(static () => new Style { Position = Position.Sticky, Top = 0, ZIndex = 2 });

    // 勾选列的格（表头与各行）：控件居中，没有左右内边距
    static Style ControlCell => Css.Define(static () => new Style { PaddingInline = 0, TextAlign = TextAlign.Center, TextOverflow = TextOverflow.Clip });

    static Style HeaderContent => Css.Define(static () => new Style { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Space.Xs, MinWidth = 0 });

    static Style HeaderMain => Css.Define(static () => new Style { FlexGrow = 1, MinWidth = 0, Overflow = Overflow.Hidden, TextOverflow = TextOverflow.Ellipsis });

    // 去掉按钮默认外观、焦点环、波纹由 ButtonBase 给出
    static Style SortButton => Css.Define(static () => new Style
    {
        Display = Display.InlineFlex,
        AlignItems = AlignItems.Center,
        Gap = Space.Xs,
        MaxWidth = CssLength.Percent(100),
        Color = CssWide.Inherit,
        [When.Hover] = new() { Color = Colors.Text },
    });

    static Style Ellipsis => Css.Define(static () => new Style { Overflow = Overflow.Hidden, TextOverflow = TextOverflow.Ellipsis });

    static Style SortIcon => Css.Define(static () => new Style { FlexShrink = 0 });

    static Style SortPriority => Css.Define(static () => new Style { FontSize = CssLength.Em(0.75), FontVariantNumeric = FontVariantNumeric.TabularNums, Color = Tones.Accent.Text });

    // 单元格里 24px 的控件：上下的负外边距抵掉比一行文字高出的部分，有控件的行与纯文字的行一样高
    static Style InlineControl => Css.Define(static () => new Style { MarginBlock = CssLength.Rem(-0.1875) });

    static Style StripedRow => Css.Define(static () => new Style
    {
        BackgroundColor = Colors.Fill,
        [When.Hover] = new() { BackgroundColor = Colors.FillHover },
    });

    // 分组标题居中，下边线淡一些：与下面一行的列标题仍是一个表头
    static Style GroupHeaderCell => Css.Define(static () => new Style { TextAlign = TextAlign.Center, BorderBottomColor = Colors.BorderMuted });

    static Style DetailToggle => Css.Define(static () => new Style
    {
        Width = CssLength.Rem(1.5),
        Height = CssLength.Rem(1.5),
        MinHeight = CssLength.Rem(1.5),
        MarginBlock = CssLength.Rem(-0.1875),
    });

    // 详情：浅底、可以换行；内边距放在内容上
    static Style DetailCell => Css.Define(static () => new Style { PaddingBlock = 0, PaddingInline = 0, WhiteSpace = WhiteSpace.Normal, BackgroundColor = Colors.Fill });

    static Style DetailContent => Css.Define(static () => new Style
    {
        Position = Position.Sticky,
        InsetInlineStart = 0,
        BoxSizing = BoxSizing.BorderBox,
        MaxWidth = CssLength.Cqi(100),
        PaddingBlock = Space.Md,
        PaddingInline = Space.Md,
    });

    static Style Skeleton => Css.Define(static () => new Style { Display = Display.Block, Height = CssLength.Em(1) });

    static Style EmptyCell => Css.Define(static () => new Style { Padding = 0, WhiteSpace = WhiteSpace.Normal });

    static Style Spacer => Css.Define(static () => new Style { Padding = 0, BorderWidth = 0 });

    // 合计行吸底；在固定列之上
    static Style Foot => Css.Define(static () => new Style { Position = Position.Sticky, Bottom = 0, ZIndex = 2 });

    static Style SummaryCell => Css.Define(static () => new Style
    {
        BorderTopWidth = BorderWidths.Xs,
        BorderTopStyle = BorderStyle.Solid,
        BorderTopColor = Colors.Border,
        BorderBottomWidth = 0,
        BackgroundColor = Colors.Fill,
        FontWeight = FontWeights.Semibold,
    });

    static Style InfoRow => Css.Define(static () => new Style { BackgroundColor = Tones.Info.Soft, [When.Hover] = new() { BackgroundColor = Tones.Info.SoftHover } });

    static Style SuccessRow => Css.Define(static () => new Style { BackgroundColor = Tones.Success.Soft, [When.Hover] = new() { BackgroundColor = Tones.Success.SoftHover } });

    static Style WarningRow => Css.Define(static () => new Style { BackgroundColor = Tones.Warning.Soft, [When.Hover] = new() { BackgroundColor = Tones.Warning.SoftHover } });

    static Style DangerRow => Css.Define(static () => new Style { BackgroundColor = Tones.Danger.Soft, [When.Hover] = new() { BackgroundColor = Tones.Danger.SoftHover } });
}
