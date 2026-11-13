using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rune.Styles;

namespace Rune.UI;

/// <summary>数据表格：按列渲染一组行，始终虚拟滚动（用法见 DataTable.razor 顶部）。</summary>
/// <typeparam name="TItem">一行的类型。</typeparam>
public partial class DataTable<TItem>
{
    // 勾选列、详情列（展开按钮）的宽度（px）
    const double SelectWidth = 44;
    const double DetailWidth = 40;
    // 详情行的估计高度（px），渲染后以实际高度为准
    const double DetailHeight = 160;
    // 加载中铺几行骨架；滚动加载（已有数据）时接在末尾几行
    const int SkeletonRowCount = 10;
    const int MoreSkeletonRows = 3;

    static readonly Dictionary<TableDensity, double> RowHeights = new()
    {
        [TableDensity.Compact] = 36,
        [TableDensity.Standard] = 44,
        [TableDensity.Comfortable] = 52,
    };

    readonly DataTableColumns<TItem> columns = new();
    readonly string radioName = UniqueId.Next("data-table-select");

    Bindable<ColumnSort?> sort { get => field ??= new(this, () => Sort, () => SortChanged); }
    Bindable<IReadOnlyList<ColumnSort>> sorts { get => field ??= new(this, () => Sorts ?? [], () => SortsChanged); }
    Bindable<IReadOnlyList<string>> selected { get => field ??= new(this, () => Selected ?? [], () => SelectedChanged); }
    Bindable<IReadOnlyDictionary<string, IReadOnlyList<string>>> filters { get => field ??= new(this, () => Filters ?? NoFilters, () => FiltersChanged); }
    Bindable<IReadOnlyList<string>> hiddenColumns { get => field ??= new(this, () => HiddenColumns ?? [], () => HiddenColumnsChanged); }
    Bindable<IReadOnlyList<string>> columnOrder { get => field ??= new(this, () => ColumnOrder ?? [], () => ColumnOrderChanged); }
    Bindable<IReadOnlyDictionary<string, ColumnSticky?>> columnPins { get => field ??= new(this, () => ColumnPins ?? NoPins, () => ColumnPinsChanged); }
    Bindable<IReadOnlyList<string>> expandedRows { get => field ??= new(this, () => ExpandedRows ?? [], () => ExpandedRowsChanged); }
    Bindable<IReadOnlyDictionary<string, double>> columnWidths { get => field ??= new(this, () => ColumnWidths ?? NoWidths, () => ColumnWidthsChanged); }

    static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> NoFilters = new Dictionary<string, IReadOnlyList<string>>();
    static readonly IReadOnlyDictionary<string, ColumnSticky?> NoPins = new Dictionary<string, ColumnSticky?>();
    static readonly IReadOnlyDictionary<string, double> NoWidths = new Dictionary<string, double>();

    Memo<(IReadOnlyList<TItem>, IReadOnlyList<ColumnSort>, IReadOnlyList<DataTableColumn<TItem>>, IReadOnlyDictionary<string, IReadOnlyList<string>>, bool, bool), IReadOnlyList<TItem>> rows;
    Memo<(IReadOnlyList<TItem>, IReadOnlyList<DataTableColumn<TItem>>), Dictionary<string, IReadOnlyList<ListboxOption>>> filterOptions;
    Memo<ColumnSort?, IReadOnlyList<ColumnSort>> singleSort;
    Memo<(IReadOnlyList<DataTableColumn<TItem>>, IReadOnlyList<TItem>, IReadOnlyList<string>, IReadOnlyDictionary<string, ColumnSticky?>, IReadOnlyDictionary<string, double>, IReadOnlyList<string>, bool), IReadOnlyList<ColumnView<TItem>>> shown;
    Memo<IReadOnlyList<string>, HashSet<string>> selectedSet;
    Memo<(IReadOnlyList<TItem>, IReadOnlyList<string>, bool), BodyEntry[]> entries;
    Memo<string?, SelectionRadios> radios;

    // 编辑中的格：行与列的 key、草稿、校验没过的原因、正在保存
    (string Row, string Column, object? Draft, string? Error, bool Saving)? editing;
    // 编辑完、放弃后让 JS 把焦点放回格上：{序列号}:{行}␟{列}
    (string Row, string Column, int Sequence) focusRequest;

    // JS 报告的要渲染的那一段；还没报告时是开头的 InitialCount 行
    (int From, int To)? reported;
    // 列虚拟化：JS 报告的视口横向位置与宽度；还没报告时按 1280px 宽算
    (double Offset, double Width) columnWindow = (0, ColumnWindow.InitialViewport);
    VisibleRange? visible;
    (int Index, ScrollAlign Align, int Sequence) scrollRequest;

    DelayedLoading? delayedLoading;
    DelayedLoading indicator => delayedLoading ??= new(() => InvokeAsync(StateHasChanged));

    /// <summary>数据。</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>行的 key：稳定且唯一（选择、测量、滚动到某一行都按它）。</summary>
    [Parameter, EditorRequired] public Func<TItem, string> GetKey { get; set; } = null!;

    /// <summary>列：PropertyColumn、StatusColumn、TemplateColumn。</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>当前排序（单列）：点可排序的列标题在 正序 → 倒序 → 不排 之间切换；null 为不排序。</summary>
    [Parameter] public ColumnSort? Sort { get; set; }

    /// <summary>排序变了。</summary>
    [Parameter] public EventCallback<ColumnSort?> SortChanged { get; set; }

    /// <summary>多列排序：排序写在 <see cref="Sorts"/>，按住 Shift 点列标题是追加一列（再点切换方向、倒序时去掉）。</summary>
    [Parameter] public bool MultiSort { get; set; }

    /// <summary>多列排序时的排序，前面的优先。</summary>
    [Parameter] public IReadOnlyList<ColumnSort>? Sorts { get; set; }

    /// <summary>多列排序变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<ColumnSort>> SortsChanged { get; set; }

    /// <summary>不在本地排序：只写回排序，数据由调用方按它取（服务端分页，绑 TableQueryState 的 Sort / Sorts）。</summary>
    [Parameter] public bool ManualSort { get; set; }

    /// <summary>表头筛选：各列（写了 Filter 的列）选中的值，关键字筛选时是 [关键字]。</summary>
    [Parameter] public IReadOnlyDictionary<string, IReadOnlyList<string>>? Filters { get; set; }

    /// <summary>表头筛选变了。</summary>
    [Parameter] public EventCallback<IReadOnlyDictionary<string, IReadOnlyList<string>>> FiltersChanged { get; set; }

    /// <summary>不在本地筛选：只写回筛选，数据由调用方按它取（绑 TableQueryState 的 ColumnFilters）。</summary>
    [Parameter] public bool ManualFilter { get; set; }

    /// <summary>列菜单：列标题结尾多一个 ⋮，排序、固定在开头 / 结尾、前后移动、隐藏，写回 ColumnPins、ColumnOrder、HiddenColumns。</summary>
    [Parameter] public bool ColumnMenu { get; set; }

    /// <summary>隐藏的列的 key。</summary>
    [Parameter] public IReadOnlyList<string>? HiddenColumns { get; set; }

    /// <summary>隐藏的列变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> HiddenColumnsChanged { get; set; }

    /// <summary>列的顺序（列的 key）：没列到的按定义的顺序排在后面。</summary>
    [Parameter] public IReadOnlyList<string>? ColumnOrder { get; set; }

    /// <summary>列的顺序变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> ColumnOrderChanged { get; set; }

    /// <summary>固定的列：key → 起始边 / 结束边（null 取消列定义里的固定）。</summary>
    [Parameter] public IReadOnlyDictionary<string, ColumnSticky?>? ColumnPins { get; set; }

    /// <summary>固定的列变了。</summary>
    [Parameter] public EventCallback<IReadOnlyDictionary<string, ColumnSticky?>> ColumnPinsChanged { get; set; }

    /// <summary>列宽可以拖动或用键盘调整（列的 Resizable 优先）。</summary>
    [Parameter] public bool Resizable { get; set; }

    /// <summary>各列调整后的宽度（px）：可以保存到本地设置里。</summary>
    [Parameter] public IReadOnlyDictionary<string, double>? ColumnWidths { get; set; }

    /// <summary>列宽变了。</summary>
    [Parameter] public EventCallback<IReadOnlyDictionary<string, double>> ColumnWidthsChanged { get; set; }

    /// <summary>选择方式；不写时没有勾选列。多选时表头有全选。</summary>
    [Parameter] public SelectionMode? SelectionMode { get; set; }

    /// <summary>已选行的 key。</summary>
    [Parameter] public IReadOnlyList<string>? Selected { get; set; }

    /// <summary>选择变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> SelectedChanged { get; set; }

    /// <summary>这一行能不能勾选：不能的勾选框不可用，全选也跳过它。</summary>
    [Parameter] public Func<TItem, bool>? IsRowSelectable { get; set; }

    /// <summary>加载中：首次加载、刷新时表体换成骨架；滚动加载（有 OnRangeChange 且已有数据）时骨架接在末尾。表格带 aria-busy。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>没有数据时显示的内容；默认是 Empty（文案取语言包，或 <see cref="EmptyText"/>）。</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>没有数据时默认空状态的文字；默认语言包的 <see cref="Lang.TableEmpty"/>。</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>密度；不写时取配置的默认值（<see cref="DataTableDefaults"/>），再没有就是标准。</summary>
    [Parameter] public TableDensity? Density { get; set; }

    /// <summary>样式；不写时取配置的默认值，再没有就是表面色加外框。</summary>
    [Parameter] public TableVariant? Variant { get; set; }

    /// <summary>斑马纹：隔行浅色底。</summary>
    [Parameter] public bool? Striped { get; set; }

    /// <summary>行的估计高度（px）；不写时按密度估计，渲染后以实际高度为准。</summary>
    [Parameter] public float? EstimateRowHeight { get; set; }

    /// <summary>静态渲染（没有交互时）与交互接管前先输出前多少行，默认 30。</summary>
    [Parameter] public int InitialCount { get; set; } = 30;

    /// <summary>
    /// 列虚拟化（同 vue-tsx-pro 的 virtualColumns）：几十上百列的宽表、透视结果只渲染看得见的列（前后各多三列）。
    /// 没写宽度的列按 150px；两端的固定列总在。交互模式下才生效。
    /// </summary>
    [Parameter] public bool VirtualColumns { get; set; }

    /// <summary>可见行范围变化（不含 overscan）：接近末尾时加载更多。</summary>
    [Parameter] public EventCallback<VisibleRange> OnRangeChange { get; set; }

    /// <summary>第一个数据列的内容前面加的东西（TreeDataTable 的缩进与展开按钮）。</summary>
    [Parameter] public RenderFragment<TItem>? Leading { get; set; }

    /// <summary>部分勾选的行的 key（树形表格的父行：子孙只勾了一部分）：勾选框显示横线，读屏念“部分选中”。</summary>
    [Parameter] public IReadOnlyList<string>? PartialKeys { get; set; }

    /// <summary>
    /// 行拖动：给了它时第一个数据列前面有把手，拖动（或聚焦后空格拿起、↑↓ 移动）改变行的顺序，拿到新的顺序（全部行）。
    /// 排序、筛选时显示的顺序不是数据的顺序，这时不能拖。
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> OnReorder { get; set; }

    /// <summary>行的详情：写了时多一列展开按钮，展开的行下面多一行跨全部列的详情（虚拟滚动按真实高度测量）。</summary>
    [Parameter] public RenderFragment<TItem>? DetailContent { get; set; }

    /// <summary>这一行有没有详情：没有的不显示展开按钮；默认都有。</summary>
    [Parameter] public Func<TItem, bool>? HasDetail { get; set; }

    /// <summary>展开了详情的行的 key。</summary>
    [Parameter] public IReadOnlyList<string>? ExpandedRows { get; set; }

    /// <summary>展开的行变了。</summary>
    [Parameter] public EventCallback<IReadOnlyList<string>> ExpandedRowsChanged { get; set; }

    /// <summary>
    /// 编辑了一格（列写了 Editable）：写回数据；不接受时（接口拒绝）调用 <see cref="CellChange{TItem}.Reject"/>，编辑器留着并显示原因。
    /// </summary>
    [Parameter] public EventCallback<CellChange<TItem>> OnCellChange { get; set; }

    /// <summary>行校验：返回各列的错误（列的 key → 原因），标在格上（红框，悬停与读屏都能知道原因）；如“结束日期早于开始日期”。</summary>
    [Parameter] public Func<TItem, IReadOnlyDictionary<string, string>?>? ValidateRow { get; set; }

    /// <summary>鼠标怎样开始编辑一格，默认双击；键盘的 Enter、F2、直接打字都能开始。</summary>
    [Parameter] public EditTrigger EditTrigger { get; set; }

    /// <summary>单元格导航：表格是 role="grid"，方向键在格之间走，只有一格能 Tab 进入；有可编辑的列时默认开。</summary>
    [Parameter] public bool? CellNavigation { get; set; }

    /// <summary>按行与列（key）计算的每个单元格的部件属性：节省是负数时标红这类（写了时代替 <see cref="PropsCell"/>）。</summary>
    [Parameter] public Func<TItem, string, Attrs?>? PropsCellFor { get; set; }

    /// <summary>行的色调：导入校验的错误行、即将超期的行整行着浅色底（不透明，固定列也盖得住）。</summary>
    [Parameter] public Func<TItem, Tone?>? RowTone { get; set; }

    /// <summary>主从视图里正在看的那一行的 key：整行高亮、起始边一条竖线，带 aria-current。</summary>
    [Parameter] public string? ActiveKey { get; set; }

    /// <summary>
    /// 点了一行（点在按钮、链接、输入框上的除外）：主从视图里打开这一行的详情。
    /// 只是鼠标的捷径：行里还要有键盘能到的入口（如名称做成按钮或链接）。
    /// </summary>
    [Parameter] public EventCallback<TItem> OnRowClick { get; set; }

    /// <summary>合计行第一列的文字（第一列自己没写合计时）；默认语言包的 <see cref="Lang.TableSummary"/>。</summary>
    [Parameter] public string? SummaryLabel { get; set; }

    /// <summary>
    /// 放在竖排的弹性容器（ViewportFill、Flex Direction="column"）里：按行数自己的高度，最多占到剩下的高度，
    /// 再多在表格里滚动；分页紧跟在表格下面。不写时最高 32em。
    /// </summary>
    [Parameter] public bool Flexible { get; set; }

    /// <summary>外层滚动区域（决定表格的高度）的部件属性；高度用 Sx 给，默认最高 32em。</summary>
    [Parameter] public Attrs? PropsRoot { get; set; }

    /// <summary>每一行（tr）的部件属性。</summary>
    [Parameter] public Attrs? PropsRow { get; set; }

    /// <summary>按行计算的每一行的部件属性（数据、序号；写了时代替 <see cref="PropsRow"/>）。</summary>
    [Parameter] public Func<TItem, int, Attrs?>? PropsRowFor { get; set; }

    /// <summary>空状态的部件属性（交给 Empty）。</summary>
    [Parameter] public Attrs? PropsEmpty { get; set; }

    /// <summary>加载中占位的部件属性（交给 Skeleton）。</summary>
    [Parameter] public Attrs? PropsSkeleton { get; set; }

    /// <summary>表头（thead）的部件属性。</summary>
    [Parameter] public Attrs? PropsHead { get; set; }

    /// <summary>表头每一行（tr）的部件属性。</summary>
    [Parameter] public Attrs? PropsHeadRow { get; set; }

    /// <summary>列标题（th）的部件属性。</summary>
    [Parameter] public Attrs? PropsHeaderCell { get; set; }

    /// <summary>分组标题（th）的部件属性。</summary>
    [Parameter] public Attrs? PropsGroupHeaderCell { get; set; }

    /// <summary>勾选列的列标题的部件属性。</summary>
    [Parameter] public Attrs? PropsSelectHeaderCell { get; set; }

    /// <summary>全选勾选框的部件属性（交给 Checkbox）。</summary>
    [Parameter] public Attrs? PropsSelectAll { get; set; }

    /// <summary>详情列的列标题的部件属性。</summary>
    [Parameter] public Attrs? PropsDetailHeaderCell { get; set; }

    /// <summary>排序按钮的部件属性（交给 ButtonBase）。</summary>
    [Parameter] public Attrs? PropsSortButton { get; set; }

    /// <summary>表头筛选按钮（漏斗）的部件属性（交给 IconButton）。</summary>
    [Parameter] public Attrs? PropsFilterButton { get; set; }

    /// <summary>列菜单按钮（⋮）的部件属性（交给 IconButton）。</summary>
    [Parameter] public Attrs? PropsColumnMenuButton { get; set; }

    /// <summary>排序按钮里文字的部件属性。</summary>
    [Parameter] public Attrs? PropsSortLabel { get; set; }

    /// <summary>调整列宽手柄的部件属性（交给 ColumnResizer）。</summary>
    [Parameter] public Attrs? PropsResizer { get; set; }

    /// <summary>数据单元格（td）的部件属性；按行与列计算的用 <see cref="PropsCellFor"/>。</summary>
    [Parameter] public Attrs? PropsCell { get; set; }

    /// <summary>勾选列的格的部件属性。</summary>
    [Parameter] public Attrs? PropsSelectCell { get; set; }

    /// <summary>每行勾选框的部件属性（多选，交给 Checkbox）。</summary>
    [Parameter] public Attrs? PropsSelectCheckbox { get; set; }

    /// <summary>每行单选框的部件属性（单选，交给 Radio）。</summary>
    [Parameter] public Attrs? PropsSelectRadio { get; set; }

    /// <summary>展开详情那一列的格的部件属性。</summary>
    [Parameter] public Attrs? PropsDetailToggleCell { get; set; }

    /// <summary>展开详情按钮的部件属性（交给 IconButton）。</summary>
    [Parameter] public Attrs? PropsDetailToggle { get; set; }

    /// <summary>第一个数据列里把手、Leading 与内容那一行的部件属性。</summary>
    [Parameter] public Attrs? PropsLeading { get; set; }

    /// <summary>上面一行里格的内容的部件属性。</summary>
    [Parameter] public Attrs? PropsContent { get; set; }

    /// <summary>详情行（tr）的部件属性。</summary>
    [Parameter] public Attrs? PropsDetailRow { get; set; }

    /// <summary>详情所在的格的部件属性。</summary>
    [Parameter] public Attrs? PropsDetailCell { get; set; }

    /// <summary>空状态所在的格的部件属性。</summary>
    [Parameter] public Attrs? PropsEmptyCell { get; set; }

    /// <summary>表尾（tfoot）的部件属性。</summary>
    [Parameter] public Attrs? PropsFoot { get; set; }

    /// <summary>合计行的格的部件属性。</summary>
    [Parameter] public Attrs? PropsSummaryCell { get; set; }

    /// <summary>当前的可见范围（JS 报告过之后才有）。</summary>
    public VisibleRange? Visible => visible;

    /// <summary>
    /// 滚动到 key 这一行：还没渲染（虚拟滚动）的行也行，停在吸顶表头之下、吸底合计行之上；align 默认已经完整可见就不动。
    /// 找不到这一行时返回 false。
    /// </summary>
    public bool ScrollToRow(string key, ScrollAlign align = ScrollAlign.Auto)
    {
        var shown = Rows;
        var index = -1;
        for (var at = 0; at < shown.Count; at++)
        {
            if (GetKey(shown[at]) != key) continue;
            index = at;
            break;
        }
        if (index < 0) return false;
        // 有展开的详情时，序号换成在表体里的位置
        if (Entries is { } list) index = list.ToList().FindIndex(entry => !entry.Detail && entry.Index == index);
        scrollRequest = (index, align, scrollRequest.Sequence + 1);
        StateHasChanged();
        return true;
    }

    /// <summary>
    /// 把表格里看到的行写成 CSV（RFC 4180）：本地筛选、排序后的行，显示中的、有值的列；表头是列的标题，单元格是看到的文字
    /// （状态写名字，数字、金额、日期按列的类型与格式），开头带 BOM，Excel 打开中文不乱码。见 <see cref="CsvOptions"/>。
    /// </summary>
    public string ToCsv(CsvOptions? options = null)
    {
        options ??= new();
        var all = ColumnDisplay.Arrange(columns.All, Rows, columnOrder.Value, columnPins.Value, columnWidths.Value, []);
        var chosen = options.Columns is { } keys
            ? [.. keys.Select(key => all.FirstOrDefault(view => view.Key == key)).OfType<ColumnView<TItem>>()]
            : Shown.Where(static view => view.Column is not TemplateColumn<TItem> { Value: null }).ToList();
        return TableCsv.Write(Rows, chosen, Config.Lang, options);
    }

    DataTableLook Look
    {
        get
        {
            var defaults = Config.Defaults.Get<DataTableDefaults>();
            return new(Density ?? defaults?.Density ?? TableDensity.Standard, Variant ?? defaults?.Variant ?? TableVariant.Surface);
        }
    }

    bool IsStriped => Striped ?? Config.Defaults.Get<DataTableDefaults>()?.Striped ?? false;

    // 同一个排序是同一个列表：Rows 按它缓存，换成新列表就会每次渲染都重新排一遍
    IReadOnlyList<ColumnSort> ActiveSorts => MultiSort
        ? sorts.Value
        : singleSort.Get(sort.Value, static single => single is { } value ? [value] : []);

    /// <summary>要显示的行：本地筛选、排序后的（ManualFilter、ManualSort 时原样）。</summary>
    IReadOnlyList<TItem> Rows => rows.Get((Items, ActiveSorts, columns.All, filters.Value, ManualFilter, ManualSort), Config,
        static (k, config) =>
        {
            var kept = k.Item5 ? k.Item1 : ColumnDisplay.Filter(k.Item1, k.Item3, k.Item4, config.Matches);
            return k.Item6 ? kept : ColumnDisplay.Sort(kept, k.Item3, k.Item2, config.Lang.Code);
        });

    /// <summary>显示中的列：按列菜单排好、去掉隐藏的，带上调整后的宽度与推断的值类型。</summary>
    IReadOnlyList<ColumnView<TItem>> Shown => shown.Get((columns.All, Rows, columnOrder.Value, columnPins.Value, columnWidths.Value, hiddenColumns.Value, VirtualColumns),
        static k => k.Item7
            // 列虚拟化：宽度要确定，才算得出哪些列看得见
            ? [.. ColumnDisplay.Arrange(k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, k.Item6).Select(static view => view.Width is null ? view with { Width = ColumnWindow.DefaultWidth } : view)]
            : ColumnDisplay.Arrange(k.Item1, k.Item2, k.Item3, k.Item4, k.Item5, k.Item6));

    bool ColumnsVirtual => VirtualColumns && Virtual;

    /// <summary>要跳过的列段（列虚拟化时）；不虚拟化时为空。</summary>
    IReadOnlyList<ColumnSkip> SkipsOf(IReadOnlyList<ColumnView<TItem>> shown) =>
        ColumnsVirtual ? ColumnWindow.Skips(shown, columnWindow.Offset, columnWindow.Width, LeadingWidth) : [];

    /// <summary>数据列前面的几列（勾选列、详情列）。</summary>
    int LeadingCount => (SelectionMode is null ? 0 : 1) + (DetailContent is null ? 0 : 1);

    /// <summary>列虚拟化时各格的 aria-colindex（第 column 个数据列）。</summary>
    int? ColIndex(int column) => ColumnsVirtual ? LeadingCount + column + 1 : null;

    /// <summary>表头筛选的选项：列出出现过的值时看全部行（不是筛过的）；行与列不变时不重算。</summary>
    IReadOnlyList<ListboxOption> FilterOptionsOf(DataTableColumn<TItem> column) =>
        column.FilterOptions ?? filterOptions.Get((Items, columns.All), Config.Lang.Code, static (k, culture) =>
            k.Item2.Where(static c => c.Filter == ColumnFilterKind.Values && c.FilterOptions is null)
                .ToDictionary(static c => c.ColumnKey, c => c.FilterOptionsFrom(k.Item1, culture))).GetValueOrDefault(column.ColumnKey, []);

    HashSet<string> SelectedKeys => selectedSet.Get(selected.Value, static keys => [.. keys]);

    // 交互模式下才虚拟滚动（JS 排位置）；静态 SSR、预渲染时在普通流里输出开头几行
    bool Virtual => RendererInfo.IsInteractive;

    /// <summary>表体里的一行：数据行，或某一行展开的详情。</summary>
    /// <param name="Index">数据行在 Rows 里的序号。</param>
    /// <param name="Detail">是这一行的详情。</param>
    readonly record struct BodyEntry(int Index, bool Detail);

    /// <summary>表体里的各行：展开了详情的行后面跟它的详情；没有展开的详情时为 null（直接用 Rows）。</summary>
    IReadOnlyList<BodyEntry>? Entries => entries.Get((Rows, expandedRows.Value, DetailContent is not null), this, static (k, owner) =>
    {
        if (!k.Item3 || k.Item2.Count == 0) return [];
        var expanded = k.Item2.ToHashSet();
        var list = new List<BodyEntry>();
        for (var index = 0; index < k.Item1.Count; index++)
        {
            var item = k.Item1[index];
            list.Add(new(index, false));
            // 展开记录里可能留着已经没有详情的行（数据变了）：只给还有详情的行出详情
            if (expanded.Contains(owner.GetKey(item)) && (owner.HasDetail?.Invoke(item) ?? true)) list.Add(new(index, true));
        }
        return [.. list];
    }) is { Length: > 0 } list ? list : null;

    int BodyCount => Entries?.Count ?? Rows.Count;

    BodyEntry EntryAt(int position) => Entries is { } list ? list[position] : new(position, false);

    // 同 SortClick：每行一个不变的处理函数；数据换了时重新建
    readonly Dictionary<string, Func<MouseEventArgs, Task>> detailClicks = [];
    IReadOnlyList<TItem>? detailClicksFor;

    Func<MouseEventArgs, Task> DetailClick(string key)
    {
        if (!ReferenceEquals(detailClicksFor, Items))
        {
            detailClicks.Clear();
            detailClicksFor = Items;
        }
        return detailClicks.TryGetValue(key, out var click) ? click : detailClicks[key] = _ => ToggleDetailAsync(key);
    }

    Task ToggleDetailAsync(string key) =>
        expandedRows.SetAsync(expandedRows.Value.Contains(key) ? [.. expandedRows.Value.Where(item => item != key)] : [.. expandedRows.Value, key]);

    /// <summary>行的名称（展开按钮用）：第一列的文字，没有时用行的 key。</summary>
    string RowLabel(TItem item, IReadOnlyList<ColumnView<TItem>> shown) =>
        shown.Count > 0 && shown[0].Column.ValueOf(item) is { } value && DataTableColumn<TItem>.FilterText(value) is { Length: > 0 } text ? text : GetKey(item);

    (int From, int To) RenderedRange
    {
        get
        {
            var last = BodyCount - 1;
            if (Virtual && reported is { } range) return (Math.Min(range.From, last), Math.Min(range.To, last));
            return (0, Math.Min(InitialCount, BodyCount) - 1);
        }
    }

    // 骨架换掉表体（首次加载、刷新、翻页），还是接在末尾（滚动加载：换掉会丢了滚动位置）
    bool Replacing => indicator.Shown && !(OnRangeChange.HasDelegate && Rows.Count > 0);

    bool Appending => indicator.Shown && OnRangeChange.HasDelegate && Rows.Count > 0;

    double LeadingWidth => (SelectionMode is null ? 0 : SelectWidth) + (DetailContent is null ? 0 : DetailWidth);


    // 滚动区域的视口：交给 JS 的选项、滚动请求与 range 命令
    Attrs ViewportManaged => new()
    {
        ["data-rune-virtual"] = Virtual
            ? FormattableString.Invariant($$"""{"count":{{BodyCount}},"estimate":{{EstimateRowHeight ?? RowHeights[Look.Density]}},"overscan":8,"table":true,"columns":{{(VirtualColumns ? "true" : "false")}}}""")
            : null,
        ["data-rune-virtual-scroll"] = scrollRequest.Sequence == 0
            ? null
            : FormattableString.Invariant($"index:{scrollRequest.Index}:{scrollRequest.Align.ToString().ToLowerInvariant()}:{scrollRequest.Sequence}"),
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(this, OnViewportCommandAsync),
    };

    /// <summary>行能拖动：给了 OnReorder，而且没有排序、筛选（显示的顺序就是数据的顺序）。</summary>
    bool Reorderable => OnReorder.HasDelegate && ActiveSorts.Count == 0 && !filters.Value.Any(static pair => pair.Value.Count > 0);

    readonly string sortableInstructionsId = UniqueId.Next("data-table-sortable");

    // JS 交来渲染出来的那几行的新顺序（虚拟滚动只渲染一段）：在全部行里换掉那一段；对不上（数据在这期间变了）时不动
    Task OnReorderAsync(ReorderEventArgs e)
    {
        var items = Rows;
        var keys = items.Select(GetKey).ToList();
        var positions = e.Values.Select(key => keys.IndexOf(key)).Order().ToList();
        if (positions.Count == 0 || positions[0] < 0 || positions[^1] - positions[0] != positions.Count - 1) return Task.CompletedTask;
        var byKey = items.ToDictionary(GetKey);
        var next = items.ToList();
        for (var at = 0; at < e.Values.Length; at++) next[positions[0] + at] = byKey[e.Values[at]];
        return OnReorder.InvokeAsync(next);
    }

    bool Navigable => CellNavigation ?? columns.All.Any(static column => column.Edits);

    // table：名称、行数、忙碌；点行、网格的方向键与编辑的按键由 js/table/table.js 交给 C#（点在控件上的不算）
    Attrs TableManaged(int rowCount, IReadOnlyList<ColumnView<TItem>> shown) => new()
    {
        Role = Navigable ? "grid" : null,
        ["data-cell-navigation"] = Navigable ? "" : null,
        ["data-edit-trigger"] = Navigable ? (EditTrigger == EditTrigger.Click ? "click" : "dblclick") : null,
        ["data-focus-cell"] = focusRequest.Sequence == 0 ? null : FormattableString.Invariant($"{focusRequest.Sequence}:{focusRequest.Row}\u001f{focusRequest.Column}"),
        InlineStyle = $"min-width:{ColumnDisplay.Px(ColumnDisplay.MinWidth(shown, LeadingWidth))}",
        ["aria-rowcount"] = rowCount,
        ["aria-colcount"] = ColumnsVirtual ? LeadingCount + shown.Count : null,
        ["aria-busy"] = Loading ? "true" : null,
        ["data-rune-table"] = "",
        ["data-row-click"] = OnRowClick.HasDelegate ? "" : null,
        ["onrunecommand"] = EventCallback.Factory.Create<CommandEventArgs>(this, OnTableCommandAsync),
    };

    /// <inheritdoc/>
    protected override void OnParametersSet() => indicator.Update(Loading);

    /// <inheritdoc/>
    protected override void OnDispose() => delayedLoading?.Dispose();

    [CascadingParameter] IDataTableColumnsListener<TItem>? ColumnsListener { get; set; }

    /// <summary>各列收集完之后：列变了时（第一次渲染、增减列）再渲染一次，各行按新的列排序、显示；包着它的 TreeDataTable 也要知道。</summary>
    void FinishCollectingColumns()
    {
        if (!columns.Finish()) return;
        ColumnsListener?.ColumnsCollected(columns.All);
        StateHasChanged();
    }

    // JS 报告要渲染的那一段与可见范围：{第一行}:{最后一行}:{第一个可见}:{最后一个可见}
    async Task OnViewportCommandAsync(CommandEventArgs e)
    {
        if (e.Name == "columns" && e.Text?.Split(' ') is [var offset, var width])
        {
            columnWindow = (double.Parse(offset, CultureInfo.InvariantCulture), double.Parse(width, CultureInfo.InvariantCulture));
            return;
        }
        if (e.Name != "range" || e.Text?.Split(':') is not [var from, var to, var start, var end]) return;
        reported = (Parse(from), Parse(to));
        var range = new VisibleRange(Parse(start), Parse(end));
        if (visible == range) return;
        visible = range;
        await OnRangeChange.InvokeAsync(range);

        static int Parse(string text) => int.Parse(text, CultureInfo.InvariantCulture);
    }

    // js/table/table.js 的命令：row-click（行的 key）；edit（行␟列，直接打字时再加 ␟打的字）、commit、commit-next、commit-prev、cancel
    async Task OnTableCommandAsync(CommandEventArgs e)
    {
        if (e.Text is not { } text) return;
        switch (e.Name)
        {
            case "row-click":
                if (Rows.FirstOrDefault(item => GetKey(item) == text) is { } row) await OnRowClick.InvokeAsync(row);
                return;
            case "edit":
                var parts = text.Split('\u001f');
                if (parts.Length >= 2) StartEdit(parts[0], parts[1], parts.Length > 2 ? parts[2] : null);
                return;
            case "commit":
                await CommitEditAsync(0);
                return;
            case "commit-next":
                await CommitEditAsync(1);
                return;
            case "commit-prev":
                await CommitEditAsync(-1);
                return;
            case "cancel":
                CancelEdit();
                return;
        }
    }

    /// <summary>合计行里一列的内容。</summary>
    RenderFragment? SummaryOf(DataTableColumn<TItem> column, int position, ColumnType type) =>
        ColumnDisplay.Summary(column, position, type, Rows, SummaryLabel, Config.Lang);

    RenderFragment CellOf(DataTableColumn<TItem> column, TItem item, ColumnType type) => ColumnDisplay.Cell(column, item, type, Config.Lang);

    // 固定列：position: sticky 加上按列宽累加的偏移；挨着滚动列的那一列加一条竖线
    static Attrs? StickyLayer(ColumnView<TItem> column, StickyOffsets offsets) => column.Sticky switch
    {
        ColumnSticky.Start => new()
        {
            Sx = Sx.Of(DataTableStyles.Sticky, offsets.StartEdge == column.Key ? DataTableStyles.StickyStartEdge : null),
            InlineStyle = $"inset-inline-start:{ColumnDisplay.Px(offsets.Start[column.Key])}",
        },
        ColumnSticky.End => new()
        {
            Sx = Sx.Of(DataTableStyles.Sticky, offsets.EndEdge == column.Key ? DataTableStyles.StickyEndEdge : null),
            InlineStyle = $"inset-inline-end:{ColumnDisplay.Px(offsets.End[column.Key])}",
        },
        _ => null,
    };
}
