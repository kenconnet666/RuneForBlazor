using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 各种图表的公共部分：量尺寸、外层结构（<see cref="ChartFrame"/>）的接线、共有的参数（单位、数值格式、高度、提示框、加载与空状态、导出、部件属性）。
/// 子类只决定画什么（<see cref="Draw"/>）、图例与数据表；在自己的标记里输出 <see cref="Frame"/>。
/// <list type="bullet">
/// <item>尺寸：charts.js 量到绘图区的大小后以 rune-command size 交来；没量到时交互渲染不画，静态 SSR、预渲染按约定的大小画（SVG 用 viewBox 缩放）。</item>
/// <item>悬停：<see cref="Draw"/> 给出绘图区上的 JSON（data-chart），十字线、高亮、提示框在 charts.js 里画，不往返；点了一项发 activate。</item>
/// </list>
/// </summary>
public abstract class ChartBase : UIComponent
{
    // 没量到时的宽度：文档的内容栏、卡片里常见的宽度
    const double FallbackWidth = 600;

    bool measured;

    /// <summary>
    /// 数值的格式：提示框、数据表（直角坐标图表还有数值轴）共用，按当前语言格式化。以万元为单位画金额时写
    /// <c>new NumberFormat { Scale = 10_000, MaximumFractionDigits = 1 }</c> 并配 <c>Unit="万元"</c>。
    /// </summary>
    [Parameter] public NumberFormat? ValueFormat { get; set; }

    /// <summary>单位：显示在左上方，提示框与数据表的数值后面带上。</summary>
    [Parameter] public string? Unit { get; set; }

    /// <summary>绘图区的高度，默认 240px。</summary>
    [Parameter] public CssLength? Height { get; set; }

    /// <summary>悬停时的提示框，默认显示。</summary>
    [Parameter] public bool Tooltip { get; set; } = true;

    /// <summary>加载中：绘图区显示扫光的骨架，带 aria-busy。</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>没有数据时显示的内容，默认是“暂无数据”。</summary>
    [Parameter] public RenderFragment? Empty { get; set; }

    /// <summary>右上角的导出菜单：绘图区存成 PNG 图片，数据表存成 CSV。</summary>
    [Parameter] public bool Exportable { get; set; }

    /// <summary>导出的文件名（不含扩展名）；不写时用图表的名称（aria-label）。</summary>
    [Parameter] public string? ExportName { get; set; }

    /// <summary>上方一行（单位与图例）的部件属性。</summary>
    [Parameter] public Attrs? PropsHeader { get; set; }

    /// <summary>单位的部件属性。</summary>
    [Parameter] public Attrs? PropsUnit { get; set; }

    /// <summary>图例的部件属性。</summary>
    [Parameter] public Attrs? PropsLegend { get; set; }

    /// <summary>图例每一项（ButtonBase）的部件属性。</summary>
    [Parameter] public Attrs? PropsLegendItem { get; set; }

    /// <summary>绘图区的部件属性。</summary>
    [Parameter] public Attrs? PropsPlot { get; set; }

    /// <summary>给读屏的数据表的部件属性。</summary>
    [Parameter] public Attrs? PropsTable { get; set; }

    /// <summary>绘图区的宽（px）：量到的，或没量到时约定的。</summary>
    private protected double PlotWidth { get; private set; }

    /// <summary>绘图区的高（px）。</summary>
    private protected double PlotHeight { get; private set; }

    /// <summary>尺寸是量到的（false 时 SVG 用 viewBox 缩放到绘图区）。</summary>
    private protected bool Measured => measured;

    // ───────── 各种图的差别 ─────────

    /// <summary>没有数据：绘图区显示空状态，没有数据表。</summary>
    private protected abstract bool IsEmpty { get; }

    /// <summary>能导出图片（绘图区是 svg）。</summary>
    private protected virtual bool ExportsImage => true;

    /// <summary>给读屏的数据表（也是导出 CSV 的内容）。</summary>
    private protected abstract ChartTable BuildTable();

    /// <summary>画出绘图区的内容（SVG、提示框）与悬停要用的数据（JSON）；尺寸已定、有数据时才调用，返回 null 时不画。</summary>
    private protected abstract (RenderFragment Plot, string? Data)? Draw();

    /// <summary>图例的各项。</summary>
    private protected virtual IReadOnlyList<ChartLegendItem> LegendItems => [];

    /// <summary>图例里隐藏着的项。</summary>
    private protected virtual IReadOnlyCollection<string> HiddenKeys => [];

    /// <summary>点了图例的一项。</summary>
    private protected virtual Task ToggleLegendAsync(string key) => Task.CompletedTask;

    /// <summary>能下钻：绘图区的指针是手形，点击交给 <see cref="ActivateAsync"/>。</summary>
    private protected virtual bool Clickable => false;

    /// <summary>点了第 index 项（下钻）：data-index 的值。</summary>
    private protected virtual Task ActivateAsync(int index) => Task.CompletedTask;

    /// <summary>不用量尺寸：按固定的画布画、用 viewBox 缩放（雷达图），或者是 HTML（漏斗图）。</summary>
    private protected virtual bool FixedCanvas => false;

    /// <summary>绘图区的高度：默认是 Height 参数（雷达图默认 300px，漏斗图随内容）。</summary>
    private protected virtual CssLength? FrameHeight => Height;

    /// <summary>点了空白处（网络图取消选中）：悬停数据里写了 select 时 charts.js 发 deselect。</summary>
    private protected virtual Task DeselectAsync() => Task.CompletedTask;

    /// <summary>左上方的单位；百分比堆叠时不写。</summary>
    private protected virtual string? FrameUnit => Unit;

    /// <summary>右上方的单位（第二数值轴）。</summary>
    private protected virtual string? FrameUnitEnd => null;

    /// <summary>加载中的骨架，默认一整块。</summary>
    private protected virtual RenderFragment SkeletonContent => DefaultSkeleton;

    /// <summary>绘图区下面的内容（缩放的滑块）。</summary>
    private protected virtual RenderFragment? FooterContent => null;

    /// <summary>上方一行右端的操作（地图的缩放按钮）。</summary>
    private protected virtual RenderFragment? ActionsContent => null;

    static readonly RenderFragment DefaultSkeleton = builder =>
    {
        builder.OpenComponent<Skeleton>(0);
        builder.AddComponentParameter(1, nameof(Skeleton.Sx), (Sx)ChartStyles.SkeletonBlock);
        builder.CloseComponent();
    };

    // ───────── 共用的层与小工具 ─────────

    /// <summary>图表的 SVG。</summary>
    private protected static readonly Attrs SvgLayer = new() { Sx = ChartSvgStyles.Svg };

    /// <summary>整张图淡入。</summary>
    private protected static readonly Attrs FadeLayer = new() { Sx = ChartSvgStyles.FadeIn };

    /// <summary>不接指针（文字、标签）。</summary>
    private protected static readonly Attrs NoPointerLayer = new() { Sx = ChartSvgStyles.NoPointer };

    /// <summary>SVG 里的数字：三位小数。</summary>
    private protected static string N(double value) => SvgFormat.Number(value);

    /// <summary>图例里点了一项：在隐藏的列表里加上或去掉它。</summary>
    private protected static IReadOnlyList<string> Toggled(IReadOnlyList<string> hidden, string key) =>
        hidden.Contains(key) ? [.. hidden.Where(item => item != key)] : [.. hidden, key];

    // ───────── 文字 ─────────

    /// <summary>按格式与单位写一个数值；没有值时是语言包的 EmptyValue。</summary>
    private protected string Format(decimal? value, NumberFormat? format, string? unit)
    {
        if (value is not { } number) return Config.Lang.EmptyValue;
        var resolved = format ?? NumberFormat.Default;
        return LangFormat.Number(number, unit is null ? resolved : resolved with { Suffix = unit }, Config.Lang);
    }

    /// <summary>数值的文字：按 ValueFormat 格式化、带上单位。</summary>
    private protected string ValueText(decimal? value) => Format(value, ValueFormat, Unit);

    /// <summary>数值的文字，不带单位（数据表的格子：单位写在列名上）。</summary>
    private protected string NumberText(decimal? value) => Format(value, ValueFormat, null);

    /// <summary>占比的文字：最多一位小数的百分比；分母为 0 时是 EmptyValue。</summary>
    private protected string ShareText(double part, double total) =>
        total == 0 ? Config.Lang.EmptyValue : LangFormat.Number((decimal)(part / total), ShareFormat, Config.Lang);

    static readonly NumberFormat ShareFormat = new() { Style = NumberStyle.Percent, MaximumFractionDigits = 1 };

    // ───────── 悬停的数据 ─────────

    /// <summary>每一项的提示框（标题与各行）写成 tips 数组：charts.js 按 data-index 取。</summary>
    private protected static void WriteTips(Utf8JsonWriter json, IEnumerable<(string Title, IReadOnlyList<ChartTooltipItem> Items)> tips)
    {
        json.WriteStartArray("tips");
        foreach (var (title, items) in tips)
        {
            json.WriteStartArray();
            json.WriteStringValue(title);
            json.WriteStartArray();
            foreach (var item in items)
            {
                json.WriteStartArray();
                json.WriteStringValue(item.Label);
                if (item.Color is { } color) json.WriteStringValue(color.ToString());
                else json.WriteNullValue();
                json.WriteStringValue(item.Value);
                json.WriteEndArray();
            }
            json.WriteEndArray();
            json.WriteEndArray();
        }
        json.WriteEndArray();
    }

    /// <summary>写一份悬停数据（JSON）：write 往对象里加字段，宽度已写好。</summary>
    private protected string HoverJson(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteNumber("width", PlotWidth);
            write(json);
            json.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// 按项悬停的图（环形图、雷达图、矩形树图……）：指针下的那一项（data-index）高亮、其余变淡（dim），
    /// 提示框跟着指针，内容是 tips 里的第 data-index 项。
    /// </summary>
    /// <param name="tips">每一项的提示框。</param>
    /// <param name="dim">其余的项变淡。</param>
    /// <param name="groups">悬停第 i 项时一起高亮的项（含自己）；null 时只有自己。</param>
    private protected string ItemHoverData(IEnumerable<(string Title, IReadOnlyList<ChartTooltipItem> Items)> tips, bool dim = true,
        IEnumerable<IEnumerable<int>>? groups = null) => HoverJson(json =>
    {
        json.WriteString("mode", "item");
        json.WriteBoolean("dim", dim);
        if (groups is not null)
        {
            json.WriteStartArray("groups");
            foreach (var group in groups)
            {
                json.WriteStartArray();
                foreach (var index in group) json.WriteNumberValue(index);
                json.WriteEndArray();
            }
            json.WriteEndArray();
        }
        if (Tooltip) WriteTips(json, tips);
    });

    /// <summary>能平移缩放（网络图、地图）：缩放的范围与按钮、键盘每一档的倍数。</summary>
    private protected static void WritePanZoom(Utf8JsonWriter json, double min, double max, double step)
    {
        json.WriteStartObject("panZoom");
        json.WriteNumber("min", min);
        json.WriteNumber("max", max);
        json.WriteNumber("step", step);
        json.WriteEndObject();
    }

    // ───────── 尺寸与命令 ─────────

    /// <summary>
    /// 这次渲染用的尺寸：量到了就用量到的；交互渲染还没量到时不画（等 charts.js 交来 size）；
    /// 静态 SSR、预渲染时按约定的宽度与 Height（px 时）画，交互之后换成量到的。
    /// </summary>
    bool UseSize()
    {
        if (measured) return true;
        if (RendererInfo.IsInteractive) return false;
        PlotWidth = FallbackWidth;
        PlotHeight = Height?.ToString() is { } text && text.EndsWith("px", StringComparison.Ordinal)
            && double.TryParse(text[..^2], CultureInfo.InvariantCulture, out var pixels) ? pixels : 240;
        return true;
    }

    async Task OnPlotCommandAsync(CommandEventArgs args)
    {
        if (args.Name == "size" && args.Text?.Split(',') is [var w, var h])
        {
            PlotWidth = double.Parse(w, CultureInfo.InvariantCulture);
            PlotHeight = double.Parse(h, CultureInfo.InvariantCulture);
            measured = true;
        }
        else if (args.Name == "activate" && int.TryParse(args.Text, CultureInfo.InvariantCulture, out var index) && index >= 0)
            await ActivateAsync(index);
        else if (args.Name == "deselect")
            await DeselectAsync();
    }

    /// <summary>SVG 的宽高：量到时是像素；没量到时占满绘图区，按 viewBox 缩放。</summary>
    private protected Attrs SvgSize() => measured
        ? new Attrs { ["width"] = SvgFormat.Number(PlotWidth), ["height"] = SvgFormat.Number(PlotHeight) }
        : new Attrs { ["width"] = "100%", ["height"] = "100%", ["viewBox"] = $"0 0 {SvgFormat.Number(PlotWidth)} {SvgFormat.Number(PlotHeight)}" };

    /// <summary>外层结构：子类在自己的标记里输出它。</summary>
    protected RenderFragment Frame => builder =>
    {
        var drawn = !Loading && !IsEmpty && (FixedCanvas || UseSize()) ? Draw() : null;
        builder.OpenComponent<ChartFrame>(0);
        builder.AddComponentParameter(1, nameof(ChartFrame.Unit), FrameUnit);
        builder.AddComponentParameter(2, nameof(ChartFrame.UnitEnd), FrameUnitEnd);
        builder.AddComponentParameter(3, nameof(ChartFrame.Height), FrameHeight);
        builder.AddComponentParameter(4, nameof(ChartFrame.Legend), LegendItems);
        builder.AddComponentParameter(5, nameof(ChartFrame.HiddenKeys), HiddenKeys);
        builder.AddComponentParameter(6, nameof(ChartFrame.OnLegendToggle), EventCallback.Factory.Create<string>(this, ToggleLegendAsync));
        builder.AddComponentParameter(7, nameof(ChartFrame.Loading), Loading);
        builder.AddComponentParameter(8, nameof(ChartFrame.Skeleton), SkeletonContent);
        builder.AddComponentParameter(9, nameof(ChartFrame.IsEmpty), IsEmpty);
        builder.AddComponentParameter(10, nameof(ChartFrame.Empty), Empty);
        builder.AddComponentParameter(11, nameof(ChartFrame.Table), BuildTable());
        builder.AddComponentParameter(12, nameof(ChartFrame.PlotData), drawn?.Data);
        builder.AddComponentParameter(13, nameof(ChartFrame.OnPlotCommand), EventCallback.Factory.Create<CommandEventArgs>(this, OnPlotCommandAsync));
        builder.AddComponentParameter(14, nameof(ChartFrame.Clickable), Clickable);
        builder.AddComponentParameter(15, nameof(ChartFrame.Plot), drawn?.Plot);
        builder.AddComponentParameter(16, nameof(ChartFrame.Footer), drawn is null ? null : FooterContent);
        builder.AddComponentParameter(17, nameof(ChartFrame.Actions), ActionsContent);
        builder.AddComponentParameter(18, nameof(ChartFrame.Exportable), Exportable);
        builder.AddComponentParameter(19, nameof(ChartFrame.ExportName), ExportName);
        builder.AddComponentParameter(20, nameof(ChartFrame.PropsHeader), PropsHeader);
        builder.AddComponentParameter(21, nameof(ChartFrame.PropsUnit), PropsUnit);
        builder.AddComponentParameter(22, nameof(ChartFrame.PropsLegend), PropsLegend);
        builder.AddComponentParameter(23, nameof(ChartFrame.PropsLegendItem), PropsLegendItem);
        builder.AddComponentParameter(24, nameof(ChartFrame.PropsPlot), PropsPlot);
        builder.AddComponentParameter(25, nameof(ChartFrame.PropsTable), PropsTable);
        builder.AddComponentParameter(26, nameof(ChartFrame.ExportsImage), ExportsImage);
        builder.AddMultipleAttributes(26, Attrs.MergeProps(null, Rest));
        builder.CloseComponent();
    };

    static readonly Attrs TooltipLayer = new() { Sx = ChartStyles.Tooltip };
    static readonly Attrs TooltipTitleLayer = new() { Sx = ChartStyles.TooltipTitle };
    static readonly Attrs TooltipRowLayer = new() { Sx = ChartStyles.TooltipRow };
    static readonly Attrs SwatchLayer = new() { Sx = ChartStyles.Swatch };
    static readonly Attrs TooltipLabelLayer = new() { Sx = ChartStyles.TooltipLabel };
    static readonly Attrs TooltipValueLayer = new() { Sx = ChartStyles.TooltipValue };

    /// <summary>提示框与它的原型（charts.js 按每项的数据克隆原型、填字）：放在绘图区里 SVG 的后面。</summary>
    private protected static readonly RenderFragment TooltipBox = builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-part", "tooltip");
        builder.AddMultipleAttributes(2, Attrs.Merge(TooltipLayer));
        builder.CloseElement();
        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "hidden", true);
        builder.AddAttribute(5, "data-tip-proto", true);
        builder.OpenElement(6, "div");
        builder.AddAttribute(7, "data-tip-title", true);
        builder.AddMultipleAttributes(8, Attrs.Merge(TooltipTitleLayer));
        builder.CloseElement();
        builder.OpenElement(9, "div");
        builder.AddAttribute(10, "data-tip-row", true);
        builder.AddMultipleAttributes(11, Attrs.Merge(TooltipRowLayer));
        foreach (var (sequence, layer) in new[] { (12, SwatchLayer), (14, TooltipLabelLayer), (16, TooltipValueLayer) })
        {
            builder.OpenElement(sequence, "span");
            builder.AddMultipleAttributes(sequence + 1, Attrs.Merge(layer));
            builder.CloseElement();
        }
        builder.CloseElement();
        builder.CloseElement();
    };
}
