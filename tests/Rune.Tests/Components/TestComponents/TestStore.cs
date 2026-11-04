namespace Rune.Tests.Components.TestComponents;

/// <summary>测试用的共享状态。</summary>
public sealed partial class TestStore
{
    [State] public partial int A { get; set; }

    [State] public partial int B { get; set; }
}
