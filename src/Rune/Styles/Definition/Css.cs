using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Rune.Styles.StyleHotReload))]

namespace Rune.Styles;

/// <summary>
/// 定义样式的入口。样式写成静态属性，第一次读取时创建、之后一直是同一个实例（引擎按实例缓存类名）：
/// <code>
/// public static class ButtonStyles
/// {
///     public static Style Root => Css.Define(static () => new Style { Padding = Space.Sm, Color = Colors.Text });
///
///     // 按档位变化的一组部件样式（配方）：每个档位只创建一次
///     public static ButtonParts Of(ButtonSize size) => Css.Define(size, static size => new ButtonParts { … });
/// }
/// </code>
/// </summary>
/// <remarks>
/// <para>为什么不写 <c>static readonly Style Root = new() { … }</c>：静态字段只初始化一次，热重载改了样式值也不会生效。
/// 写成 Define 时样式在 lambda 的方法体里，热重载能改到它，引擎随后清掉缓存重新创建。</para>
/// <para>lambda 要写成 <c>static</c>：缓存按 lambda 本身区分，捕获的外部变量不参与区分。</para>
/// </remarks>
public static class Css
{
    static readonly ConcurrentDictionary<MethodInfo, object> defined = new();
    static readonly ConcurrentDictionary<(MethodInfo Method, object Key), object> definedByKey = new();

    /// <summary>定义一段样式（或任意一组样式），第一次调用时创建并缓存。</summary>
    public static T Define<T>(Func<T> define) where T : class =>
        (T)defined.GetOrAdd(define.Method, _ => define());

    /// <summary>按键定义：每个键创建一次（档位 → 部件样式）。</summary>
    public static T Define<TKey, T>(TKey key, Func<TKey, T> define) where TKey : notnull where T : class =>
        (T)definedByKey.GetOrAdd((define.Method, key), _ => define(key));

    /// <summary>定义标记：<c>Css.Marker("rune-listbox-option")</c>，类名就是这个名字（同 vue-tsx-pro 的 defineMarker），用连字符小写、带组件库前缀，避免与使用方的类重名。</summary>
    public static Marker Marker(string name) =>
        System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z][a-z0-9-]*$") ? new(name) : throw new ArgumentException($"标记名要是连字符小写的类名，收到 {name}", nameof(name));

    /// <summary>
    /// 回退值：同一个属性写几条声明，浏览器用最后一个认识的（同 vue-tsx-pro 的数组写法）。
    /// <c>Height = Css.Fallback("100vh", "100dvh")</c> 输出 <c>height:100vh;height:100dvh</c>。
    /// </summary>
    public static string Fallback(params string[] values) => string.Join(FallbackSeparator, values);

    /// <summary>回退值之间的分隔（控制字符，不会出现在 CSS 值里）；编译时拆成几条声明。</summary>
    internal const char FallbackSeparator = (char)1;

    /// <summary>清掉定义缓存，之后的读取重新创建样式。</summary>
    internal static void ClearDefinitions()
    {
        defined.Clear();
        definedByKey.Clear();
        StyleCompiler.ClearContentCache();
    }
}

/// <summary>
/// 热重载：.NET 在应用了代码改动后调用 <see cref="ClearCache"/>（按约定的方法名查找，见 <see cref="MetadataUpdateHandlerAttribute"/>）。
/// 清掉样式定义，随后的重新渲染创建新样式、算出新类名，界面上的状态不受影响。
/// </summary>
internal static class StyleHotReload
{
    /// <summary>热重载入口。</summary>
    public static void ClearCache(Type[]? updatedTypes) => Css.ClearDefinitions();
}
