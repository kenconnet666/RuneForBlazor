namespace Rune.UI;

/// <summary>地图的投影。</summary>
public enum MapProjectionKind
{
    /// <summary>等积圆锥（Albers）：两条标准纬线取数据范围的 1/6 与 5/6 处，中国、美国这类中纬度的大国（默认）。</summary>
    Conic,

    /// <summary>墨卡托：网页地图常见，高纬度放大。</summary>
    Mercator,

    /// <summary>Natural Earth：世界地图，形状与面积折中。</summary>
    NaturalEarth,

    /// <summary>Equal Earth：等积的世界地图。</summary>
    EqualEarth,

    /// <summary>等距圆柱：经纬度直接当坐标。</summary>
    Equirectangular,

    /// <summary>正射：从太空看的地球，只画朝向观察者的半球。</summary>
    Orthographic,
}

/// <summary>
/// 地图投影（同 vue-tsx-pro 的 geo/projection.ts，公式同 d3-geo、d3-geo-projection）：经纬度 → 平面坐标（y 向下，同屏幕），
/// 以及把多边形裁到能画的范围里：先旋转到以 center 为中心；世界地图换了中央经线后跨反子午线的多边形切开，绕极点的环沿极点闭合；
/// 正射投影在三维里按平面裁剪，切口沿地平圈补弧。
/// </summary>
internal sealed class MapProjection
{
    const double Radians = Math.PI / 180;
    const double HalfPi = Math.PI / 2;
    const double Tau = Math.PI * 2;
    // 墨卡托到这个纬度为止（再往上无穷大）：同网页地图的 ±85.05°
    static readonly double MercatorLimit = Math.Atan(Math.Sinh(Math.PI));

    readonly Func<IReadOnlyList<IReadOnlyList<(double, double)>>, List<List<(double X, double Y)>>> polygon;
    readonly Func<(double, double), (double X, double Y)?> point;

    MapProjection(Func<IReadOnlyList<IReadOnlyList<(double, double)>>, List<List<(double X, double Y)>>> polygon, Func<(double, double), (double X, double Y)?> point, bool sphere)
    {
        this.polygon = polygon;
        this.point = point;
        Sphere = sphere;
    }

    /// <summary>正射投影：地球的轮廓是原点处半径 1 的圆（投影坐标）；其余投影没有。</summary>
    public bool Sphere { get; }

    /// <summary>一个多边形（几个环，经纬度）裁剪、投影后的环（y 向下）；整个看不见时是空的。</summary>
    public List<List<(double X, double Y)>> Polygon(IReadOnlyList<IReadOnlyList<(double Longitude, double Latitude)>> rings) => polygon(rings);

    /// <summary>一个点；看不见（地球背面）时为 null。</summary>
    public (double X, double Y)? Point((double Longitude, double Latitude) coordinates) => point(coordinates);

    // 原始投影：弧度的经纬度 → 平面坐标（y 向上）
    static (double, double) Mercator(double lambda, double phi) =>
        (lambda, Math.Log(Math.Tan(Math.PI / 4 + Math.Max(-MercatorLimit, Math.Min(MercatorLimit, phi)) / 2)));

    static (double, double) NaturalEarth(double lambda, double phi)
    {
        var phi2 = phi * phi;
        var phi4 = phi2 * phi2;
        return (lambda * (0.8707 - 0.131979 * phi2 + phi4 * (-0.013791 + phi4 * (0.003971 * phi2 - 0.001529 * phi4))),
            phi * (1.007226 + phi2 * (0.015085 + phi4 * (-0.044475 + 0.028874 * phi2 - 0.005916 * phi4))));
    }

    static (double, double) EqualEarth(double lambda, double phi)
    {
        var (a1, a2, a3, a4) = (1.340264, -0.081106, 0.000893, 0.003796);
        var m = Math.Sqrt(3) / 2;
        var l = Math.Asin(m * Math.Sin(phi));
        var l2 = l * l;
        var l6 = l2 * l2 * l2;
        return (lambda * Math.Cos(l) / (m * (a1 + 3 * a2 * l2 + l6 * (7 * a3 + 9 * a4 * l2))), l * (a1 + a2 * l2 + l6 * (a3 + a4 * l2)));
    }

    // 等积圆锥（Albers）：两条标准纬线（弧度）；两条纬线关于赤道对称时退化成等积圆柱
    static Func<double, double, (double, double)> ConicEqualArea(double phi0, double phi1)
    {
        var sy0 = Math.Sin(phi0);
        var n = (sy0 + Math.Sin(phi1)) / 2;
        if (Math.Abs(n) < 1e-6)
        {
            var cosPhi0 = Math.Cos(phi0);
            return (lambda, phi) => (lambda * cosPhi0, Math.Sin(phi) / cosPhi0);
        }
        var c = 1 + sy0 * (2 * n - sy0);
        var r0 = Math.Sqrt(c) / n;
        return (lambda, phi) =>
        {
            var r = Math.Sqrt(Math.Max(0, c - 2 * n * Math.Sin(phi))) / n;
            return (r * Math.Sin(lambda * n), r0 - r * Math.Cos(lambda * n));
        };
    }

    // 经度归到 [-π, π]（JS 的 Math.round：.5 往正方向进）
    static double Wrap(double lambda) => lambda - Tau * Math.Floor(lambda / Tau + 0.5);

    // 单位球上的点：x 朝向经纬度 (0, 0)，z 朝北极
    static (double X, double Y, double Z) ToVector(double lambda, double phi) => (Math.Cos(phi) * Math.Cos(lambda), Math.Cos(phi) * Math.Sin(lambda), Math.Sin(phi));

    // 绕东西轴转过 -pitch：把纬度 pitch 处转到赤道上
    static (double X, double Y, double Z) Tilt((double X, double Y, double Z) v, double pitch)
    {
        var (cos, sin) = (Math.Cos(pitch), Math.Sin(pitch));
        return (v.X * cos + v.Z * sin, v.Y, v.Z * cos - v.X * sin);
    }

    // Sutherland–Hodgman：一个环裁到 keep(点) 为真的一侧，cross 求边与边界的交点
    static List<T> ClipRing<T>(IReadOnlyList<T> ring, Func<T, bool> keep, Func<T, T, T> cross)
    {
        var output = new List<T>();
        for (var i = 0; i < ring.Count; i++)
        {
            var current = ring[i];
            var previous = ring[(i + ring.Count - 1) % ring.Count];
            if (keep(current))
            {
                if (!keep(previous)) output.Add(cross(previous, current));
                output.Add(current);
            }
            else if (keep(previous)) output.Add(cross(previous, current));
        }
        return output;
    }

    // 去掉环末尾与开头重复的点（TopoJSON 的环是闭合的）
    static List<(double, double)> Open(IReadOnlyList<(double, double)> ring) =>
        ring.Count > 1 && ring[0] == ring[^1] ? [.. ring.Take(ring.Count - 1)] : [.. ring];

    // 跨反子午线的环切开（弧度，经度已旋转到 [-π, π]）：展开成连续的经度，绕极点的环沿极点闭合，
    // 再在 [-π, π] 的条带里裁剪自己和平移 ±2π 的副本；裁出的边沿着反子午线每隔 2° 补点（投影后是曲线）
    static List<List<(double, double)>> CutAntimeridian(List<(double, double)> ring)
    {
        var unwrapped = new List<(double Lambda, double Phi)>();
        var offset = 0.0;
        for (var i = 0; i < ring.Count; i++)
        {
            var (lambda, phi) = ring[i];
            if (i > 0)
            {
                var delta = lambda - ring[i - 1].Item1;
                if (delta > Math.PI) offset -= Tau;
                else if (delta < -Math.PI) offset += Tau;
            }
            unwrapped.Add((lambda + offset, phi));
        }
        if (Math.Abs(offset) > Math.PI)
        {
            // 绕了极点一圈：从最后一点沿纬线走到极点，再回到第一点的经度
            var pole = unwrapped.Sum(point => point.Phi) < 0 ? -HalfPi : HalfPi;
            unwrapped.Add((unwrapped[^1].Lambda, pole));
            unwrapped.Add((unwrapped[0].Lambda, pole));
        }
        if (unwrapped.Count == 0) return [];
        var (min, max) = (unwrapped.Min(point => point.Lambda), unwrapped.Max(point => point.Lambda));
        if (min >= -Math.PI - 1e-9 && max <= Math.PI + 1e-9) return [[.. unwrapped.Select(point => (point.Lambda, point.Phi))]];
        Func<(double, double), (double, double), (double, double)> AtEdge(double edge) =>
            (a, b) => (edge, a.Item2 + (edge - a.Item1) / (b.Item1 - a.Item1) * (b.Item2 - a.Item2));
        var pieces = new List<List<(double, double)>>();
        foreach (var shift in new[] { -Tau, 0, Tau })
        {
            if (max + shift <= -Math.PI || min + shift >= Math.PI) continue;
            var shifted = unwrapped.Select(point => (point.Lambda + shift, point.Phi)).ToList();
            var clipped = ClipRing(ClipRing(shifted, point => point.Item1 <= Math.PI, AtEdge(Math.PI)), point => point.Item1 >= -Math.PI, AtEdge(-Math.PI));
            if (clipped.Count >= 3) pieces.Add(DensifyEdge(clipped));
        }
        return pieces;
    }

    // 沿反子午线的边每隔 2° 补一个点
    static List<(double, double)> DensifyEdge(List<(double, double)> ring)
    {
        var step = 2 * Radians;
        var output = new List<(double, double)>();
        for (var i = 0; i < ring.Count; i++)
        {
            var point = ring[i];
            var next = ring[(i + 1) % ring.Count];
            output.Add(point);
            var onEdge = Math.Abs(Math.Abs(point.Item1) - Math.PI) < 1e-9 && point.Item1 == next.Item1;
            if (!onEdge) continue;
            var count = (int)Math.Floor(Math.Abs(next.Item2 - point.Item2) / step);
            for (var k = 1; k < count; k++) output.Add((point.Item1, point.Item2 + (next.Item2 - point.Item2) * k / count));
        }
        return output;
    }

    // 正射投影：环裁到朝向观察者的半球（x ≥ 0），切口沿地平圈补弧；返回投影后的平面坐标（y 向上）
    static List<(double, double)> ClipHemisphere(List<(double X, double Y, double Z)> ring)
    {
        var clipped = ClipRing(ring, point => point.X >= 0, (a, b) =>
        {
            var t = a.X / (a.X - b.X);
            var (y, z) = (a.Y + t * (b.Y - a.Y), a.Z + t * (b.Z - a.Z));
            var length = double.Hypot(y, z) is var h && h != 0 ? h : 1;
            return (0, y / length, z / length);
        });
        var output = new List<(double, double)>();
        for (var i = 0; i < clipped.Count; i++)
        {
            var point = clipped[i];
            output.Add((point.Y, point.Z));
            var next = clipped[(i + 1) % clipped.Count];
            if (point.X != 0 || next.X != 0) continue;
            // 两个交点之间沿地平圈走较短的那段弧
            var from = Math.Atan2(point.Z, point.Y);
            var delta = Math.Atan2(next.Z, next.Y) - from;
            if (delta > Math.PI) delta -= Tau;
            if (delta < -Math.PI) delta += Tau;
            var count = (int)Math.Ceiling(Math.Abs(delta) / (4 * Radians));
            for (var k = 1; k < count; k++)
            {
                var angle = from + delta * k / count;
                output.Add((Math.Cos(angle), Math.Sin(angle)));
            }
        }
        return output;
    }

    /// <summary>建投影：center 是 [经度, 纬度]（度，中央经线；正射投影还用纬度），bounds 是数据的经纬度范围 [西, 南, 东, 北]。</summary>
    public static MapProjection Create(MapProjectionKind kind, (double Longitude, double Latitude) center, (double West, double South, double East, double North) bounds)
    {
        var lambda0 = center.Longitude * Radians;
        static (double X, double Y) Flip((double, double) p) => (p.Item1, -p.Item2);
        if (kind == MapProjectionKind.Orthographic)
        {
            var pitch = center.Latitude * Radians;
            (double X, double Y, double Z) ToView((double Longitude, double Latitude) p) => Tilt(ToVector(p.Longitude * Radians - lambda0, p.Latitude * Radians), pitch);
            return new MapProjection(
                rings => [.. rings.Select(ring => ClipHemisphere([.. Open(ring).Select(ToView)])).Where(clipped => clipped.Count >= 3).Select(clipped => clipped.Select(Flip).ToList())],
                p =>
                {
                    var (x, y, z) = ToView(p);
                    return x < 0 ? null : (y, -z);
                },
                sphere: true);
        }
        var (south, north) = (bounds.South, bounds.North);
        Func<double, double, (double, double)> raw = kind switch
        {
            MapProjectionKind.Conic => ConicEqualArea((south + (north - south) / 6) * Radians, (north - (north - south) / 6) * Radians),
            MapProjectionKind.Mercator => Mercator,
            MapProjectionKind.EqualEarth => EqualEarth,
            MapProjectionKind.NaturalEarth => NaturalEarth,
            _ => (lambda, phi) => (lambda, phi),
        };
        (double, double) Rotate((double Longitude, double Latitude) p) => (Wrap(p.Longitude * Radians - lambda0), p.Latitude * Radians);
        return new MapProjection(
            rings => [.. rings.SelectMany(ring => CutAntimeridian([.. Open(ring).Select(Rotate)]).Select(piece => piece.Select(p => Flip(raw(p.Item1, p.Item2))).ToList()))],
            p =>
            {
                var (lambda, phi) = Rotate(p);
                return Flip(raw(lambda, phi));
            },
            sphere: false);
    }
}
