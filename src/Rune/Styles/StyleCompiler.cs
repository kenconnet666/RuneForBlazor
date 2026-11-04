using System.Collections.Concurrent;
using System.Text;

namespace Rune.Styles;

/// <summary>一组样式合并后的结果：一个类名与它的 CSS 规则。</summary>
/// <param name="ClassName">类名，按内容哈希（同样的样式在服务端、浏览器、所有请求里都同名）。</param>
/// <param name="Css">这个类的全部规则（基础块与各条件块）。</param>
internal sealed record CompiledStyle(string ClassName, string Css);

/// <summary>
/// 样式引擎：把按顺序排列的几段样式合成一个类。
/// </summary>
/// <remarks>
/// <para>合并：同一“条件 + 属性”后写的覆盖先写的；条件块之间互不影响（只改悬停时的颜色不会清掉默认颜色）。</para>
/// <para>
/// 缓存分两层：
/// <list type="number">
/// <item><description>按样式实例：结果挂在第一段样式上，按“后面跟着哪几段”区分。渲染时用的都是 <see cref="Css.Define"/> 定义的同一批实例，
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

    /// <summary>挂在每个 <see cref="Style"/> 实例上的缓存。</summary>
    internal sealed class InstanceCache
    {
        /// <summary>只有这一段样式时的结果。</summary>
        public CompiledStyle? Alone;

        /// <summary>后面还跟着别的样式时的结果。</summary>
        public ConcurrentDictionary<Followers, CompiledStyle>? WithFollowers;
    }

    /// <summary>跟在第一段后面的样式，按引用比较（Style 没有重写 Equals）。</summary>
    internal readonly record struct Followers(Style? A, Style? B, Style? C, Style? D, Style? E);

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
        // 条件链（"" 表示无条件）→ 属性 → 值；后写的覆盖先写的
        var blocks = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var style in styles) Flatten(style, "", blocks);
        if (blocks.Count == 0) return null;

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
        var key = canonical.ToString();
        if (byContent.TryGetValue(key, out var hit)) return hit;

        if (byContent.Count >= ContentCacheLimit) byContent.Clear();
        var className = "r" + StableHash.Of(key);
        var compiled = new CompiledStyle(className, BuildCss(className, orderedBlocks.Select(b => (b.Chain, b.Declarations))));
        return byContent.GetOrAdd(key, compiled);
    }

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
                css.Append(property).Append(':').Append(value).Append(';');
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
