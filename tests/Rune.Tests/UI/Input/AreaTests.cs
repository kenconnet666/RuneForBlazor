using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>省市区：树的三级与代码前缀、直辖市补上市一级、depth 与缓存、按代码查名称。</summary>
public class AreaTests
{
    [Fact]
    public void 三级的树_代码前缀一致_直辖市补上市一级()
    {
        var tree = Area.Tree();
        Assert.Equal(34, tree.Count);
        var zhejiang = tree.Single(node => node.Code == "330000");
        var hangzhou = zhejiang.Children!.Single(node => node.Code == "330100");
        Assert.Equal("杭州市", hangzhou.Name);
        Assert.Contains(hangzhou.Children!, node => node is { Code: "330106", Name: "西湖区" });
        Assert.All(hangzhou.Children!, node => Assert.StartsWith("3301", node.Code));
        Assert.All(tree, province => Assert.NotEmpty(province.Children!));
    }

    [Fact]
    public void depth与缓存()
    {
        Assert.Same(Area.Tree(), Area.Tree(3));
        Assert.All(Area.Tree(1), province => Assert.Null(province.Children));
        Assert.All(Area.Tree(2).SelectMany(province => province.Children!), city => Assert.Null(city.Children));
        Assert.Throws<ArgumentOutOfRangeException>(() => Area.Tree(4));
    }

    [Fact]
    public void 按代码查名称_不认识的原样留着()
    {
        Assert.Equal("西湖区", Area.Name("330106"));
        Assert.Null(Area.Name("999999"));
        Assert.Equal(["浙江省", "杭州市", "西湖区", "x"], Area.Names(["330000", "330100", "330106", "x"]));
    }
}
