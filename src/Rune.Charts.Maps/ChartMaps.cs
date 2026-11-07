namespace Rune.UI;

/// <summary>
/// 地图的地理数据（同 vue-tsx-pro 的 @zerodep-dev/vue-tsx-pro-ui/maps）：给 <see cref="MapChart"/> 用的 TopoJSON，第一次用时读出、之后共用。
/// <code>&lt;MapChart Topology="ChartMaps.ChinaProvinces" Feature="provinces" Areas="areas" /&gt;</code>
/// 来源与许可见包里的 NOTICE.md（Natural Earth 公有领域，经 Unovis 整理，Apache-2.0）。也可以用 <see cref="MapTopology.Parse"/> 读自己的 TopoJSON。
/// </summary>
public static class ChartMaps
{
    static readonly Lazy<MapTopology> China = new(() => Load("china-provinces.json"));
    static readonly Lazy<MapTopology> World = new(() => Load("world-countries.json"));

    /// <summary>中国省份：要素组是 provinces，要素的 id 是 "CN-JS" 这样的写法（湖北是 CN-HU、河北是 CN-HB、河南是 CN-HE、海南是 CN-HA、陕西是 CN-SA）。</summary>
    public static MapTopology ChinaProvinces => China.Value;

    /// <summary>世界各国（不含南极洲）：要素组是 countries，要素的 id 是 ISO 3166-1 两位代码（"CN"、"DE"）。</summary>
    public static MapTopology WorldCountries => World.Value;

    static MapTopology Load(string name)
    {
        using var stream = typeof(ChartMaps).Assembly.GetManifestResourceStream($"maps/{name}")
            ?? throw new InvalidOperationException($"找不到内嵌的地理数据 {name}");
        using var reader = new StreamReader(stream);
        return MapTopology.Parse(reader.ReadToEnd());
    }
}
