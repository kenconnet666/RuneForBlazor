namespace Rune.Tests.Runtime;

public class MemoTests
{
    [Fact]
    public void 输入不变时返回同一个对象_变了才重新创建()
    {
        var memo = new Memo<(string, bool), object>();
        var created = 0;

        var first = memo.Get(("a", true), _ => { created++; return new object(); });
        var again = memo.Get(("a", true), _ => { created++; return new object(); });
        var changed = memo.Get(("a", false), _ => { created++; return new object(); });

        Assert.Same(first, again);
        Assert.NotSame(first, changed);
        Assert.Equal(2, created);
    }

    [Fact]
    public void 带状态的重载把状态交给创建函数()
    {
        var memo = new Memo<int, string>();

        Assert.Equal("1:x", memo.Get(1, "x", static (key, state) => $"{key}:{state}"));
        Assert.Equal("1:x", memo.Get(1, "y", static (key, state) => $"{key}:{state}"));
    }
}
