using System.Text;

namespace Rune.UI;

/// <summary>线的形状。</summary>
public enum LineCurve
{
    /// <summary>折线：相邻的点直线相连（默认）。</summary>
    Linear,

    /// <summary>平滑曲线：不会越过数据点（单调插值），趋势更柔和。</summary>
    Monotone,

    /// <summary>阶梯：值保持到下一个点才跳变，适合离散的状态、价格档位。</summary>
    Step,
}

/// <summary>柱子的哪一端是圆角：柱顶朝哪个方向。</summary>
public enum BarEnd
{
    /// <summary>朝上（竖向的正值）。</summary>
    Top,

    /// <summary>朝下（竖向的负值）。</summary>
    Bottom,

    /// <summary>朝左（横向的负值）。</summary>
    Left,

    /// <summary>朝右（横向的正值）。</summary>
    Right,

    /// <summary>没有圆角（堆叠中间的段）。</summary>
    None,
}

/// <summary>环形的一段：角度从 12 点钟方向顺时针（弧度），圆心在原点。</summary>
/// <param name="Inner">内半径；0 是饼图的扇形。</param>
/// <param name="Outer">外半径。</param>
/// <param name="Start">起始角。</param>
/// <param name="End">结束角。</param>
/// <param name="Corner">四个角的圆角半径。</param>
public readonly record struct ArcShape(double Inner, double Outer, double Start, double End, double Corner = 0);

/// <summary>
/// 图形的 SVG 路径（同 vue-tsx-pro 的 shape.ts）：折线与面积（直线、单调平滑、阶梯）、一端圆角的柱子、环形的扇区、弦图的弦、桑基图的带子。
/// 曲线的算法同 d3-shape（Monotone 是 Steffen 的单调三次插值，不会越过数据点）；坐标保留三位小数，与 vue 版逐字相同。
/// <code>ChartShapes.Line([(0, 10), (40, 30), (80, 20)], LineCurve.Monotone)   // "M0,10C13.333,…"
/// ChartShapes.Arc(new(Inner: 60, Outer: 88, Start: 0, End: Math.PI / 2, Corner: 3))</code>
/// </summary>
public static class ChartShapes
{
    static string N(double value) => SvgFormat.Number(value);

    static string Pair(double x, double y) => $"{N(x)},{N(y)}";

    static int Sign(double value) => value < 0 ? -1 : 1;

    // 中间一点的切线斜率：两侧的斜率异号时为 0（极值点不越过），否则取不超过两侧的调和值（同 d3 的 slope3）
    static double InteriorSlope((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
    {
        var h0 = b.X - a.X;
        var h1 = c.X - b.X;
        // 横坐标相同时除以带符号的 0：得到同向的无穷大，下面取 min 时自然被别的值替代
        var s0 = (b.Y - a.Y) / (h0 != 0 ? h0 : h1 < 0 ? -0.0 : 0.0);
        var s1 = (c.Y - b.Y) / (h1 != 0 ? h1 : h0 < 0 ? -0.0 : 0.0);
        var p = (s0 * h1 + s1 * h0) / (h0 + h1);
        var slope = (Sign(s0) + Sign(s1)) * Math.Min(Math.Min(Math.Abs(s0), Math.Abs(s1)), 0.5 * Math.Abs(p));
        return double.IsNaN(slope) ? 0 : slope;
    }

    // 端点的切线斜率：由相邻一段的斜率与相邻点的切线推出（同 d3 的 slope2）
    static double EndSlope((double X, double Y) a, (double X, double Y) b, double tangent)
    {
        var h = b.X - a.X;
        return h != 0 ? (3 * (b.Y - a.Y) / h - tangent) / 2 : tangent;
    }

    // 单调三次插值：各点的切线，再按 Hermite 写成三次贝塞尔
    static void Monotone(StringBuilder path, IReadOnlyList<(double X, double Y)> points)
    {
        var n = points.Count;
        var tangents = new double[n];
        for (var i = 1; i < n - 1; i++) tangents[i] = InteriorSlope(points[i - 1], points[i], points[i + 1]);
        tangents[0] = EndSlope(points[0], points[1], tangents[1]);
        tangents[n - 1] = EndSlope(points[n - 2], points[n - 1], tangents[n - 2]);
        for (var i = 1; i < n; i++)
        {
            var (x0, y0) = points[i - 1];
            var (x1, y1) = points[i];
            var dx = (x1 - x0) / 3;
            path.Append('C').Append(Pair(x0 + dx, y0 + dx * tangents[i - 1])).Append(',')
                .Append(Pair(x1 - dx, y1 - dx * tangents[i])).Append(',').Append(Pair(x1, y1));
        }
    }

    /// <summary>
    /// 一串点连成的路径；<paramref name="start"/> 是开头的命令（M 新起一段，L 接着上一段画：面积的下沿）。
    /// 少于三个点时平滑曲线退化成直线。
    /// </summary>
    public static string Line(IReadOnlyList<(double X, double Y)> points, LineCurve curve = LineCurve.Linear, char start = 'M')
    {
        if (points.Count == 0) return "";
        var path = new StringBuilder().Append(start).Append(Pair(points[0].X, points[0].Y));
        if (curve == LineCurve.Monotone && points.Count > 2)
        {
            Monotone(path, points);
            return path.ToString();
        }
        for (var i = 1; i < points.Count; i++)
        {
            // 阶梯：横着走到下一个点的横坐标，再竖着跳到它的值
            if (curve == LineCurve.Step) path.Append('L').Append(Pair(points[i].X, points[i - 1].Y));
            path.Append('L').Append(Pair(points[i].X, points[i].Y));
        }
        return path.ToString();
    }

    /// <summary>面积：上沿 <paramref name="top"/> 从左到右，下沿 <paramref name="bottom"/>（与 top 一一对应）从右到左，同一种曲线，闭合。</summary>
    public static string Area(IReadOnlyList<(double X, double Y)> top, IReadOnlyList<(double X, double Y)> bottom, LineCurve curve = LineCurve.Linear)
    {
        if (top.Count == 0) return "";
        // 阶梯的下沿倒着画时要先竖后横，才与上沿的拐角对齐：倒着画的阶梯把每一步的值挪到前一个点
        var back = curve == LineCurve.Step
            ? bottom.SelectMany((point, i) => i == 0 ? [point] : new[] { (point.X, bottom[i - 1].Y), point }).ToList()
            : [.. bottom];
        back.Reverse();
        return $"{Line(top, curve)}{Line(back, curve == LineCurve.Step ? LineCurve.Linear : curve, 'L')}Z";
    }

    /// <summary>柱子：矩形（左上角 x、y，宽高），<paramref name="end"/> 那一端的两个角是半径 <paramref name="radius"/> 的圆角（不超过宽或高的一半）。</summary>
    public static string Bar(double x, double y, double width, double height, double radius, BarEnd end)
    {
        var w = Math.Max(width, 0);
        var h = Math.Max(height, 0);
        var vertical = end is BarEnd.Top or BarEnd.Bottom;
        var r = end == BarEnd.None ? 0 : Math.Max(0, Math.Min(radius, Math.Min(vertical ? w / 2 : h / 2, vertical ? h : w)));
        var (x1, y1) = (x + w, y + h);
        string Corner(double cx, double cy) => $"A{Pair(r, r)},0,0,1,{Pair(cx, cy)}";
        if (r == 0) return $"M{Pair(x, y)}H{N(x1)}V{N(y1)}H{N(x)}Z";
        return end switch
        {
            BarEnd.Top => $"M{Pair(x, y1)}V{N(y + r)}{Corner(x + r, y)}H{N(x1 - r)}{Corner(x1, y + r)}V{N(y1)}Z",
            BarEnd.Bottom => $"M{Pair(x1, y)}V{N(y1 - r)}{Corner(x1 - r, y1)}H{N(x + r)}{Corner(x, y1 - r)}V{N(y)}Z",
            BarEnd.Right => $"M{Pair(x, y)}H{N(x1 - r)}{Corner(x1, y + r)}V{N(y1 - r)}{Corner(x1 - r, y1)}H{N(x)}Z",
            _ => $"M{Pair(x1, y1)}H{N(x + r)}{Corner(x, y1 - r)}V{N(y + r)}{Corner(x + r, y)}H{N(x1)}Z",
        };
    }

    /// <summary>半径 <paramref name="r"/>、角度 <paramref name="angle"/>（12 点钟方向顺时针）处的点。</summary>
    public static (double X, double Y) Polar(double r, double angle) => (r * Math.Sin(angle), -r * Math.Cos(angle));

    static string At(double r, double angle)
    {
        var (x, y) = Polar(r, angle);
        return Pair(x, y);
    }

    /// <summary>
    /// 环形一段的路径：两条径向的边、内外两段弧；有圆角时四个角各是一段与边、弧都相切的小圆弧
    /// （边是径向的，与圆正交，切点可以直接算出）。整圈时画成两段弧（没有边）。
    /// </summary>
    public static string Arc(ArcShape shape)
    {
        var (inner, outer, start, end, corner) = shape;
        var sweep = end - start;
        if (!(sweep > 0) || !(outer > 0)) return "";
        string ArcTo(double r, double to, bool clockwise, double span) => $"A{Pair(r, r)},0,{(span > Math.PI ? 1 : 0)},{(clockwise ? 1 : 0)},{At(r, to)}";
        if (sweep >= Math.PI * 2 - 1e-9)
        {
            var outerRing = $"M{At(outer, 0)}{ArcTo(outer, Math.PI, true, Math.PI)}{ArcTo(outer, 0, true, Math.PI)}Z";
            if (inner <= 0) return outerRing;
            return $"{outerRing}M{At(inner, 0)}{ArcTo(inner, Math.PI, false, Math.PI)}{ArcTo(inner, 0, false, Math.PI)}Z";
        }
        // 圆角不超过环宽的一半，也不超过这一段在内（饼图是外）弧上长度的一半
        var r = Math.Max(0, Math.Min(corner, Math.Min((outer - inner) / 2, (inner > 0 ? inner : outer) * sweep / 2.2)));
        if (r < 0.5)
        {
            var outerPart = $"M{At(outer, start)}{ArcTo(outer, end, true, sweep)}";
            if (inner <= 0) return $"{outerPart}L0,0Z";
            return $"{outerPart}L{At(inner, end)}{ArcTo(inner, start, false, sweep)}Z";
        }
        // 外角：小圆心在半径 outer - r 上，切边的点离圆心 √((outer-r)² - r²)，切弧的点偏开 asin(r / (outer - r))
        var outerShift = Math.Asin(r / (outer - r));
        var outerEdge = Math.Sqrt((outer - r) * (outer - r) - r * r);
        string Fillet(string to) => $"A{Pair(r, r)},0,0,1,{to}";
        var path = $"M{At(outerEdge, start)}{Fillet(At(outer, start + outerShift))}"
            + $"{ArcTo(outer, end - outerShift, true, sweep - outerShift * 2)}{Fillet(At(outerEdge, end))}";
        if (inner <= 0) return $"{path}L0,0Z";
        // 内角：小圆心在半径 inner + r 上
        var innerShift = Math.Asin(r / (inner + r));
        var innerEdge = Math.Sqrt((inner + r) * (inner + r) - r * r);
        return path + $"L{At(innerEdge, end)}{Fillet(At(inner, end - innerShift))}"
            + $"{ArcTo(inner, start + innerShift, false, sweep - innerShift * 2)}{Fillet(At(innerEdge, start))}Z";
    }

    /// <summary>弦图的一根弦：两端是圆上的两段弧（半径 <paramref name="radius"/>，角度同 <see cref="Arc"/>），中间经圆心的二次曲线相连（同 d3 的 ribbon）。</summary>
    public static string Ribbon(double radius, (double Start, double End) source, (double Start, double End) target)
    {
        string ArcOf((double Start, double End) span) => $"A{Pair(radius, radius)},0,{(span.End - span.Start > Math.PI ? 1 : 0)},1,{At(radius, span.End)}";
        var path = $"M{At(radius, source.Start)}{ArcOf(source)}";
        if (source != target) path += $"Q0,0,{At(radius, target.Start)}{ArcOf(target)}";
        return $"{path}Q0,0,{At(radius, source.Start)}Z";
    }

    /// <summary>横向的流向带（桑基图的连线）：从 (x0, y0) 到 (x1, y1) 的 S 形三次曲线，控制点在两端的中间。</summary>
    public static string HorizontalLink(double x0, double y0, double x1, double y1)
    {
        var middle = (x0 + x1) / 2;
        return $"M{Pair(x0, y0)}C{Pair(middle, y0)},{Pair(middle, y1)},{Pair(x1, y1)}";
    }
}
