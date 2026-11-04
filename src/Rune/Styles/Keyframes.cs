using System.Text;

namespace Rune.Styles;

/// <summary>
/// 关键帧动画。名字按内容生成；样式里用 <see cref="Style.Keyframes"/> 引用时，这组关键帧随那个类一起输出：
/// <code>
/// static Keyframes Spin => Css.Define(static () => new Keyframes
/// {
///     ["from"] = new() { Transform = "rotate(0deg)" },
///     ["to"] = new() { Transform = "rotate(360deg)" },
/// });
/// static Style Spinner => Css.Define(static () => new Style { Keyframes = Spin, AnimationDuration = 800, AnimationIterationCount = "infinite" });
/// </code>
/// </summary>
public sealed class Keyframes
{
    readonly List<(string Offset, Style Frame)> frames = [];
    string? name;
    string? css;

    /// <summary>一帧：<c>["from"]</c>、<c>["50%"]</c>、<c>["to"]</c>；帧里只写声明，不写条件。</summary>
    public Style this[string offset]
    {
        init => frames.Add((offset, value));
    }

    /// <summary>动画名（按内容生成，所有请求同名）。</summary>
    public string Name => name ??= "rk" + StableHash.Of(Body);

    /// <summary>完整的 <c>@keyframes</c> 规则。</summary>
    internal string Css => css ??= $"@keyframes {Name}{{{Body}}}";

    string Body
    {
        get
        {
            var builder = new StringBuilder();
            foreach (var (offset, frame) in frames)
            {
                builder.Append(offset).Append('{');
                foreach (var (property, value) in frame.Declarations) builder.Append(property).Append(':').Append(value).Append(';');
                builder.Append('}');
            }
            return builder.ToString();
        }
    }
}
