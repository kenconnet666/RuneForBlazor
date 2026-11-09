using System.Text.RegularExpressions;

namespace Rune.E2E;

/// <summary>
/// 文档站的全部组件页（地址 /components/{页}），供逐页检查的测试（截图基线、焦点环）用。
/// xunit 只让不同的测试类并行，同一个理论的各行依次跑：逐页的测试分成 <see cref="Shards"/> 个子类（…0 到 …3），各取 <see cref="Shard"/> 的一组，几组同时跑。
/// </summary>
public static partial class DocsPages
{
    /// <summary>逐页的测试分成几组。</summary>
    public const int Shards = 4;

    /// <summary>全部页，按名称排序。</summary>
    public static IEnumerable<string> All =>
        Directory.EnumerateFiles(Path.Combine(DemoProcess.RepositoryRoot, "apps", "Rune.Docs.Content", "Components"), "*Page.razor", SearchOption.AllDirectories)
            .Select(file => PageRoute().Match(File.ReadAllText(file)))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal);

    /// <summary>第 <paramref name="shard"/> 组：按序号轮流分，各组的页数与轻重相近。</summary>
    public static TheoryData<string> Shard(int shard) => [.. All.Where((_, index) => index % Shards == shard)];

    // 页面文件可能是 CRLF：多行模式的 $ 只认 \n
    [GeneratedRegex("""@page "/components/([a-z0-9-]+)"\r?$""", RegexOptions.Multiline)]
    private static partial Regex PageRoute();
}
