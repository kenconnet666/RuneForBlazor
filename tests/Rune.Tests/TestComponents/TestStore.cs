namespace Rune.Tests.TestComponents;

/// <summary>测试用的共享状态。</summary>
public sealed partial class TestStore
{
    [State] public partial int A { get; set; }

    [State] public partial int B { get; set; }
}

/// <summary>测试用的级联值（record，按值比较）。</summary>
public sealed record CascadeContext(string Name);
