using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Rune.Styles;

/// <summary>一组样式合并后的结果：一个类名与它的 CSS 规则。</summary>
/// <param name="className">类名，按内容哈希（同样的样式在服务端、浏览器、所有请求里都同名）。</param>
/// <param name="css">这个类的全部规则（基础块与各条件块）。</param>
internal sealed class CompiledStyle(string className, string css)
{
    /// <summary>类名。</summary>
    public string ClassName { get; } = className;

    /// <summary>规则文本。</summary>
    public string Css { get; } = css;

    /// <summary>
    /// 最近一次登记进的登记表：同一个作用域里每次渲染都要登记一遍，是它就不用再加锁查表。
    /// 只是提示：几个作用域交替时被覆盖，照常查表（登记本身是幂等的）。
    /// </summary>
    internal StyleRegistry? LastRegistry;
}

/// <summary>
/// 样式引擎：把按顺序排列的几段样式合成一个类；按合并来源分段时（<see cref="CompileSegments"/>）每段一个类，各进一层级联层。
/// </summary>
/// <remarks>
/// <para>
/// 级联层（同 vue-tsx-pro 的分段输出）：每块样式表开头声明 <see cref="LayerOrder"/>，基础规则在 <c>rune.base</c>，主题变量在 <c>rune.vars</c>，第 k 段的规则在 <c>rune.s{k}</c>，
/// 后面的段不论特异性都盖过前面的段。第 k 段（k &gt; 0）输出它改到的属性所在的属性族（<see cref="FamilyOf"/>）在前 k + 1 段合并后的完整取值，
/// 含它没改的条件值：每个属性族最高的那一层里总有这一族完整的合并结果，层里排出的规则与合成一个类时相同，语义逐字一致。
/// 简写与长属性分在不同的段时靠属性族一起带（前段写 <c>outline</c>、后段写 <c>outline-color</c>，后段带上那条简写，顺序照常）。
/// 合并只在同一条件链里覆盖（含简写去掉被它重设的长属性，<see cref="MergeFragment"/>），前 k + 1 段的合并结果里没有的低层取值也不会漏上来，不需要 vue-tsx-pro 那样补 <c>revert</c>。
/// </para>
/// <para>合并：同一“条件 + 属性”后写的覆盖先写的，后写的简写去掉同一条件里先写的长属性；条件块之间互不影响（只改悬停时的颜色不会清掉默认颜色）。</para>
/// <para>
/// 缓存分两层：
/// <list type="number">
/// <item><description>按样式实例：结果挂在第一段样式上，按“后面跟着哪几段”区分（分段时还按“每段从哪里开始”）。渲染时用的都是 <see cref="Css.Define{T}(Func{T})"/> 定义的同一批实例，
/// 命中时不做任何计算；实例被回收（渲染里临时 new 的样式），缓存跟着回收，不会越积越多。</description></item>
/// <item><description>按内容：实例缓存没命中时，展开声明、拼出规范文本再查。同样的内容只生成一次规则。</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class StyleCompiler
{
    /// <summary>实例缓存最多认几段样式（第一段 + 后面 5 段）；更长的组合直接走内容缓存。</summary>
    const int MaxFollowers = 5;

    /// <summary>内容缓存的上限：动态值（每次不同的宽度）写进样式会让它一直增长，到了上限就清空重来。</summary>
    const int ContentCacheLimit = 10_000;

    static readonly ConcurrentDictionary<string, CompiledStyle> byContent = new();

    /// <summary>分段的上限：超出的并进最后一段。</summary>
    internal const int MaxSegments = 8;

    /// <summary>级联层的顺序：每块样式表最前面声明（重复声明同样的顺序不改变什么）。</summary>
    internal static readonly string LayerOrder =
        $"@layer {BaseLayer},{VarsLayer}," + string.Join(',', Enumerable.Range(0, MaxSegments).Select(k => $"rune.s{k}")) + ";";

    /// <summary>基础规则（<see cref="GlobalRule"/>）所在的层：最前面，什么都盖得过它。</summary>
    internal const string BaseLayer = "rune.base";

    /// <summary>主题变量所在的层。</summary>
    internal const string VarsLayer = "rune.vars";

    /// <summary>挂在每个 <see cref="Style"/> 实例上的缓存。</summary>
    internal sealed class InstanceCache
    {
        /// <summary>只有这一段样式时的结果。</summary>
        public CompiledStyle? Alone;

        /// <summary>后面还跟着别的样式时的结果。</summary>
        public ConcurrentDictionary<Followers, CompiledStyle>? WithFollowers;

        /// <summary>这个样式打头、分成几段时各段的结果（<see cref="CompileSegments"/>）。</summary>
        public ConcurrentDictionary<SegmentsKey, IReadOnlyList<CompiledStyle>>? Segmented;
    }

    /// <summary>跟在第一段后面的样式，按引用比较（Style 没有重写 Equals）。</summary>
    internal readonly record struct Followers(Style? A, Style? B, Style? C, Style? D, Style? E);

    /// <summary>分段的实例缓存最多认几个样式（各段合起来，含打头的那个）；更多的每次重算。</summary>
    const int MaxSegmented = 8;

    /// <summary>
    /// 分成几段的一组样式里打头那个之后的样式（按引用比较），与每段从第几个样式开始（<paramref name="Starts"/> 的第 i 位：第 i 个样式是一段的开头）。
    /// </summary>
    internal readonly record struct SegmentsKey(Style? A, Style? B, Style? C, Style? D, Style? E, Style? F, Style? G, int Starts);

    /// <summary>
    /// 按合并来源分段编译：每段一个类（没有声明的段不出类），第 k 段在层 <c>rune.s{k}</c>。只有一段时同 <see cref="Compile"/>。
    /// </summary>
    public static IReadOnlyList<CompiledStyle> CompileSegments(IReadOnlyList<IReadOnlyList<Style>> segments)
    {
        if (segments.Count > MaxSegments)
            segments = [.. segments.Take(MaxSegments - 1), [.. segments.Skip(MaxSegments - 1).SelectMany(segment => segment)]];
        if (segments.Count == 1) return Compile(segments[0]) is { } single ? [single] : [];

        // 几层各有样式的元素（部件的默认样式 + 使用方或组件管理的样式）每次合并都走到这里：整组结果按样式实例缓存，同 Compile。
        // 不缓存的话每次都要把各段展开、按属性族重算一遍（DataTable 的文档页一次渲染 7800 次，占掉 1.4 秒）
        if (!TryKeyOf(segments, out var first, out var key)) return CompileSegmentsByContent(segments);
        var cache = first.Compiled.Segmented ??= new ConcurrentDictionary<SegmentsKey, IReadOnlyList<CompiledStyle>>();
        if (cache.TryGetValue(key, out var hit)) return hit;
        var compiled = CompileSegmentsByContent(segments);
        cache.TryAdd(key, compiled);
        return compiled;
    }

    // 有空段的、样式太多的不缓存：段的序号决定它在哪一层，空段不能略过
    static bool TryKeyOf(IReadOnlyList<IReadOnlyList<Style>> segments, [NotNullWhen(true)] out Style? first, out SegmentsKey key)
    {
        first = null;
        key = default;
        var flat = new Style?[MaxSegmented];
        var count = 0;
        var starts = 0;
        foreach (var segment in segments)
        {
            if (segment.Count == 0 || count + segment.Count > MaxSegmented) return false;
            starts |= 1 << count;
            foreach (var style in segment) flat[count++] = style;
        }
        first = flat[0]!;
        key = new SegmentsKey(flat[1], flat[2], flat[3], flat[4], flat[5], flat[6], flat[7], starts);
        return true;
    }

    static IReadOnlyList<CompiledStyle> CompileSegmentsByContent(IReadOnlyList<IReadOnlyList<Style>> segments)
    {
        var result = new List<CompiledStyle>(segments.Count);
        for (var index = 0; index < segments.Count; index++)
        {
            var compiled = index == 0 ? Compile(segments[0]) : CompileSegment(segments, index);
            if (compiled is not null) result.Add(compiled);
        }
        return result;
    }

    /// <summary>
    /// 第 index 段（index &gt; 0）的声明：前 index + 1 段合并后，属于这一段改到的属性族的那些（含它没改的条件值）。
    /// </summary>
    internal static Dictionary<string, Dictionary<string, string>> SegmentBlocks(IReadOnlyList<IReadOnlyList<Style>> segments, int index)
    {
        var own = FlattenAll(segments[index]);
        var families = own.Values.SelectMany(declarations => declarations.Keys).Select(FamilyOf).ToHashSet(StringComparer.Ordinal);
        var prefix = FlattenAll(segments.Take(index + 1).SelectMany(segment => segment));
        var blocks = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (chain, declarations) in prefix)
        {
            var kept = declarations.Where(d => families.Contains(FamilyOf(d.Key))).ToDictionary(StringComparer.Ordinal);
            if (kept.Count > 0) blocks[chain] = kept;
        }
        return blocks;
    }

    static readonly Dictionary<string, string> familyRoots = new(StringComparer.Ordinal)
    {
        ["top"] = "inset", ["right"] = "inset", ["bottom"] = "inset", ["left"] = "inset",
        ["row-gap"] = "gap", ["column-gap"] = "gap", ["align"] = "place", ["justify"] = "place",
        ["columns"] = "column", ["line-height"] = "font", ["white"] = "text",
    };

    /// <summary>
    /// 属性族（同 vue-tsx-pro 的 familyOf）：有简写、长属性关系的属性归同一族，按名字的第一段分（border、margin、outline、font……）；
    /// 名字对不上的几个例外：inset 与 top / right / bottom / left，gap 与 row-gap / column-gap，place 与 align / justify，
    /// columns 归 column，line-height 归 font（font 简写会重设它），white-space 归 text（text-wrap-mode 的简写）。自定义属性各自一族。
    /// 分得粗没关系（只是多带几条声明），分得细会出错。
    /// </summary>
    internal static string FamilyOf(string property)
    {
        if (property.StartsWith("--", StringComparison.Ordinal)) return property;
        // 从第二个字符找：带厂商前缀的（-webkit-line-clamp）按前缀归族
        var dash = property.IndexOf('-', 1);
        var root = dash < 0 ? property : property[..dash];
        return familyRoots.GetValueOrDefault(property) ?? familyRoots.GetValueOrDefault(root) ?? root;
    }

    static CompiledStyle? CompileSegment(IReadOnlyList<IReadOnlyList<Style>> segments, int index)
    {
        var blocks = SegmentBlocks(segments, index);
        if (blocks.Count == 0) return null;
        // 带进来的动画名（animation 这一族）引用的关键帧：前面各段用到的都随这个类输出
        var keyframes = blocks.Values.Any(block => block.ContainsKey("animation-name"))
            ? segments.Take(index + 1).SelectMany(segment => segment).SelectMany(AllKeyframes)
            : [];
        return CompileBlocks(blocks, index, keyframes);
    }

    /// <summary>合成一个类；没有任何声明时返回 null。</summary>
    public static CompiledStyle? Compile(IReadOnlyList<Style> styles)
    {
        if (styles.Count == 0) return null;
        var first = styles[0];
        if (styles.Count == 1) return first.Compiled.Alone ??= CompileByContent(styles);
        if (styles.Count > MaxFollowers + 1) return CompileByContent(styles);

        var followers = new Followers(At(1), At(2), At(3), At(4), At(5));
        var cache = first.Compiled.WithFollowers ??= new ConcurrentDictionary<Followers, CompiledStyle>();
        if (cache.TryGetValue(followers, out var hit)) return hit;
        var compiled = CompileByContent(styles);
        if (compiled is not null) cache.TryAdd(followers, compiled);
        return compiled;

        Style? At(int index) => index < styles.Count ? styles[index] : null;
    }

    /// <summary>清空内容缓存（测试与热重载用）。实例缓存跟着实例走，不用清。</summary>
    internal static void ClearContentCache() => byContent.Clear();

    static CompiledStyle? CompileByContent(IReadOnlyList<Style> styles)
    {
        var blocks = FlattenAll(styles);
        return blocks.Count == 0 ? null : CompileBlocks(blocks, 0, styles.SelectMany(AllKeyframes));
    }

    // 条件链（"" 表示无条件）→ 属性 → 值；后写的覆盖先写的
    static Dictionary<string, Dictionary<string, string>> FlattenAll(IEnumerable<Style> styles)
    {
        var blocks = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var style in styles)
        {
            if (blocks.Count == 0)
            {
                Flatten(style, "", blocks);
                continue;
            }
            var fragment = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            Flatten(style, "", fragment);
            MergeFragment(blocks, fragment);
        }
        return blocks;
    }

    /// <summary>
    /// 后面的样式并进前面合并的结果：同一条件链里写了简写，就去掉前面写的、被它重设的长属性（同 CSS 的“后写的赢”）。
    /// 不去掉的话两者进了同一个类，长属性排在简写后面（<see cref="StylePriority.OfProperty"/>），后写的 <c>Padding = 0</c> 反而盖不过组件的 <c>PaddingInline</c>。
    /// 同一个样式里两者都写时仍是长属性赢。
    /// </summary>
    static void MergeFragment(Dictionary<string, Dictionary<string, string>> blocks, Dictionary<string, Dictionary<string, string>> fragment)
    {
        foreach (var (chain, declarations) in fragment)
        {
            if (!blocks.TryGetValue(chain, out var merged))
            {
                blocks[chain] = declarations;
                continue;
            }
            foreach (var property in declarations.Keys)
                foreach (var longhand in CssShorthands.CoveredBy(property))
                    if (!declarations.ContainsKey(longhand)) merged.Remove(longhand);
            foreach (var (property, value) in declarations) merged[property] = value;
        }
    }

    /// <summary>声明表 → 第 segment 段的类：类名按内容哈希（第 0 段与不分段时相同；后面的段带上段号，同样的规则在不同的层里是不同的类）。</summary>
    static CompiledStyle CompileBlocks(Dictionary<string, Dictionary<string, string>> blocks, int segment, IEnumerable<Keyframes> keyframes)
    {
        var orderedBlocks = blocks
            .Select(block => (Chain: block.Key, Priority: ChainPriority(block.Key), Declarations: block.Value))
            .OrderBy(block => block.Priority)
            .ThenBy(block => block.Chain, StringComparer.Ordinal)
            .ToList();

        var canonical = new StringBuilder();
        foreach (var block in orderedBlocks)
        {
            canonical.Append(block.Chain).Append('{');
            foreach (var (property, value) in Ordered(block.Declarations))
                canonical.Append(property).Append(':').Append(value).Append(';');
            canonical.Append('}');
        }
        var key = segment == 0 ? canonical.ToString() : $"{segment}|{canonical}";
        if (byContent.TryGetValue(key, out var hit)) return hit;

        if (byContent.Count >= ContentCacheLimit) byContent.Clear();
        var className = "r" + StableHash.Of(key);
        var css = $"@layer rune.s{segment}{{{BuildCss(className, orderedBlocks.Select(b => (b.Chain, b.Declarations)))}}}";
        // 引用到的关键帧随类一起输出（类名里已含动画名，内容相同的类引用的也是同一组关键帧）；放在层外，名字按内容哈希不会撞
        foreach (var used in keyframes.Distinct()) css += used.Css;
        return byContent.GetOrAdd(key, new CompiledStyle(className, css));
    }

    static IEnumerable<Keyframes> AllKeyframes(Style style) =>
        style.UsedKeyframes.Concat(style.Conditions.Values.SelectMany(AllKeyframes));

    /// <summary>把样式（含嵌套条件）展开到各条件链的声明表里。</summary>
    static void Flatten(Style style, string chain, Dictionary<string, Dictionary<string, string>> blocks)
    {
        if (style.Declarations.Count > 0)
        {
            if (!blocks.TryGetValue(chain, out var declarations))
                blocks[chain] = declarations = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (property, value) in style.Declarations) declarations[property] = value;
        }
        foreach (var (condition, nested) in style.Conditions)
            Flatten(nested, chain.Length == 0 ? condition : chain + ChainSeparator + condition, blocks);
    }

    /// <summary>条件链里各条件之间的分隔符（控制字符，不会出现在选择器里）。</summary>
    const char ChainSeparator = '\u0001';

    static int ChainPriority(string chain) =>
        chain.Length == 0 ? 0 : chain.Split(ChainSeparator).Sum(StylePriority.OfCondition);

    static IEnumerable<KeyValuePair<string, string>> Ordered(Dictionary<string, string> declarations) =>
        declarations
            .OrderBy(d => StylePriority.OfProperty(d.Key))
            .ThenBy(d => d.Key, StringComparer.Ordinal);

    /// <summary>
    /// 生成规则文本。选择器片段接在类名后面（<c>.r1a2b:hover</c>），@ 规则从外到内包在外面：
    /// <c>@media (…){.r1a2b:hover{…}}</c>。
    /// </summary>
    static string BuildCss(string className, IEnumerable<(string Chain, Dictionary<string, string> Declarations)> blocks)
    {
        var css = new StringBuilder();
        foreach (var (chain, declarations) in blocks)
        {
            var selector = "." + className;
            var atRules = new List<string>();
            foreach (var condition in chain.Split(ChainSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (condition.StartsWith('@')) atRules.Add(condition);
                else selector += condition;
            }

            foreach (var atRule in atRules) css.Append(atRule).Append('{');
            css.Append(selector).Append('{');
            foreach (var (property, value) in Ordered(declarations))
                foreach (var single in value.Split(Css.FallbackSeparator))
                    css.Append(property).Append(':').Append(single).Append(';');
            css.Append('}');
            css.Append('}', atRules.Count);
        }
        return css.ToString();
    }
}

/// <summary>跨进程稳定的字符串哈希（string.GetHashCode 每次启动都不同，不能用来起类名）。</summary>
internal static class StableHash
{
    /// <summary>FNV-1a 64 位，取低 40 位写成 base36，约 8 个字符。</summary>
    public static string Of(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        var value = hash & 0xFF_FFFF_FFFFUL;
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        var builder = new StringBuilder();
        do
        {
            builder.Insert(0, digits[(int)(value % 36)]);
            value /= 36;
        } while (value > 0);
        return builder.ToString();
    }
}
