using Rune.Styles;

namespace Rune;

public sealed partial class Attrs
{
    /// <summary>
    /// 合并三层属性：组件的默认层 &lt; 使用方传入的 &lt; 组件管理的（状态、无障碍属性，使用方不能改）。
    /// <list type="bullet">
    /// <item><description>样式（<see cref="Sx"/>）：按层的顺序叠加，后面的覆盖前面的同名属性。几层各有样式时按层分段（<see cref="Sx.Layers"/>）：
    /// 每段一个类、各进一层级联层，结果与合成一个类相同；只有一层有样式（最常见）时一个类；</description></item>
    /// <item><description>class、style：依次拼接；</description></item>
    /// <item><description>事件（on 开头）：都执行，按层的顺序；每个处理器由写它的组件执行，执行后那个组件照常重新渲染；</description></item>
    /// <item><description>aria-describedby：各层的 id 拼在一起、去掉重复的；</description></item>
    /// <item><description>嵌套的部件属性（值是 <see cref="Attrs"/> 的 PropsXxx）：按这里的规则再合并一次，不整个替换；</description></item>
    /// <item><description>其他属性：后面的层覆盖前面的；值是 null 的不表态，留着前面几层的。</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// 只能在 <see cref="RuneComponent"/> 的渲染中调用（要把样式规则登记到当前作用域）。
    /// 每个元素每次渲染都会调用：只有默认层样式时直接返回缓存的结果，不分配。
    /// </remarks>
    public static Attrs Merge(Attrs? defaults, Attrs? user = null, Attrs? managed = null)
    {
        var anchor = AnchorOf(defaults, user, managed);
        if (anchor?.mergeCache is { } hit && hit.Matches(MergeKind.Element, defaults, user, managed))
        {
            Register(hit.Compiled);
            return hit.Result;
        }

        var compiled = CompileLayers(defaults, user, managed);
        Register(compiled);

        Attrs result;
        // 最常见：几层都只有样式（部件的默认样式）→ 每组样式对应一个固定的结果
        if (defaults?.values is null && user?.values is null && managed?.values is null)
            result = compiled.Count switch
            {
                0 => Empty,
                1 => ClassOnlyCache.Get(compiled[0]),
                _ => new Attrs { values = new Dictionary<string, object>(1, StringComparer.OrdinalIgnoreCase) { ["class"] = ClassNames(compiled) } },
            };
        else
        {
            var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            MergeInto(merged, defaults);
            MergeInto(merged, user);
            MergeInto(merged, managed);
            if (compiled.Count > 0)
                merged["class"] = merged.TryGetValue("class", out var extra) ? $"{extra} {ClassNames(compiled)}" : ClassNames(compiled);
            result = new Attrs { values = merged };
        }
        anchor?.mergeCache = new MergeEntry(MergeKind.Element, defaults, user, managed, result, compiled);
        return result;
    }

    /// <summary>
    /// 部件是本库的组件（按钮里的图标、加载指示）时用：规则同 <see cref="Merge"/>，但样式不在这里合成类，
    /// 而是作为子组件的 <c>Sx</c> 参数交下去（几层的分段保留着），子组件把它排在自己的样式之后合并。
    /// <code>&lt;Icon Data="Icon" @attributes="Attrs.MergeProps(parts[ButtonPart.Icon], PropsIcon)" /&gt;</code>
    /// </summary>
    public static Attrs MergeProps(Attrs? defaults, Attrs? user = null, Attrs? managed = null)
    {
        // 同样的几层返回同一个结果：子组件按引用比较 Sx 参数，新建的 Sx 会让它每次都跟着重新渲染
        var anchor = AnchorOf(defaults, user, managed);
        if (anchor?.mergeCache is { } hit && hit.Matches(MergeKind.Props, defaults, user, managed)) return hit.Result;

        var sx = Sx.Layers(defaults?.Sx, user?.Sx, managed?.Sx);
        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        MergeInto(merged, defaults);
        MergeInto(merged, user);
        MergeInto(merged, managed);
        // 键名与 RuneComponent.Sx 参数同名：展开到组件上时 Blazor 按名字设置这个参数
        if (sx is not null) merged[nameof(Sx)] = sx;
        var result = new Attrs { values = merged };
        anchor?.mergeCache = new MergeEntry(MergeKind.Props, defaults, user, managed, result, []);
        return result;
    }

    /// <summary>
    /// 把几层合成一层、不合成类（样式留在 <see cref="Sx"/> 上）：交给子组件的 Props 参数、由子组件再合并到它的元素上时用
    /// （列表框是 ScrollArea 的视口：<c>PropsViewport="Attrs.Layer(parts[ListboxPart.Root], Rest, managed)"</c>）。
    /// 展开到组件上用 <see cref="MergeProps"/>（样式成了 Sx 参数），合并到元素上用 <see cref="Merge"/>。
    /// </summary>
    public static Attrs Layer(Attrs? defaults, Attrs? user = null, Attrs? managed = null)
    {
        var anchor = AnchorOf(defaults, user, managed);
        if (anchor?.mergeCache is { } hit && hit.Matches(MergeKind.Layer, defaults, user, managed)) return hit.Result;

        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        MergeInto(merged, defaults);
        MergeInto(merged, user);
        MergeInto(merged, managed);
        var result = new Attrs
        {
            Sx = Sx.Layers(defaults?.Sx, user?.Sx, managed?.Sx),
            values = merged.Count == 0 ? null : merged,
        };
        anchor?.mergeCache = new MergeEntry(MergeKind.Layer, defaults, user, managed, result, []);
        return result;
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

    /// <summary>
    /// 合并结果记在哪一层上：最靠后的非空层（受管层多半是 Memo 住的、使用方层是参数没变就不变的 Rest，部件的默认层按配方缓存）。
    /// 空层（<see cref="Empty"/>，没有使用方属性时的 Rest）是共用的，记在它上面会被各处互相覆盖，跳过。
    /// </summary>
    static Attrs? AnchorOf(Attrs? defaults, Attrs? user, Attrs? managed) =>
        !IsBlank(managed) ? managed : !IsBlank(user) ? user : !IsBlank(defaults) ? defaults : null;

    static bool IsBlank(Attrs? layer) => layer is null || (layer.values is null && layer.Sx is null);

    /// <summary>几层的样式按层分段编译（只有一层带样式时就是它自己的分段，渲染里最常见）。</summary>
    static IReadOnlyList<CompiledStyle> CompileLayers(Attrs? defaults, Attrs? user, Attrs? managed) =>
        Sx.Layers(defaults?.Sx, user?.Sx, managed?.Sx) is { } sx ? StyleCompiler.CompileSegments(sx.Segments) : [];

    static void Register(IReadOnlyList<CompiledStyle> compiled)
    {
        foreach (var style in compiled) StyleRegistry.Register(style);
    }

    static string ClassNames(IReadOnlyList<CompiledStyle> compiled) =>
        compiled.Count == 1 ? compiled[0].ClassName : string.Join(' ', compiled.Select(style => style.ClassName));

    /// <summary>算出一组样式的类名并登记规则（直接展开一层 Attrs 时用）。</summary>
    static string? CompileClass(Sx sx)
    {
        var compiled = StyleCompiler.CompileSegments(sx.Segments);
        if (compiled.Count == 0) return null;
        Register(compiled);
        return ClassNames(compiled);
    }

    static void MergeInto(Dictionary<string, object> merged, Attrs? layer)
    {
        if (layer?.values is not { } values) return;
        foreach (var (name, value) in values)
        {
            // 值是 null 的属性不表态（使用方写了 title="@error"、error 是 null 时），留着前面几层的
            if (value is null) continue;
            if (!merged.TryGetValue(name, out var earlier))
            {
                merged[name] = value;
                continue;
            }
            merged[name] = name.ToLowerInvariant() switch
            {
                "class" => $"{earlier} {value}",
                "style" => $"{earlier};{value}",
                "aria-describedby" => JoinIds(earlier, value),
                var key when IsEventHandler(key) => EventHandlerMerger.Combine(earlier, value),
                // 嵌套的部件属性（PropsIcon……）按同样的规则再合并，不整个替换：组件给子部件的默认不因为使用方也写了这个部件就丢掉
                _ when earlier is Attrs inner && value is Attrs outer && IsPartProps(name) => Layer(inner, outer),
                _ => value,
            };
        }
    }

    /// <summary>几层都写了 aria-describedby 时 id 拼在一起、去掉重复的：Field 的说明、错误与使用方自己写的提示都念得到。</summary>
    static object JoinIds(object earlier, object value)
    {
        if (earlier is not string first || value is not string second) return value;
        var ids = new List<string>();
        foreach (var id in $"{first} {second}".Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (!ids.Contains(id)) ids.Add(id);
        return string.Join(' ', ids);
    }

    // PropsIcon、PropsRemove：Props 后面跟大写字母，值是另一个部件的属性
    static bool IsPartProps(string name) => name.Length > 5 && name.StartsWith("Props", StringComparison.Ordinal) && char.IsAsciiLetterUpper(name[5]);

    // onclick、onkeydown……；“on”开头但不是事件的属性（如 one-time）不会以处理器为值
    static bool IsEventHandler(string name) => name.Length > 2 && name.StartsWith("on", StringComparison.Ordinal);

    enum MergeKind
    {
        Element,
        Props,
        Layer,
    }

    /// <summary>
    /// 上一次以这一层为锚的合并：几层是同样的对象（Attrs 创建后不变）时结果也一样，直接复用，不再收集样式、建字典。
    /// 条目不可变、整体替换：静态的默认层被几个电路同时用时，读到的总是某一次完整的结果。
    /// </summary>
    sealed record MergeEntry(MergeKind Kind, Attrs? Defaults, Attrs? User, Attrs? Managed, Attrs Result, IReadOnlyList<CompiledStyle> Compiled)
    {
        public bool Matches(MergeKind kind, Attrs? defaults, Attrs? user, Attrs? managed) =>
            Kind == kind && ReferenceEquals(Defaults, defaults) && ReferenceEquals(User, user) && ReferenceEquals(Managed, managed);
    }

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
