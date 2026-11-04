namespace Rune.Docs.Client.Demo;

/// <summary>
/// 演示用的共享状态（scoped 服务）：普通类里的 [State]，读写像普通属性。
/// 组件渲染时读了哪个属性，只有那个属性变化时才重新渲染。
/// </summary>
public sealed partial class CounterStore
{
    readonly Derived<int> sum;

    /// <summary>创建状态。</summary>
    public CounterStore() => sum = new Derived<int>(() => A + B);

    /// <summary>计数 A。</summary>
    [State] public partial int A { get; set; }

    /// <summary>计数 B。</summary>
    [State] public partial int B { get; set; }

    /// <summary>A + B：派生值，A 或 B 变了才重新计算。</summary>
    public int Sum => sum.Value;
}
