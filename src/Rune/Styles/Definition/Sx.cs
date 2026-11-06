using System.Collections;
using System.Runtime.CompilerServices;

namespace Rune.Styles;

/// <summary>
/// 按顺序合并的一组样式，后面的覆盖前面的。可以直接写单个样式、集合表达式，有可选的项时用 <see cref="Of"/>（跳过 null）：
/// <code>
/// Sx="AppStyles.Wide"
/// Sx="@([AppStyles.Wide, AppStyles.Bordered])"
/// Sx="@(Sx.Of(AppStyles.Wide, compact ? AppStyles.Compact : null))"
/// </code>
/// 来自不同合并来源的几组（组件自己的、配置的定制、使用方的）用 <see cref="Layers"/> 分段：每段一个类、各进一层级联层。
/// </summary>
[CollectionBuilder(typeof(Sx), nameof(Create))]
public sealed class Sx : IReadOnlyList<Style>
{
    readonly Style[] styles;
    // 第二段起各段在 styles 里的起点；只有一段时为 null
    readonly int[]? breaks;
    IReadOnlyList<IReadOnlyList<Style>>? segments;

    Sx(Style[] styles, int[]? breaks = null)
    {
        this.styles = styles;
        this.breaks = breaks;
    }

    /// <summary>样式的个数（不含 null）。</summary>
    public int Count => styles.Length;

    /// <summary>第 <paramref name="index"/> 段样式。</summary>
    public Style this[int index] => styles[index];

    /// <summary>按顺序组合几段样式，跳过 null（可选的样式写成 <c>条件 ? 样式 : null</c>）。</summary>
    public static Sx Of(params ReadOnlySpan<Style?> items) => Create(items);

    /// <summary>集合表达式用（也接受 null 项，跳过）。</summary>
    public static Sx Create(ReadOnlySpan<Style?> items)
    {
        var list = new List<Style>(items.Length);
        foreach (var item in items)
            if (item is not null) list.Add(item);
        return new Sx([.. list]);
    }

    /// <summary>
    /// 按合并来源分段（同 vue-tsx-pro 的 <c>css.layers</c>）：每组（连同它自己已有的分段）各合成一个类、各进一层级联层，
    /// 合并的结果与按顺序合成一个类相同，但组合数随段相加而不是相乘（组件的档位 × 配置的定制 × 使用方的样式）。
    /// 组件库在部件的默认样式与配置的定制之间、几层属性之间分段（<see cref="Attrs.Merge"/>）。空的组不算；只剩一组时就是它本身。
    /// </summary>
    public static Sx? Layers(params ReadOnlySpan<Sx?> groups)
    {
        Sx? only = null;
        var nonEmpty = 0;
        foreach (var group in groups)
        {
            if (group is not { Count: > 0 }) continue;
            only = group;
            nonEmpty++;
        }
        if (nonEmpty <= 1) return only;

        var styles = new List<Style>();
        var breaks = new List<int>();
        foreach (var group in groups)
        {
            if (group is not { Count: > 0 }) continue;
            if (styles.Count > 0) breaks.Add(styles.Count);
            foreach (var start in group.breaks ?? []) breaks.Add(styles.Count + start);
            styles.AddRange(group.styles);
        }
        return new Sx([.. styles], [.. breaks]);
    }

    /// <summary>各段的样式，按顺序；没有分段时只有一段（全部样式）。</summary>
    internal IReadOnlyList<IReadOnlyList<Style>> Segments => segments ??= breaks is null
        ? [styles]
        : [.. Enumerable.Range(0, breaks.Length + 1).Select(i =>
            (IReadOnlyList<Style>)styles[(i == 0 ? 0 : breaks[i - 1])..(i == breaks.Length ? styles.Length : breaks[i])])];

    /// <summary>单个样式。</summary>
    public static implicit operator Sx(Style style) => new([style]);

    /// <summary>按顺序遍历样式。</summary>
    public IEnumerator<Style> GetEnumerator() => ((IEnumerable<Style>)styles).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
