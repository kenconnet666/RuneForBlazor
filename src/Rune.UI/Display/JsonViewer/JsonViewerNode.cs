using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Rune.UI;

/// <summary>JSON 查看器树上的一项。</summary>
/// <param name="Path">路径（JavaScript 写法：<c>$.items[0]["unit-price"]</c>），也是树的 key。</param>
/// <param name="Name">键名或序号。</param>
/// <param name="Value">这一项的值。</param>
public sealed partial record JsonViewerNode(string Path, string Name, JsonElement Value)
{
    // 中文等不转义（看原始数据时要读得懂）；对象、数组缩进两格
    static readonly JsonWriterOptions Readable = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = true };

    IReadOnlyList<JsonViewerNode>? children;

    /// <summary>对象、数组：可以展开。</summary>
    public bool IsContainer => Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array;

    /// <summary>名字是数组的序号。</summary>
    public bool IsIndex => Name.Length > 0 && Name.All(char.IsAsciiDigit);

    /// <summary>子节点（展开到哪一层才建哪一层，每个节点只建一次）；不是容器时为 null。</summary>
    public IReadOnlyList<JsonViewerNode>? Children => !IsContainer ? null : children ??= Value.ValueKind == JsonValueKind.Array
        ? [.. Value.EnumerateArray().Select((item, index) => new JsonViewerNode($"{Path}[{index}]", index.ToString(System.Globalization.CultureInfo.InvariantCulture), item))]
        : [.. Value.EnumerateObject().Select(member => new JsonViewerNode(MemberPath(Path, member.Name), member.Name, member.Value))];

    /// <summary>收起时的项数。</summary>
    public int Count => Value.ValueKind == JsonValueKind.Array ? Value.GetArrayLength() : Value.EnumerateObject().Count();

    /// <summary>一行里值的写法：字符串带引号，数字照原文（不丢精度），对象、数组是 {…}、[…]。</summary>
    public string Display => Value.ValueKind switch
    {
        JsonValueKind.Object => "{…}",
        JsonValueKind.Array => "[…]",
        JsonValueKind.String => Write(writer => writer.WriteStringValue(Value.GetString())),
        _ => Value.GetRawText(),
    };

    /// <summary>复制的值：字符串原样，对象、数组写成缩进两格的 JSON。</summary>
    public string CopyText => Value.ValueKind switch
    {
        JsonValueKind.String => Value.GetString()!,
        JsonValueKind.Object or JsonValueKind.Array => Write(Value.WriteTo),
        _ => Display,
    };

    /// <summary>根：是对象、数组时第一层是它的各项，不然就是它自己。</summary>
    internal static IReadOnlyList<JsonViewerNode> RootsOf(JsonElement value)
    {
        var root = new JsonViewerNode("$", "$", value);
        return root.Children ?? [root];
    }

    // 对象的键接在路径后面：标识符用点，别的写成 ["…"]
    static string MemberPath(string parent, string key) =>
        Identifier().IsMatch(key) ? $"{parent}.{key}" : $"{parent}[{Write(writer => writer.WriteStringValue(key))}]";

    // 用 Utf8JsonWriter 写（不经反射的序列化，裁剪发布也能用）
    static string Write(Action<Utf8JsonWriter> write)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, Readable)) write(writer);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    [GeneratedRegex(@"^[A-Za-z_$][\w$]*$")]
    private static partial Regex Identifier();

    // 节点按路径比较：作为记录比较 JsonElement 不比内容，子节点的缓存也不参与
    /// <inheritdoc/>
    public bool Equals(JsonViewerNode? other) => other is not null && other.Path == Path;

    /// <inheritdoc/>
    public override int GetHashCode() => Path.GetHashCode(StringComparison.Ordinal);
}
