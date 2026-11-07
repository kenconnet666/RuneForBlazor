using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Rune.UI;

/// <summary>列的值类型：不写单元格内容时按它格式化（跟随语言），数字类默认靠结束边对齐（同 vue-tsx-pro 的 ColumnTypes）。</summary>
public enum ColumnType
{
    /// <summary>文字：原样显示。</summary>
    Text,

    /// <summary>数字：按语言分组，1234.5 → 1,234.5。</summary>
    Number,

    /// <summary>金额，语言的币种：19.9 → ¥19.90。</summary>
    Currency,

    /// <summary>百分比：0.125 → 12.5%。</summary>
    Percent,

    /// <summary>文件大小（字节）：1234567 → 1.2 MB。</summary>
    FileSize,

    /// <summary>日期。</summary>
    Date,

    /// <summary>日期与时间。</summary>
    DateTime,
}

/// <summary>固定在哪一侧（横向滚动时不动）。</summary>
public enum ColumnSticky
{
    /// <summary>起始边：名称、编号。</summary>
    Start,

    /// <summary>结束边：操作。</summary>
    End,
}

/// <summary>
/// DataTable 的一列（同 vue-tsx-pro 的 DataTableColumn）：写在 DataTable 里面，渲染时把自己交给表格（同 QuickGrid 的列）。
/// 取值的列用 <see cref="PropertyColumn{TItem, TValue}"/>，自己写内容的用 <see cref="TemplateColumn{TItem}"/>。
/// </summary>
/// <typeparam name="TItem">一行的类型。</typeparam>
public abstract class DataTableColumn<TItem> : ComponentBase
{
    [CascadingParameter] internal DataTableColumns<TItem>? Columns { get; set; }

    /// <summary>列的标识：同一表格里不能重复；排序、表头筛选、列宽、隐藏都按它，服务端排序时就是排序参数里的名字。</summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>表头的文字。</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>表头的内容（写了时代替 <see cref="Title"/> 显示；Title 仍是列的名称，给读屏与列菜单）。</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>值的类型：决定默认的格式与对齐；不写时按第一个非空值推断（数字 → Number，日期 → Date，其余 Text）。</summary>
    [Parameter] public ColumnType? Type { get; set; }

    /// <summary>数字类（Number、Currency、Percent）的格式，盖过类型的默认格式：万元写 <c>new() { Scale = 10_000, MaximumFractionDigits = 2 }</c>。</summary>
    [Parameter] public NumberFormat? Format { get; set; }

    /// <summary>宽度（px）；不写时与其他没写宽度的列平分剩余宽度。固定列一定要写（偏移量按它算）。</summary>
    [Parameter] public double? Width { get; set; }

    /// <summary>最小宽度（px）：没写宽度的列按它算表格的最小宽度，放不下时横向滚动而不是继续压窄；默认 120。</summary>
    [Parameter] public double? MinWidth { get; set; }

    /// <summary>对齐；默认数字类（Number、Currency、Percent、FileSize）靠结束边，其余起始边。</summary>
    [Parameter] public TableAlign? Align { get; set; }

    /// <summary>可以点表头排序。</summary>
    [Parameter] public bool Sortable { get; set; }

    /// <summary>固定在哪一侧。</summary>
    [Parameter] public ColumnSticky? Sticky { get; set; }

    /// <summary>
    /// 表头筛选：列标题上多一个筛选按钮。<see cref="ColumnFilterKind.Values"/> 勾选值（不写 <see cref="FilterOptions"/> 时列出这一列出现过的值），
    /// <see cref="ColumnFilterKind.Keyword"/> 输入关键字。选中的值在 DataTable 的 Filters 里。
    /// </summary>
    [Parameter] public ColumnFilterKind? Filter { get; set; }

    /// <summary>表头筛选的选项（值与文字）；不写时列出这一列出现过的值（按语言排序，状态列按状态的顺序、显示状态名）。</summary>
    [Parameter] public IReadOnlyList<ListboxOption>? FilterOptions { get; set; }

    /// <summary>这一行留不留（values 是选中的值，关键字筛选时是 [关键字]）；默认这一列的值在其中 / 文字包含关键字。</summary>
    [Parameter] public Func<TItem, IReadOnlyList<string>, bool>? FilterBy { get; set; }

    /// <summary>可以拖动调整宽度；不写时跟随表格的 Resizable。</summary>
    [Parameter] public bool? Resizable { get; set; }

    /// <summary>
    /// 分组表头的组标题：相邻的几列写同一个组标题时，上方共用一个标题（如“调整前”下的型号、单价、数量），表头变成两行，没分组的列跨两行。
    /// 组里的列被移开时，后一段另起一格、重复组标题。
    /// </summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>内容可以换行；默认一行显示不下时省略。</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>没有值（null、空字符串）时显示的文字，如“待定价”；默认语言包的 <see cref="Lang.EmptyValue"/>（—）。</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>合计行里这一列：<see cref="ColumnSummary.Sum"/> 把数值精确相加，按列的类型与格式显示。有一列写了合计，表格底部就多一行吸底的合计。</summary>
    [Parameter] public ColumnSummary? Summary { get; set; }

    /// <summary>合计行里这一列的内容（拿到全部行：平均值、“3 项待定价”）；写了时代替 <see cref="Summary"/>。</summary>
    [Parameter] public RenderFragment<IReadOnlyList<TItem>>? SummaryContent { get; set; }

    /// <summary>列的标识：没写 Key 时由子类给（属性路径、标题）。</summary>
    internal string ColumnKey => Key ?? DefaultKey ?? Title ?? "";

    /// <summary>列的名称（读屏、列菜单、展开按钮）：标题文字，没有时用 key。</summary>
    internal string Label => Title ?? DefaultTitle ?? ColumnKey;

    /// <summary>没写 Key 时的标识。</summary>
    private protected virtual string? DefaultKey => null;

    /// <summary>没写 Title 时的标题。</summary>
    private protected virtual string? DefaultTitle => null;

    /// <summary>这一行排序、筛选、合计、默认显示用的值。</summary>
    internal abstract object? ValueOf(TItem item);

    /// <summary>自己写的单元格内容；没有时按类型格式化 <see cref="ValueOf"/>。</summary>
    internal virtual RenderFragment? CellContent(TItem item) => null;

    /// <summary>这一列写了能编辑（Editable 或 IsEditable）：表格随之变成网格。</summary>
    internal virtual bool Edits => false;

    /// <summary>这一格能不能编辑。</summary>
    internal virtual bool CanEdit(TItem item) => false;

    /// <summary>编辑这一格时的编辑器（自己写的，或按值类型选的默认编辑器）；没有合适的编辑器时为 null。</summary>
    internal virtual RenderFragment? EditorFor(TItem item, CellEditSession session) => null;

    /// <summary>提交前校验草稿：返回原因就不提交。</summary>
    internal virtual string? ValidateDraft(object? draft, TItem item) => null;

    /// <summary>直接打字开始编辑：打的字变成草稿（数字列只认数字，不是数字时 false，保留原值）。</summary>
    internal virtual bool TryDraftFromText(string text, out object? draft)
    {
        draft = text;
        return true;
    }

    /// <summary>值显示成状态标记（PropertyColumn 的 Statuses）；不是状态列时为 null。</summary>
    internal virtual RenderFragment? StatusContent(object value) => null;

    /// <summary>是状态列。</summary>
    internal virtual bool HasStatuses => false;

    /// <summary>值在筛选里的文字（筛选状态里存它，与选项的值比较）。</summary>
    internal static string FilterText(object? value) => value switch
    {
        null => "",
        IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    /// <summary>没写 FilterOptions 时的选项：这一列出现过的值，按语言排序。</summary>
    internal virtual IReadOnlyList<ListboxOption> FilterOptionsFrom(IEnumerable<TItem> items, string culture)
    {
        var seen = new Dictionary<string, object>();
        foreach (var item in items)
        {
            if (ValueOf(item) is not { } value || FilterText(value) is not { Length: > 0 } text) continue;
            seen.TryAdd(text, value);
        }
        var compare = Collation.Comparer(culture);
        return [.. seen.OrderBy(static pair => pair.Value, Comparer<object>.Create((a, b) => compare(a, b))).Select(static pair => new ListboxOption(pair.Key, pair.Key))];
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder) => Columns?.Add(this);
}

/// <summary>合计行里一列的算法。</summary>
public enum ColumnSummary
{
    /// <summary>精确相加（按 decimal），按列的类型与格式显示。</summary>
    Sum,
}

/// <summary>DataTable 收集列（级联给各列）：每次表格渲染时各列把自己加进来，表体在它们之后渲染（同 QuickGrid）。</summary>
sealed class DataTableColumns<TItem>
{
    readonly List<DataTableColumn<TItem>> collecting = [];

    /// <summary>上一次收集到的列。</summary>
    public IReadOnlyList<DataTableColumn<TItem>> All { get; private set; } = [];

    public void Start() => collecting.Clear();

    public void Add(DataTableColumn<TItem> column) => collecting.Add(column);

    /// <summary>收集完：列变了时返回 true。</summary>
    public bool Finish()
    {
        if (collecting.SequenceEqual(All)) return false;
        All = [.. collecting];
        return true;
    }
}

/// <summary>把内容排到渲染队列的末尾：DataTable 的表格在各列把自己交给表格之后才渲染（同 QuickGrid 的 Defer）。</summary>
sealed class Defer : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}
