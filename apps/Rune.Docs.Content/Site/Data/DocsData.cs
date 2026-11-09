using System.Text.Json;
using System.Text.Json.Serialization;
using Rune.Docs.Components.Base.Icon;
using Rune.UI;

namespace Rune.Docs.Site;

/// <summary>
/// 文档的数据：示例的源码、组件的 API 表、图标库的全部图标。
/// 它们由服务端从程序集、示例文件与 XML 文档注释生成（Rune.Docs.Server），浏览器里不带这些原料（几 MB）：
/// 预渲染时用到的随页面带过去（PersistentComponentState），站内换页时再按类型下载 <c>/_docs/…</c> 的静态文件（静态导出时一并写出）。
/// 服务端、浏览器各注册一个实现；组件只经这里取，取到之前显示空的内容。
/// </summary>
public abstract class DocsData
{
    /// <summary>随页面带给浏览器的数据在 PersistentComponentState 里的键：地址 → 文件内容。</summary>
    public const string StateKey = "rune-docs-data";

    /// <summary>示例组件的源码（示例文件原文）。</summary>
    public abstract ValueTask<string> SourceAsync(Type example);

    /// <summary>组件（或数据类型）的 API 表。泛型组件写开放类型。</summary>
    public abstract ValueTask<ComponentApi> ApiAsync(Type type);

    /// <summary>图标库的全部图标（Lucide，按名字排好）。</summary>
    public abstract ValueTask<IReadOnlyList<IconEntry>> IconsAsync();

    /// <summary>示例源码的地址（相对站点根）。</summary>
    public static string SourcePath(Type example) => $"_docs/source/{Key(example)}.txt";

    /// <summary>API 表的地址（相对站点根）。</summary>
    public static string ApiPath(Type type) => $"_docs/api/{Key(type)}.json";

    /// <summary>图标库的地址（相对站点根）。</summary>
    public const string IconsPath = "_docs/icons.json";

    /// <summary>类型在地址里的写法：完整类型名，泛型的 ` 换成 -（Rune.UI.Slider-1），嵌套类型的 + 换成 .。</summary>
    public static string Key(Type type) =>
        (type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName!.Replace('`', '-').Replace('+', '.');

    /// <summary>API 表写成 JSON（源码生成，WebAssembly 裁剪后也能读）。</summary>
    public static string ToJson(ComponentApi api) => JsonSerializer.Serialize(api, DocsJson.Default.ComponentApi);

    /// <summary>读 JSON 写的 API 表。</summary>
    public static ComponentApi ApiFromJson(string json) =>
        JsonSerializer.Deserialize(json, DocsJson.Default.ComponentApi) ?? throw new InvalidDataException("API 表的 JSON 是空的");

    /// <summary>图标库写成 JSON。</summary>
    public static string ToJson(IReadOnlyList<IconEntry> icons) => JsonSerializer.Serialize(icons, DocsJson.Default.IReadOnlyListIconEntry);

    /// <summary>读 JSON 写的图标库。</summary>
    public static IReadOnlyList<IconEntry> IconsFromJson(string json) =>
        JsonSerializer.Deserialize(json, DocsJson.Default.IReadOnlyListIconEntry) ?? throw new InvalidDataException("图标库的 JSON 是空的");
}

/// <summary>文档数据的 JSON（源码生成：不靠反射，WebAssembly 裁剪后照样能读写）。</summary>
[JsonSerializable(typeof(ComponentApi))]
[JsonSerializable(typeof(IReadOnlyList<IconEntry>))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal sealed partial class DocsJson : JsonSerializerContext;
