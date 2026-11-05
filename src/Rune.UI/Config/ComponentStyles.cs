using System.Collections;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 按部件定制组件的样式：追加在组件自己的样式之后（外层 Provider 在前、内层在后），使用方的 Sx 再排在它们后面。
/// 部件用各组件的部件枚举指定，写错组件或部件编译不过：
/// <code>
/// o.Styles = new() { { CollapsiblePart.Trigger, AppStyles.BoldTrigger } };
/// </code>
/// 只在创建时添加，之后不再修改（同一个实例会被缓存引用）。
/// </summary>
public sealed class ComponentStyles : IEnumerable
{
    readonly Dictionary<(Type Part, long Value), List<Style>> byPart = [];

    /// <summary>不做任何定制。</summary>
    public static readonly ComponentStyles Empty = new();

    /// <summary>给某个部件追加一段样式（集合初始化器用）。</summary>
    public void Add<TPart>(TPart part, Style style) where TPart : struct, Enum
    {
        var key = KeyOf(part);
        if (!byPart.TryGetValue(key, out var styles)) byPart[key] = styles = [];
        styles.Add(style);
    }

    /// <summary>某个部件的定制样式，按添加顺序；没有时为空。</summary>
    public IReadOnlyList<Style> Of<TPart>(TPart part) where TPart : struct, Enum =>
        byPart.TryGetValue(KeyOf(part), out var styles) ? styles : [];

    /// <summary>叠上内层的定制（Provider 嵌套时）：内层的样式排在外层之后。</summary>
    public ComponentStyles Then(ComponentStyles inner)
    {
        if (inner.byPart.Count == 0) return this;
        var merged = new ComponentStyles();
        foreach (var source in (ReadOnlySpan<ComponentStyles>)[this, inner])
            foreach (var (key, styles) in source.byPart)
            {
                if (!merged.byPart.TryGetValue(key, out var list)) merged.byPart[key] = list = [];
                list.AddRange(styles);
            }
        return merged;
    }

    static (Type, long) KeyOf<TPart>(TPart part) where TPart : struct, Enum => (typeof(TPart), Convert.ToInt64(part));

    IEnumerator IEnumerable.GetEnumerator() => byPart.GetEnumerator();
}
