using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.Charts;

// 雷达图的渲染：网格、轴、系列是 svg 里的兄弟元素，@key 不能重复（重复时再次渲染的对比出错，之后整个渲染器都坏掉）
public class RadarChartTests : BunitContext
{
    sealed record Score(string Item, decimal Value);

    static readonly Score[] Scores = [new("a", 3), new("b", 5), new("c", 2), new("d", 4)];

    static readonly ChartSeries<Score>[] Series = [new("score", "得分", s => s.Value)];

    public RadarChartTests()
    {
        Services.AddRuneUI();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void 再次渲染时对比正常(bool circleGrid)
    {
        var cut = Render<RadarChart<Score>>(p => p.Add(x => x.Items, Scores).Add(x => x.Dimension, s => s.Item).Add(x => x.Series, Series)
            .Add(x => x.CircleGrid, circleGrid).Add(x => x.Levels, 4).Add(x => x.Height, 260));

        cut.Render(p => p.Add(x => x.Levels, 3));

        Assert.Equal(3, cut.FindAll(circleGrid ? "svg > circle" : "svg > polygon").Count);
        Assert.Equal(Scores.Length, cut.FindAll("svg > g > line").Count);
    }
}
