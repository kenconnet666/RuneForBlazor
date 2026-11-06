using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Rune;

/// <summary>
/// 合并同一个事件的两个处理器（组件自己的与使用方的），依次执行。
/// </summary>
/// <remarks>
/// <para>不能简单包一层 lambda：Blazor 执行事件后，重新渲染的是处理器所属的组件（委托的 Target）。
/// 包出来的 lambda 不属于任何组件，结果谁都不刷新。这里让每个处理器经它自己的接收者执行，各自的组件照常刷新。</para>
/// <para>合并结果的参数类型取自原处理器（MouseEventArgs 等）：Blazor 按处理器的参数类型解析浏览器传来的事件数据。</para>
/// </remarks>
internal static class EventHandlerMerger
{
    static readonly MethodInfo combineTyped =
        typeof(EventHandlerMerger).GetMethod(nameof(CombineTyped), BindingFlags.NonPublic | BindingFlags.Static)!;

    static readonly ConcurrentDictionary<Type, MethodInfo> combineByArgsType = new();

    /// <summary>先执行 <paramref name="first"/>，再执行 <paramref name="second"/>。两者可以是委托或 EventCallback。</summary>
    public static object Combine(object first, object second)
    {
        var argsType = ArgsTypeOf(first) ?? ArgsTypeOf(second) ?? typeof(EventArgs);
        var combine = combineByArgsType.GetOrAdd(argsType, MakeCombine);
        return combine.Invoke(null, [first, second])!;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "CombineTyped 的类型参数没有 DynamicallyAccessedMembers 的要求，裁剪不会影响它。")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "类型参数是事件参数类型（引用类型），共享同一份泛型代码，不需要运行时生成。")]
    static MethodInfo MakeCombine(Type argsType) => combineTyped.MakeGenericMethod(argsType);

    static Func<TArgs, Task> CombineTyped<TArgs>(object first, object second) => async args =>
    {
        await InvokeAsync(first, args);
        await InvokeAsync(second, args);
    };

    static Task InvokeAsync<TArgs>(object handler, TArgs args) => handler switch
    {
        EventCallback<TArgs> callback => callback.InvokeAsync(args),
        EventCallback callback => callback.InvokeAsync(args),
        // 普通委托：交给它所属的组件执行（EventCallback 的接收者），执行后那个组件重新渲染
        MulticastDelegate @delegate => new EventCallback(@delegate.Target as IHandleEvent, @delegate).InvokeAsync(args),
        _ => throw new InvalidOperationException($"事件处理器的类型不支持合并：{handler.GetType()}"),
    };

    /// <summary>处理器的事件参数类型；不带参数的处理器返回 null。</summary>
    static Type? ArgsTypeOf(object handler)
    {
        var type = handler.GetType();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>))
            return type.GetGenericArguments()[0];
        if (handler is Delegate @delegate)
            return @delegate.Method.GetParameters() is [var parameter] ? parameter.ParameterType : null;
        return null;
    }
}
