using System.Text.Json;

namespace Rune.UI;

/// <summary>一个行政区：6 位代码、名称与下一级（最后一级没有）。</summary>
/// <param name="Code">6 位代码。</param>
/// <param name="Name">名称。</param>
/// <param name="Children">下一级；最后一级为 null。</param>
public sealed record AreaNode(string Code, string Name, IReadOnlyList<AreaNode>? Children = null);

/// <summary>
/// 全国省市区（同 vue-tsx-pro 的 /area）：省 → 市 → 区县的树与按代码查名称，Cascader、TreeSelect 直接用。
/// <code>
/// &lt;Cascader TItem="AreaNode" Items="Area.Tree()" GetKey="Area.KeyAt" GetLabel="Area.LabelOf" GetChildren="Area.ChildrenOf" @bind-Value="path" /&gt;
/// Area.Names(path)   // ["浙江省", "杭州市", "西湖区"]
/// </code>
/// 数据取自 @vant/area-data（MIT），来源见包里的 NOTICE.md；第一次用时读出、之后共用。
/// </summary>
public static class Area
{
    static readonly Lazy<(Dictionary<string, string> Provinces, Dictionary<string, string> Cities, Dictionary<string, string> Counties)> data = new(Load);
    static readonly Lazy<IReadOnlyList<AreaNode>>[] trees =
        [new(() => Build(1)), new(() => Build(2)), new(() => Build(3))];

    /// <summary>行政区的代码（交给 TreeSelect、Tree 的 GetKey）。</summary>
    public static readonly Func<AreaNode, string> KeyOf = static node => node.Code;

    /// <summary>行政区的代码（交给 Cascader 的 GetKey，它带着序号）。</summary>
    public static readonly Func<AreaNode, int, string> KeyAt = static (node, _) => node.Code;

    /// <summary>行政区的名称（GetLabel）。</summary>
    public static readonly Func<AreaNode, string> LabelOf = static node => node.Name;

    /// <summary>下一级（GetChildren）。</summary>
    public static readonly Func<AreaNode, IReadOnlyList<AreaNode>?> ChildrenOf = static node => node.Children;

    /// <summary>
    /// 省 → 市 → 区县的树：<paramref name="depth"/> 是展开几级（1 省、2 省市、3 省市区，默认 3）。
    /// 直辖市这类没有市一级的，市一级用省的名字补上，保证几级都在。同样的 depth 只建一次，返回的是同一个列表。
    /// </summary>
    public static IReadOnlyList<AreaNode> Tree(int depth = 3) =>
        depth is >= 1 and <= 3 ? trees[depth - 1].Value : throw new ArgumentOutOfRangeException(nameof(depth), depth, "depth 只能是 1、2、3");

    /// <summary>代码对应的名称（省、市、区县都行）；没有时 null。</summary>
    public static string? Name(string code)
    {
        var (provinces, cities, counties) = data.Value;
        return counties.GetValueOrDefault(code) ?? cities.GetValueOrDefault(code) ?? provinces.GetValueOrDefault(code);
    }

    /// <summary>一条代码路径的名称（Cascader 的值 → 显示用的文字）；不认识的代码原样留着。</summary>
    public static IReadOnlyList<string> Names(IEnumerable<string> path) => [.. path.Select(code => Name(code) ?? code)];

    static IReadOnlyList<AreaNode> Build(int depth)
    {
        var (provinces, cities, counties) = data.Value;
        return [.. provinces.Select(province =>
        {
            if (depth == 1) return new AreaNode(province.Key, province.Value);
            var prefix = province.Key[..2];
            var own = cities.Where(city => city.Key.StartsWith(prefix, StringComparison.Ordinal)).ToList();
            if (own.Count == 0) own = [new($"{prefix}0100", province.Value)];
            return new AreaNode(province.Key, province.Value, [.. own.Select(city => depth == 2
                ? new AreaNode(city.Key, city.Value)
                : new AreaNode(city.Key, city.Value, [.. counties
                    .Where(county => county.Key.StartsWith(city.Key[..4], StringComparison.Ordinal))
                    .Select(county => new AreaNode(county.Key, county.Value))]))]);
        })];
    }

    static (Dictionary<string, string>, Dictionary<string, string>, Dictionary<string, string>) Load()
    {
        using var stream = typeof(Area).Assembly.GetManifestResourceStream("area.json")
            ?? throw new InvalidOperationException("找不到内嵌的省市区数据 area.json");
        using var document = JsonDocument.Parse(stream);
        Dictionary<string, string> Read(string name) =>
            document.RootElement.GetProperty(name).EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetString()!, StringComparer.Ordinal);
        return (Read("province"), Read("city"), Read("county"));
    }
}
