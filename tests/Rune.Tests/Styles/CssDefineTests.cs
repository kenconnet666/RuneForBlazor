using Rune.Styles;

namespace Rune.Tests.Styles;

// 会清空全局的样式定义缓存，不与其他测试并行
[Collection(nameof(CssDefineTests))]
public class CssDefineTests
{
    static Style Sample => Css.Define(static () => new Style { Color = "red" });

    static Style Variant(int size) => Css.Define(size, static size => new Style { FontSize = size });

    [Fact]
    public void 定义的样式每次读取都是同一个实例()
    {
        Assert.Same(Sample, Sample);
        Assert.Same(Variant(12), Variant(12));
        Assert.NotSame(Variant(12), Variant(14));
    }

    [Fact]
    public void 热重载清缓存后重新创建()
    {
        var before = Sample;

        StyleHotReload.ClearCache(null);

        Assert.NotSame(before, Sample);
    }
}
