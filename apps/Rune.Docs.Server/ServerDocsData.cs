using System.Collections.Concurrent;
using System.Reflection;
using Rune.Docs.Components.Base.Icon;
using Rune.Docs.Site;
using Rune.Icons;
using Rune.UI;

namespace Rune.Docs.Server;

/// <summary>
/// 服务端的文档数据：当场从示例文件、程序集与 XML 文档注释生成（同一份结果全进程缓存）。
/// 用到过的地址记进 <see cref="DocsFiles"/>，静态导出按它把这些文件写出来，浏览器从那里下载。
/// </summary>
public sealed class ServerDocsData : DocsData
{
    static readonly ConcurrentDictionary<string, string> rendered = new();
    static readonly Lazy<(IReadOnlyList<IconEntry> List, string Json)> icons = new(LoadIcons);

    readonly DocsFiles files;

    /// <summary>用到的地址记进 <paramref name="files"/>。</summary>
    public ServerDocsData(DocsFiles files) => this.files = files;

    /// <inheritdoc/>
    public override ValueTask<string> SourceAsync(Type example) => new(Use(SourcePath(example), () => ExampleSource.Of(example)));

    /// <inheritdoc/>
    public override ValueTask<ComponentApi> ApiAsync(Type type) => new(ApiFromJson(Use(ApiPath(type), () => ToJson(ComponentApiReader.Read(type)))));

    /// <inheritdoc/>
    public override ValueTask<IReadOnlyList<IconEntry>> IconsAsync()
    {
        files.Add(IconsPath);
        return new(icons.Value.List);
    }

    string Use(string path, Func<string> create)
    {
        files.Add(path);
        return rendered.GetOrAdd(path, _ => create());
    }

    /// <summary>地址对应的文件内容（/_docs/… 的响应）；不认识的地址为 null。</summary>
    internal static string? Render(string path)
    {
        if (path == IconsPath) return icons.Value.Json;
        if (rendered.TryGetValue(path, out var content)) return content;
        return path switch
        {
            _ when Parse(path, "_docs/source/", ".txt") is { } type => rendered.GetOrAdd(path, _ => ExampleSource.Of(type)),
            _ when Parse(path, "_docs/api/", ".json") is { } type => rendered.GetOrAdd(path, _ => ToJson(ComponentApiReader.Read(type))),
            _ => null,
        };
    }

    // 地址里的类型：按 Key 的写法在文档内容与组件库的程序集里找
    static Type? Parse(string path, string prefix, string suffix) =>
        path.StartsWith(prefix, StringComparison.Ordinal) && path.EndsWith(suffix, StringComparison.Ordinal)
            ? Types.Value.GetValueOrDefault(path[prefix.Length..^suffix.Length])
            : null;

    // 示例在文档内容里，API 表列组件库各包的类型
    static readonly string[] Assemblies = ["Rune.Docs.Content", "Rune.UI", "Rune.UI.Dates", "Rune.UI.Area", "Rune.Charts", "Rune.Charts.Maps"];

    static readonly Lazy<IReadOnlyDictionary<string, Type>> Types = new(() =>
        Assemblies.Select(name => Assembly.Load(name))
            .SelectMany(a => a.GetTypes())
            .Where(t => t.FullName is not null && !t.FullName.Contains('<'))
            .GroupBy(Key)
            .ToDictionary(g => g.Key, g => g.First()));

    // 全部图标：Lucide 的属性名、文件名与数据，按名字排好；旧名（[Obsolete] 转发）不重复列出
    static (IReadOnlyList<IconEntry>, string) LoadIcons()
    {
        IReadOnlyList<IconEntry> list = typeof(Lucide).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(IconData) && !p.IsDefined(typeof(ObsoleteAttribute)))
            .Select(p => new IconEntry(p.Name, (IconData)p.GetValue(null)!))
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return (list, ToJson(list));
    }
}

/// <summary>预渲染用到过的文档数据的地址（进程内）：静态导出爬完页面后取这份清单，把文件一并写出。</summary>
public sealed class DocsFiles
{
    readonly ConcurrentDictionary<string, byte> paths = new();

    /// <summary>记下一个地址。</summary>
    public void Add(string path) => paths.TryAdd(path, 0);

    /// <summary>记下的全部地址（排好序）。</summary>
    public IReadOnlyList<string> All => [.. paths.Keys.Order(StringComparer.Ordinal)];
}
