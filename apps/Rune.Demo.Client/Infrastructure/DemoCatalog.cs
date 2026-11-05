using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Rune.Demo.Client.Infrastructure;

/// <summary>一个演示。</summary>
/// <param name="Slug">地址里的名字。</param>
/// <param name="Title">显示的名字。</param>
/// <param name="Group">分组。</param>
/// <param name="Order">组内排序。</param>
/// <param name="Component">演示组件的类型。</param>
public sealed record DemoEntry(string Slug, string Title, string Group, int Order, Type Component);

/// <summary>
/// 演示目录：扫描本程序集里带 <see cref="ComponentDemoAttribute"/> 的组件。
/// 第一次用到时才扫描（服务端与 WebAssembly 各扫一次），之后一直用同一份。
/// </summary>
public static class DemoCatalog
{
    static readonly Lazy<IReadOnlyList<DemoEntry>> entries = new(Scan);
    static readonly Lazy<IReadOnlyDictionary<(string Slug, string Name), Type>> frames = new(ScanFrames);

    /// <summary>分组的显示顺序（按字符串排会把“布局”排到“基础”前面）；不在表里的分组排在最后。</summary>
    static readonly string[] groupOrder = ["基础", "基础组件", "布局", "表单", "展示", "反馈", "浮层", "导航", "表格", "页面组合"];

    /// <summary>全部演示，按分组、排序、名字排好。</summary>
    public static IReadOnlyList<DemoEntry> All => entries.Value;

    /// <summary>按地址里的名字找演示；找不到时返回 null。</summary>
    public static DemoEntry? Find(string? slug) =>
        All.FirstOrDefault(e => string.Equals(e.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>按演示与名字找整页示例（<see cref="DemoFrameAttribute"/>）；找不到时返回 null。</summary>
    public static Type? FindFrame(string? slug, string? name) =>
        frames.Value.GetValueOrDefault(((slug ?? "").ToLowerInvariant(), (name ?? "").ToLowerInvariant()));

    static IReadOnlyDictionary<(string Slug, string Name), Type> ScanFrames() =>
        (from type in typeof(DemoCatalog).Assembly.GetTypes()
         let frame = type.GetCustomAttribute<DemoFrameAttribute>()
         where frame is not null && typeof(IComponent).IsAssignableFrom(type)
         select (Key: (frame.Slug.ToLowerInvariant(), frame.Name.ToLowerInvariant()), type))
        .ToDictionary(x => x.Key, x => x.type);

    static IReadOnlyList<DemoEntry> Scan()
    {
        var found =
            from type in typeof(DemoCatalog).Assembly.GetTypes()
            let demo = type.GetCustomAttribute<ComponentDemoAttribute>()
            where demo is not null && typeof(IComponent).IsAssignableFrom(type)
            let groupIndex = Array.IndexOf(groupOrder, demo.Group) is var i and >= 0 ? i : int.MaxValue
            orderby groupIndex, demo.Group, demo.Order, demo.Title
            select new DemoEntry(demo.Slug, demo.Title, demo.Group, demo.Order, type);
        var list = found.ToList();

        var duplicate = list.GroupBy(e => e.Slug, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"演示的名字重复：{duplicate.Key}（{string.Join("、", duplicate.Select(e => e.Component.Name))}）");
        return list;
    }
}

/// <summary>演示可以选的渲染模式：地址里的名字 ↔ Blazor 的渲染模式。</summary>
public static class DemoRenderModes
{
    /// <summary>地址里的名字与说明，按显示顺序。</summary>
    public static readonly IReadOnlyList<(string Slug, string Title)> All =
    [
        ("static", "静态 SSR"),
        ("server", "Server"),
        ("wasm", "WebAssembly"),
        ("auto", "Auto"),
    ];

    /// <summary>默认模式：开发时最常用、改代码热重载最快。</summary>
    public const string Default = "server";

    /// <summary>名字对应的渲染模式；静态渲染是 null。</summary>
    public static IComponentRenderMode? Of(string slug) => slug switch
    {
        "server" => RenderMode.InteractiveServer,
        "wasm" => RenderMode.InteractiveWebAssembly,
        "auto" => RenderMode.InteractiveAuto,
        _ => null,
    };

    /// <summary>是不是认识的模式名。</summary>
    public static bool IsKnown(string? slug) => All.Any(m => m.Slug == slug);
}
