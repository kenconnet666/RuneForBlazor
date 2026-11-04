using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Rune.UI;

/// <summary>交给 JS 初始化器的列表数据（data-rune-options）。</summary>
static class ListboxInterop
{
    /// <summary>
    /// 选项表 <c>{"labels":["…"],"disabled":[3,17]}</c>：虚拟滚动时看不见的选项不在 DOM 里，
    /// JS 靠它按序号移动高亮、做类型搜索。手写 JSON（不经反射序列化，WebAssembly 裁剪后也能用）。
    /// </summary>
    public static string OptionTable<TItem>(IReadOnlyList<ListEntry<TItem>> entries)
    {
        var buffer = new ArrayBufferWriter<byte>();
        // 中文不转义成 \uXXXX（属性值由 Blazor 按 HTML 转义，宽松的 JSON 转义在这里是安全的），数据量小一半
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
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
        }
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
