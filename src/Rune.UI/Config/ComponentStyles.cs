using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 合成好的样式定制（由 <see cref="ComponentConfigs"/> 的各项拆出，组件经 <see cref="UIConfig.Styles"/> 读取、配方据此追加）：
/// 按部件追加在组件自己的样式之后（外层 Provider 在前、内层在后），再加上组件当时的档位取值对应的样式；使用方的 Sx 排在它们后面。
/// 配置写在 <see cref="ComponentConfigs"/>，这里只读。
/// </summary>
public sealed class ComponentStyles
{
    readonly Dictionary<(Type Part, long Value), List<Style>> byPart = [];
    readonly Dictionary<(Type Part, long Value), List<(Enum Variant, Style Style)>> byVariant = [];

    /// <summary>不做任何定制。</summary>
    public static readonly ComponentStyles Empty = new();

    /// <summary>有按档位取值的定制：配方才去取组件的档位取值。</summary>
    internal bool HasVariants => byVariant.Count > 0;

    internal void Add<TPart>(TPart part, Style style) where TPart : struct, Enum
    {
        var key = KeyOf(part);
        if (!byPart.TryGetValue(key, out var styles)) byPart[key] = styles = [];
        styles.Add(style);
    }

    internal void AddVariant<TPart>(Enum variant, TPart part, Style style) where TPart : struct, Enum
    {
        var key = KeyOf(part);
        if (!byVariant.TryGetValue(key, out var styles)) byVariant[key] = styles = [];
        styles.Add((variant, style));
    }

    /// <summary>某个部件的定制样式，按添加顺序；没有时为空。</summary>
    public IReadOnlyList<Style> Of<TPart>(TPart part) where TPart : struct, Enum =>
        byPart.TryGetValue(KeyOf(part), out var styles) ? styles : [];

    /// <summary>某个部件在这些档位取值下的定制样式：按部件的在前，按档位取值的在后（都按添加顺序）。</summary>
    public IReadOnlyList<Style> Of<TPart>(TPart part, IReadOnlyCollection<Enum> variants) where TPart : struct, Enum
    {
        var own = Of(part);
        if (variants.Count == 0 || !byVariant.TryGetValue(KeyOf(part), out var byValue)) return own;
        var matched = byValue.Where(entry => variants.Contains(entry.Variant)).Select(entry => entry.Style).ToList();
        return matched.Count == 0 ? own : [.. own, .. matched];
    }

    /// <summary>叠上内层的定制（Provider 嵌套时）：内层的样式排在外层之后。</summary>
    public ComponentStyles Then(ComponentStyles inner)
    {
        if (inner.byPart.Count == 0 && inner.byVariant.Count == 0) return this;
        var merged = new ComponentStyles();
        foreach (var source in (ReadOnlySpan<ComponentStyles>)[this, inner])
        {
            Append(merged.byPart, source.byPart);
            Append(merged.byVariant, source.byVariant);
        }
        return merged;
    }

    static void Append<T>(Dictionary<(Type, long), List<T>> target, Dictionary<(Type, long), List<T>> source)
    {
        foreach (var (key, items) in source)
        {
            if (!target.TryGetValue(key, out var list)) target[key] = list = [];
            list.AddRange(items);
        }
    }

    static (Type, long) KeyOf<TPart>(TPart part) where TPart : struct, Enum => (typeof(TPart), Convert.ToInt64(part));
}
