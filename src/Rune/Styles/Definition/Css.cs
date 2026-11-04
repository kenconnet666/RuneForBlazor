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
