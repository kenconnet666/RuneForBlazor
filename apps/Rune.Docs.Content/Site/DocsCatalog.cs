using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Rune.Docs.Site;

/// <summary>文档页的分类（同 vue-tsx-pro 文档站的 CATEGORIES）：侧栏里的组与顺序。</summary>
public enum DocCategory
{
    /// <summary>指南。</summary>
    Guide,

    /// <summary>基础。</summary>
    Base,

    /// <summary>布局。</summary>
    Layout,

    /// <summary>展示。</summary>
    Display,

    /// <summary>反馈。</summary>
    Feedback,

    /// <summary>浮层。</summary>
    Overlay,

    /// <summary>导航。</summary>
    Navigation,

    /// <summary>输入。</summary>
    Input,

    /// <summary>日期。</summary>
    Date,

    /// <summary>表单。</summary>
    Form,

    /// <summary>表格。</summary>
    Table,

    /// <summary>页面组合。</summary>
    Patterns,

    /// <summary>图表（Rune.Charts）。</summary>
    Charts,

    /// <summary>配置。</summary>
    Config,
}

/// <summary>
/// 文档页的标题、简介、分类与顺序：写在页面组件上，与 <c>@page</c> 一起登记进目录。
/// <code>@page "/components/button"
/// @attribute [DocMeta("Button 按钮", "触发一个操作", DocCategory.Base, Order = 1)]</code>
/// </summary>
/// <param name="title">标题：侧栏、页面标题、搜索都用它。</param>
/// <param name="description">一句话简介：页面开头、搜索结果都显示。</param>
/// <param name="category">分类。</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DocMetaAttribute(string title, string description, DocCategory category) : Attribute
{
    /// <summary>标题。</summary>
    public string Title { get; } = title;

    /// <summary>一句话简介。</summary>
    public string Description { get; } = description;

    /// <summary>分类。</summary>
    public DocCategory Category { get; } = category;

    /// <summary>在分类里的顺序，越小越靠前（可以是小数，插在两页之间）；不写时排在后面、按标题排。</summary>
    public double Order { get; init; } = double.MaxValue;
}

/// <summary>
/// 整页示例（同 vue-tsx-pro 文档站的 frames）：AppShell 这类按视口排版、占整页的组件单独渲染在 <c>/frame/{页面}/{名字}</c>，
/// 示例卡片用 iframe 打开它。<code>@attribute [DocFrame("app-shell", "admin")]</code>
/// </summary>
/// <param name="page">所属页面（地址最后一段）。</param>
/// <param name="name">示例的名字（页面内唯一）。</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DocFrameAttribute(string page, string name) : Attribute
{
    /// <summary>所属页面。</summary>
    public string Page { get; } = page;

    /// <summary>示例的名字。</summary>
    public string Name { get; } = name;
}

/// <summary>一个文档页。</summary>
/// <param name="Path">地址（<c>/components/button</c>）。</param>
/// <param name="Meta">标题、简介、分类与顺序。</param>
/// <param name="Component">页面组件。</param>
public sealed record DocEntry(string Path, DocMetaAttribute Meta, Type Component)
{
    /// <summary>地址的最后一段（<c>button</c>）：渲染模式测试台按它打开页面。</summary>
    public string Slug => Path[(Path.LastIndexOf('/') + 1)..];
}

/// <summary>侧栏里的一组。</summary>
/// <param name="Category">分类。</param>
/// <param name="Label">组名。</param>
/// <param name="Pages">组里的页面，按顺序。</param>
public sealed record DocSectionEntry(DocCategory Category, string Label, IReadOnlyList<DocEntry> Pages);

/// <summary>
/// 文档目录：扫描内容程序集里带 <see cref="DocMetaAttribute"/> 与 <c>@page</c> 的页面。侧栏、搜索、上一篇 / 下一篇都从这里取，顺序一致。
/// </summary>
public static class DocsCatalog
{
    static readonly Lazy<IReadOnlyList<DocEntry>> pages = new(Scan);
    static readonly Lazy<IReadOnlyDictionary<(string Page, string Name), Type>> frames = new(ScanFrames);

    /// <summary>分类的组名。</summary>
    public static string LabelOf(DocCategory category) => category switch
    {
        DocCategory.Guide => "指南",
        DocCategory.Base => "基础",
        DocCategory.Layout => "布局",
        DocCategory.Display => "展示",
        DocCategory.Feedback => "反馈",
        DocCategory.Overlay => "浮层",
        DocCategory.Navigation => "导航",
        DocCategory.Input => "输入",
        DocCategory.Date => "日期",
        DocCategory.Form => "表单",
        DocCategory.Table => "表格",
        DocCategory.Patterns => "页面组合",
        DocCategory.Charts => "图表",
        DocCategory.Config => "配置",
        _ => category.ToString(),
    };

    /// <summary>所有页面，按侧栏顺序（分类、顺序、标题）。</summary>
    public static IReadOnlyList<DocEntry> All => pages.Value;

    /// <summary>侧栏的各组。</summary>
    public static IReadOnlyList<DocSectionEntry> Sections =>
        [.. All.GroupBy(p => p.Meta.Category).Select(g => new DocSectionEntry(g.Key, LabelOf(g.Key), [.. g]))];

    /// <summary>按地址的最后一段找页面；找不到时返回 null。</summary>
    public static DocEntry? Find(string? slug) =>
        All.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>按地址找页面（<c>/components/button</c>，忽略大小写与末尾的斜杠）；找不到时返回 null。</summary>
    public static DocEntry? FindByPath(string path) =>
        All.FirstOrDefault(p => string.Equals(p.Path.Trim('/'), path.Trim('/'), StringComparison.OrdinalIgnoreCase));

    /// <summary>页面组件的元数据。</summary>
    public static DocMetaAttribute MetaOf(Type page) =>
        page.GetCustomAttribute<DocMetaAttribute>() ?? throw new InvalidOperationException($"{page.Name} 没有写 [DocMeta]");

    /// <summary>按页面与名字找整页示例；找不到时返回 null。</summary>
    public static Type? FindFrame(string? page, string? name) =>
        frames.Value.GetValueOrDefault(((page ?? "").ToLowerInvariant(), (name ?? "").ToLowerInvariant()));

    static IReadOnlyList<DocEntry> Scan()
    {
        var found =
            from type in typeof(DocsCatalog).Assembly.GetTypes()
            let meta = type.GetCustomAttribute<DocMetaAttribute>()
            let route = type.GetCustomAttribute<RouteAttribute>()
            where meta is not null && route is not null
            orderby meta.Category, meta.Order, meta.Title
            select new DocEntry(route.Template, meta, type);
        var list = found.ToList();
        var duplicate = list.GroupBy(p => p.Slug, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"文档页的地址重复：{duplicate.Key}（{string.Join("、", duplicate.Select(p => p.Component.Name))}）");
        return list;
    }

    static IReadOnlyDictionary<(string Page, string Name), Type> ScanFrames() =>
        (from type in typeof(DocsCatalog).Assembly.GetTypes()
         let frame = type.GetCustomAttribute<DocFrameAttribute>()
         where frame is not null
         select (Key: (frame.Page.ToLowerInvariant(), frame.Name.ToLowerInvariant()), Type: type))
        .ToDictionary(x => x.Key, x => x.Type);
}
