using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Rune.UI;

/// <summary>
/// 交给 JS 初始化器的列表数据（写在 data 属性上的 JSON）。手写 JSON（不经反射序列化，WebAssembly 裁剪后也能用）；
/// 中文不转义成 \uXXXX（属性值由 Blazor 按 HTML 转义，宽松的 JSON 转义在这里是安全的），数据量小一半。
/// </summary>
static class ListboxInterop
{
    static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// 选项表 <c>{"labels":["…"],"disabled":[3,17]}</c>（data-rune-options）：虚拟滚动时看不见的选项不在 DOM 里，
    /// JS 靠它按序号移动高亮、做类型搜索。
    /// </summary>
    public static string OptionTable<TItem>(IReadOnlyList<ListEntry<TItem>> entries) => Write(writer =>
    {
        writer.WriteStartObject();
        writer.WriteStartArray("labels");
        foreach (var entry in entries) writer.WriteStringValue(entry.Option.Label);
        writer.WriteEndArray();
        writer.WriteStartArray("disabled");
        for (var index = 0; index < entries.Count; index++)
            if (entries[index].Option.Disabled) writer.WriteNumberValue(index);
        writer.WriteEndArray();
        writer.WriteEndObject();
    });

    /// <summary>字符串数组（SelectedTags 的“+1”…“+N”）。</summary>
    public static string StringArray(IEnumerable<string> values) => Write(writer =>
    {
        writer.WriteStartArray();
        foreach (var value in values) writer.WriteStringValue(value);
        writer.WriteEndArray();
    });

    static string Write(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions)) write(writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
