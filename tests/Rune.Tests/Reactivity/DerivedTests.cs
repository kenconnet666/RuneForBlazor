namespace Rune.Tests.Reactivity;

public class DerivedTests
{
    [Fact]
    public void 第一次读取才计算_之后用缓存()
    {
        var source = new State<int>(2);
        var computations = 0;
        var doubled = new Derived<int>(() => { computations++; return source.Value * 2; });

        Assert.Equal(0, computations);
        Assert.Equal(4, doubled.Value);
        Assert.Equal(4, doubled.Value);
        Assert.Equal(1, computations);
    }

    [Fact]
    public void 依赖变化后重新计算并通知一次()
    {
        var source = new State<int>(1);
        var doubled = new Derived<int>(() => source.Value * 2);
        var observer = new RecordingObserver();
        doubled.Subscribe(observer);
        _ = doubled.Value;

        source.Value = 5;
        source.Value = 6;

        // 第二次变化时派生值已经是过期状态，不重复通知
        Assert.Equal(1, observer.Count);
        Assert.Equal(12, doubled.Value);
    }

    [Fact]
    public void 派生值可以依赖派生值()
    {
        var source = new State<int>(1);
        var doubled = new Derived<int>(() => source.Value * 2);
        var quadrupled = new Derived<int>(() => doubled.Value * 2);
        _ = quadrupled.Value;

        source.Value = 3;

        Assert.Equal(12, quadrupled.Value);
    }
}
