namespace Rune;

/// <summary>
/// 把 partial 属性声明成响应式状态，生成器补出实现，读写像普通属性一样（不用 .Value）：
/// <code>
/// [State] public partial int Count { get; set; }
/// [State("未命名")] public partial string Name { get; private set; }
/// </code>
/// 生成的内容：懒创建的 <see cref="State{T}"/>（热重载新增的状态在已有实例上也能用）、读写代理，
/// 以及交出信号本身的 <c>CountState</c> 属性（给子组件、<see cref="Bindable{T}"/> 这类需要信号的地方）。
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>初始值写常量参数 <c>[State(5)]</c>；复杂的初始值在构造函数或 OnInitialized 里赋值。</description></item>
/// <item><description>只能写在 .cs / .razor.cs 里：生成器看不到 .razor 的 @code（写在那里会报 RUNE003）。</description></item>
/// <item><description>不能是静态的（RUNE001）：Blazor Server 下静态字段被所有用户共享，共享状态放进 scoped 服务。</description></item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class StateAttribute : Attribute
{
    /// <summary>初始值为类型默认值。</summary>
    public StateAttribute() { }

    /// <summary>指定初始值（编译期常量）。</summary>
    public StateAttribute(object? initial) => Initial = initial;

    /// <summary>初始值；生成器直接取源码里的参数表达式，这里只为反射查看。</summary>
    public object? Initial { get; }
}
