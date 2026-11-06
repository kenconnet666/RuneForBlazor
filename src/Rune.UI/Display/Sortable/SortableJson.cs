using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rune.UI;

// 裁剪友好：源生成的序列化，不靠反射；文字原样写进属性（Blazor 会转义属性值）
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class SortableJson : JsonSerializerContext
{
    internal static SortableJson Relaxed { get; } = new(new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
}
