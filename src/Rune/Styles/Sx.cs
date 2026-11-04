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
/// </summary>
[CollectionBuilder(typeof(Sx), nameof(Create))]
public sealed class Sx : IReadOnlyList<Style>
{
    readonly Style[] styles;

    Sx(Style[] styles) => this.styles = styles;

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

    /// <summary>单个样式。</summary>
    public static implicit operator Sx(Style style) => new([style]);

    /// <summary>按顺序遍历样式。</summary>
    public IEnumerator<Style> GetEnumerator() => ((IEnumerable<Style>)styles).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
