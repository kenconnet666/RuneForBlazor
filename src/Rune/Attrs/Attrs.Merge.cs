using Rune.Styles;

namespace Rune;

public sealed partial class Attrs
{
    /// <summary>
    /// 合并三层属性：组件的默认层 &lt; 使用方传入的 &lt; 组件管理的（状态、无障碍属性，使用方不能改）。
    /// <list type="bullet">
    /// <item><description>样式（<see cref="Sx"/>）：按层的顺序叠加，后面的覆盖前面的同名属性；合成一个类；</description></item>
    /// <item><description>class、style：依次拼接；</description></item>
    /// <item><description>事件（on 开头）：都执行，按层的顺序；每个处理器由写它的组件执行，执行后那个组件照常重新渲染；</description></item>
    /// <item><description>其他属性：后面的层覆盖前面的。</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// 只能在 <see cref="RuneComponent"/> 的渲染中调用（要把样式规则登记到当前作用域）。
    /// 每个元素每次渲染都会调用：只有默认层样式时直接返回缓存的结果，不分配。
    /// </remarks>
    public static Attrs Merge(Attrs? defaults, Attrs? user = null, Attrs? managed = null)
    {
        var styles = CollectStyles(defaults, user, managed);
        var compiled = StyleCompiler.Compile(styles);
        if (compiled is not null) StyleRegistry.Register(compiled);

        // 最常见：几层都只有样式（部件的默认样式）→ 每组样式对应一个固定的结果
        if (defaults?.values is null && user?.values is null && managed?.values is null)
            return compiled is null ? Empty : ClassOnlyCache.Get(compiled);

        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        MergeInto(merged, defaults);
        MergeInto(merged, user);
        MergeInto(merged, managed);
        if (compiled is not null)
            merged["class"] = merged.TryGetValue("class", out var extra) ? $"{extra} {compiled.ClassName}" : compiled.ClassName;
        return new Attrs { values = merged };
    }

    /// <summary>
    /// 部件是本库的组件（按钮里的图标、加载指示）时用：规则同 <see cref="Merge"/>，但样式不在这里合成类，
    /// 而是作为子组件的 <c>Sx</c> 参数交下去，子组件把它排在自己的样式之后、只合成一个类
    /// （两个类各自的规则谁先插入谁就输，结果不可靠）。
    /// <code>&lt;Icon Data="Icon" @attributes="Attrs.MergeProps(parts[ButtonPart.Icon], PropsIcon)" /&gt;</code>
    /// </summary>
    public static Attrs MergeProps(Attrs? defaults, Attrs? user = null, Attrs? managed = null)
    {
        var styles = CollectStyles(defaults, user, managed);
        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        MergeInto(merged, defaults);
        MergeInto(merged, user);
        MergeInto(merged, managed);
        // 键名与 RuneComponent.Sx 参数同名：展开到组件上时 Blazor 按名字设置这个参数
        if (styles.Count > 0) merged[nameof(Sx)] = styles as Sx ?? Sx.Create([.. styles]);
        return new Attrs { values = merged };
    }

    /// <summary>去掉某些属性（不可用、加载中时去掉使用方的点击处理）。</summary>
    public Attrs Without(params ReadOnlySpan<string> names)
    {
        if (values is null) return this;
        Dictionary<string, object>? kept = null;
        foreach (var name in names)
        {
            if (!values.ContainsKey(name)) continue;
            kept ??= new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);
            kept.Remove(name);
        }
        return kept is null ? this : new Attrs { Sx = Sx, values = kept };
    }

    /// <summary>按层的顺序收集样式；只有一层带样式时直接用它（渲染里最常见，不分配）。</summary>
    static IReadOnlyList<Style> CollectStyles(Attrs? defaults, Attrs? user, Attrs? managed)
    {
        Sx? only = null;
        var layersWithStyles = 0;
        foreach (var layer in (ReadOnlySpan<Attrs?>)[defaults, user, managed])
        {
            if (layer?.Sx is not { Count: > 0 } sx) continue;
            only = sx;
            layersWithStyles++;
        }
        if (layersWithStyles <= 1) return only ?? (IReadOnlyList<Style>)[];

        var styles = new List<Style>();
        foreach (var layer in (ReadOnlySpan<Attrs?>)[defaults, user, managed])
            if (layer?.Sx is { } sx) styles.AddRange(sx);
        return styles;
    }

    /// <summary>算出一组样式的类名并登记规则（直接展开一层 Attrs 时用）。</summary>
    static string? CompileClass(Sx sx)
    {
        var compiled = StyleCompiler.Compile(sx);
        if (compiled is null) return null;
        StyleRegistry.Register(compiled);
        return compiled.ClassName;
    }

    static void MergeInto(Dictionary<string, object> merged, Attrs? layer)
    {
        if (layer?.values is not { } values) return;
        foreach (var (name, value) in values)
        {
            if (!merged.TryGetValue(name, out var earlier))
            {
                merged[name] = value;
                continue;
            }
            merged[name] = name.ToLowerInvariant() switch
            {
                "class" => $"{earlier} {value}",
                "style" => $"{earlier};{value}",
                var key when IsEventHandler(key) => EventHandlerMerger.Combine(earlier, value),
                _ => value,
            };
        }
    }

    // onclick、onkeydown……；“on”开头但不是事件的属性（如 one-time）不会以处理器为值
    static bool IsEventHandler(string name) => name.Length > 2 && name.StartsWith("on", StringComparison.Ordinal);

    /// <summary>合并后只有一个类名的结果，按样式结果缓存（同一组样式每次渲染返回同一个对象）。</summary>
    static class ClassOnlyCache
    {
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CompiledStyle, Attrs> cache = new();

        public static Attrs Get(CompiledStyle compiled) =>
            cache.GetValue(compiled, static c => new Attrs
            {
                values = new Dictionary<string, object>(1, StringComparer.OrdinalIgnoreCase) { ["class"] = c.ClassName },
            });
    }
}
