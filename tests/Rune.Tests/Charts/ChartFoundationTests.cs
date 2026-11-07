using Rune.UI;

namespace Rune.Tests.Charts;

// 比例尺、刻度、曲线、扇区、堆叠、文字宽度与标签避让：期望值同 vue-tsx-pro 的 scale-shape.test.ts、label-layout.test.ts（即 d3 的同名算法）
public class ChartFoundationTests
{
    [Fact]
    public void 线性比例尺_数值换坐标_坐标换回数值_范围两端相同时取中间()
    {
        var y = new LinearScale((0, 250), (200, 0));
        Assert.Equal(200, y.Map(0));
        Assert.Equal(100, y.Map(125));
        Assert.Equal(187.5, y.Invert(50));
        Assert.Equal(50, new LinearScale((3, 3), (0, 100)).Map(3));
    }

    [Fact]
    public void 好看的刻度_小数不带浮点误差()
    {
        Assert.Equal([0, 50, 100, 150, 200, 250], ChartScale.Ticks(0, 250, 5));
        Assert.Equal([0, 0.2, 0.4, 0.6, 0.8, 1], ChartScale.Ticks(0, 1, 5));
        Assert.Equal([-0.3, -0.2, -0.1, 0, 0.1, 0.2, 0.3], ChartScale.Ticks(-0.3, 0.3, 6));
        Assert.Equal([1], ChartScale.Ticks(1, 1, 5));
        Assert.Equal(2, ChartScale.TickIncrement(0, 10, 5));
        Assert.Equal(-5, ChartScale.TickIncrement(0, 1, 5));
    }

    [Fact]
    public void 范围扩到刻度上()
    {
        Assert.Equal((0, 250), ChartScale.Nice((0, 236)));
        Assert.Equal((-20, 100), ChartScale.Nice((-12.3, 87.1)));
        Assert.Equal((0, 1), ChartScale.Nice((0.12, 0.93)));
        Assert.Equal((1, 5), ChartScale.Nice((5, 1)));
    }

    [Fact]
    public void 时间刻度_跨几个月取月初_跨几天取0点_跨几小时取整点()
    {
        Assert.Equal([2, 3, 4, 5, 6, 7], ChartScale.TimeTicks(new(2026, 1, 15), new(2026, 7, 20)).Select(date => date.Day == 1 ? date.Month : -1));
        Assert.Equal([2, 3, 4, 5, 6, 7], ChartScale.TimeTicks(new(2026, 3, 1, 6, 0, 0), new(2026, 3, 7)).Select(date => date.Hour == 0 ? date.Day : -1));
        Assert.Equal([9, 10, 11, 12, 13, 14, 15], ChartScale.TimeTicks(new(2026, 3, 1, 9, 0, 0), new(2026, 3, 1, 15, 0, 0)).Select(date => date.Hour));
    }

    [Fact]
    public void 对齐的双数值轴_格数相同_步长好看()
    {
        var axes = AxisTicks.Aligned([0, 1280, 960], [0.12, 0.35]);
        Assert.Equal([0, 200, 400, 600, 800, 1000, 1200, 1400], axes.Primary.Ticks);
        Assert.Equal([0, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35], axes.Secondary.Ticks);
        Assert.Equal(800, axes.ToPrimary(0.2), 9);
        Assert.Equal(2.5, AxisTicks.NiceStep(2.1));
    }

    static readonly (double, double)[] Points = [(0, 10), (10, 20), (20, 0)];

    [Fact]
    public void 折线_阶梯_平滑曲线不越过数据点()
    {
        Assert.Equal("M0,10L10,20L20,0", ChartShapes.Line(Points));
        Assert.Equal("M0,10L10,10L10,20L20,20L20,0", ChartShapes.Line(Points, LineCurve.Step));
        // 中间的点是极大值：两侧斜率异号，切线水平（控制点与它同高）
        Assert.Equal("M0,10C3.333,15,6.667,20,10,20C13.333,20,16.667,10,20,0", ChartShapes.Line(Points, LineCurve.Monotone));
        // 两个点的平滑曲线退化成直线
        Assert.Equal("M0,10L10,20", ChartShapes.Line(Points[..2], LineCurve.Monotone));
    }

    [Fact]
    public void 面积_上沿从左到右_下沿从右到左_闭合()
    {
        Assert.Equal("M0,5L10,2L10,10L0,10Z", ChartShapes.Area([(0, 5), (10, 2)], [(0, 10), (10, 10)]));
    }

    [Fact]
    public void 柱子_只有柱顶一端是圆角_不超过宽的一半()
    {
        Assert.Equal("M0,0H10V20H0Z", ChartShapes.Bar(0, 0, 10, 20, 0, BarEnd.Top));
        Assert.Equal("M0,20V4A4,4,0,0,1,4,0H6A4,4,0,0,1,10,4V20Z", ChartShapes.Bar(0, 0, 10, 20, 4, BarEnd.Top));
        Assert.Contains("A2,2", ChartShapes.Bar(0, 0, 4, 20, 8, BarEnd.Top));
    }

    [Fact]
    public void 扇区_整圈是两段弧_圆角是四段小弧_饼图回到圆心()
    {
        Assert.Equal("M0,-10A10,10,0,0,1,0,10A10,10,0,0,1,0,-10Z", ChartShapes.Arc(new(0, 10, 0, Math.PI * 2)));
        Assert.Equal(4, System.Text.RegularExpressions.Regex.Count(ChartShapes.Arc(new(60, 90, 0, Math.PI / 2, 3)), "A3,3"));
        Assert.Equal("M0,-10A10,10,0,0,1,10,0L0,0Z", ChartShapes.Arc(new(0, 10, 0, Math.PI / 2)));
        Assert.Equal("", ChartShapes.Arc(new(5, 10, 1, 1)));
    }

    [Fact]
    public void 弦_两端各一段弧_中间经圆心()
    {
        Assert.StartsWith("M0,-10A10,10,0,0,1,10,0Q0,0,0,10A10,10,0,0,1,-10,0Q0,0,0,-10Z", ChartShapes.Ribbon(10, (0, Math.PI / 2), (Math.PI, Math.PI * 3 / 2)));
    }

    [Fact]
    public void 堆叠_正值往上_负值往下_空值高度为0()
    {
        var stacks = ChartStack.Of([[3, 5, 2], [2, -1, null], [-4, 1, 1]]);
        Assert.Equal([(0, 3), (0, 5), (0, 2)], stacks[0].Select(s => (s.Low, s.High)));
        Assert.Equal([(3, 5), (-1, 0), (2, 2)], stacks[1].Select(s => (s.Low, s.High)));
        Assert.Equal([(-4, 0), (5, 6), (2, 3)], stacks[2].Select(s => (s.Low, s.High)));
    }

    [Fact]
    public void 文字宽度_中日韩一个字号_其余0点6个_放不下截短()
    {
        Assert.Equal(10 * 2 + 6 * 3, ChartText.Width("华东 AB", 10), 9);
        Assert.Equal("短", ChartText.Truncate("短", 100));
        Assert.Equal("一二三四…", ChartText.Truncate("一二三四五六七八", 60, 12));
    }

    [Fact]
    public void SVG数字_三位小数_同JS的四舍五入与写法()
    {
        Assert.Equal(["0", "1.235", "-1.234", "0.1", "13.333"], new[] { -0.0001, 1.2345, -1.2345, 0.1, 40 / 3.0 }.Select(SvgFormat.Number));
    }

    static LabelAnchor Anchor(double x, double y, double width = 40, double radius = 4, double priority = 0) => new(x, y, radius, width, 12, priority);

    [Fact]
    public void 标签避让_默认在下方_挤了换方位_优先级高的先放()
    {
        Assert.Equal(new LabelPlacement(new(80, 107, 40, 12), false), LabelLayout.Place([Anchor(100, 100)])[0]);

        var pair = LabelLayout.Place([Anchor(100, 100), Anchor(100, 116)]);
        Assert.False(pair[0]!.Value.Box.Overlaps(pair[1]!.Value.Box));

        Assert.Equal(116 + 4 + 3, LabelLayout.Place([Anchor(100, 100), Anchor(100, 116, priority: 10)])[1]!.Value.Box.Y);
    }

    [Fact]
    public void 标签避让_一圈都占了往外退画引线_放不下不显示_尽量不压区域名()
    {
        var around = (from dx in new[] { -1, 0, 1 } from dy in new[] { -1, 0, 1 } select Anchor(100 + dx * 30, 100 + dy * 16, width: 20, radius: 6)).ToList();
        Assert.Contains(LabelLayout.Place(around, new LabelRect(0, 0, 300, 300)), item => item?.Leader == true);
        Assert.Equal([null], LabelLayout.Place([Anchor(100, 100)], new LabelRect(90, 90, 20, 20)));

        var name = new LabelRect(80, 105, 40, 14);
        var moved = LabelLayout.Place([Anchor(100, 100)], soft: [name])[0]!.Value;
        Assert.False(moved.Box.Overlaps(name));
        Assert.False(moved.Leader);
        Assert.Equal(107, LabelLayout.Place([Anchor(100, 100)], soft: [new LabelRect(0, 0, 300, 300)])[0]!.Value.Box.Y);
    }
}
