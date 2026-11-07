using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Rune.UI;

/// <summary>解出来的一个地图要素。</summary>
/// <param name="Id">要素的 id。</param>
/// <param name="Name">属性里的 name（英文名）；没有时为 null。</param>
/// <param name="Polygons">多边形：每个多边形是几个环（[经度, 纬度]），第一个是外环，其余是洞。</param>
public sealed record MapFeature(string Id, string? Name, IReadOnlyList<IReadOnlyList<IReadOnlyList<(double Longitude, double Latitude)>>> Polygons);

/// <summary>
/// TopoJSON 拓扑（同 vue-tsx-pro 的 geo/topojson.ts，解码的写法同 topojson-client）：相邻区域共用的边界存成一段段弧（arcs），
/// 多边形的环是弧的序号（负数 ~i 表示倒着走第 i 段）；有 transform 时弧的坐标是量化后逐点的差值。
/// <code>var topology = MapTopology.Parse(json);
/// topology.Features("provinces")[0].Id   // "CN-GS"</code>
/// </summary>
public sealed class MapTopology
{
    readonly JsonElement objects;

    MapTopology(IReadOnlyList<(double X, double Y, double Weight)[]> arcs, JsonElement objects, (double, double, double, double)? bbox)
    {
        Arcs = arcs;
        this.objects = objects;
        Bbox = bbox;
    }

    /// <summary>各段弧的绝对经纬度（简化过的拓扑里第三项是 Visvalingam 的权重）。</summary>
    internal IReadOnlyList<(double X, double Y, double Weight)[]> Arcs { get; }

    /// <summary>文件里写的经纬度范围 [西, 南, 东, 北]。</summary>
    internal (double West, double South, double East, double North)? Bbox { get; }

    /// <summary>读 TopoJSON 文本。</summary>
    public static MapTopology Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var (sx, sy, tx, ty, quantized) = (1.0, 1.0, 0.0, 0.0, false);
        if (root.TryGetProperty("transform", out var transform))
        {
            quantized = true;
            var scale = transform.GetProperty("scale");
            var translate = transform.GetProperty("translate");
            (sx, sy, tx, ty) = (scale[0].GetDouble(), scale[1].GetDouble(), translate[0].GetDouble(), translate[1].GetDouble());
        }
        var arcs = root.GetProperty("arcs").EnumerateArray().Select(arc =>
        {
            var (x, y) = (0.0, 0.0);
            return arc.EnumerateArray().Select(point =>
            {
                var (px, py) = (point[0].GetDouble(), point[1].GetDouble());
                if (!quantized) return (px, py, 0.0);
                x += px;
                y += py;
                return (x * sx + tx, y * sy + ty, 0.0);
            }).ToArray();
        }).ToList();
        (double, double, double, double)? bbox = root.TryGetProperty("bbox", out var box) && box.GetArrayLength() >= 4
            ? (box[0].GetDouble(), box[1].GetDouble(), box[2].GetDouble(), box[3].GetDouble())
            : null;
        return new MapTopology(arcs, root.GetProperty("objects").Clone(), bbox);
    }

    /// <summary>要素组 name 里的要素（GeometryCollection 展开；单个几何对象当作一个要素）；没有这一组时是空的。</summary>
    public IReadOnlyList<MapFeature> Features(string name)
    {
        if (!objects.TryGetProperty(name, out var group)) return [];
        var members = group.GetProperty("type").GetString() == "GeometryCollection" && group.TryGetProperty("geometries", out var geometries)
            ? geometries.EnumerateArray().ToList()
            : [group];
        return [.. members.Select(member => new MapFeature(
            member.TryGetProperty("id", out var id) ? id.ValueKind == JsonValueKind.Number ? id.GetRawText() : id.GetString() ?? "" : "",
            member.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty("name", out var label) ? label.GetString() : null,
            PolygonsOf(member)))];
    }

    // 一个环：依次接上各段弧（后一段的第一个点就是前一段的最后一个点，去掉重复的）
    List<(double, double)> Ring(JsonElement indices)
    {
        var points = new List<(double, double)>();
        foreach (var item in indices.EnumerateArray())
        {
            var index = item.GetInt32();
            var arc = index < 0 ? Arcs[~index].Reverse() : Arcs[index];
            if (points.Count > 0) points.RemoveAt(points.Count - 1);
            points.AddRange(arc.Select(point => (point.X, point.Y)));
        }
        return points;
    }

    IReadOnlyList<IReadOnlyList<IReadOnlyList<(double, double)>>> PolygonsOf(JsonElement member)
    {
        if (!member.TryGetProperty("arcs", out var arcs)) return [];
        return member.GetProperty("type").GetString() switch
        {
            "Polygon" => [[.. arcs.EnumerateArray().Select(Ring)]],
            "MultiPolygon" => [.. arcs.EnumerateArray().Select(rings => (IReadOnlyList<IReadOnlyList<(double, double)>>)[.. rings.EnumerateArray().Select(Ring)])],
            _ => [],
        };
    }

    /// <summary>经纬度范围 [西, 南, 东, 北]：有 bbox 用它，否则按各段弧的坐标算。</summary>
    public (double West, double South, double East, double North) Bounds()
    {
        if (Bbox is { } box) return box;
        var points = Arcs.SelectMany(arc => arc).ToList();
        return points.Count == 0 ? (0, 0, 0, 0) : (points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
    }

    // 算好了各点权重的、按档位简化过的：同一份拓扑只算一次
    static readonly ConditionalWeakTable<MapTopology, MapTopology> Presimplified = [];
    static readonly ConditionalWeakTable<MapTopology, Dictionary<double, MapTopology>> Simplified = [];

    /// <summary>
    /// 简化：去掉与前后两点围成的三角形面积不到 minArea（坐标单位的平方，经纬度是平方度）的点（Visvalingam，同 topojson-simplify）。
    /// 相邻区域共用的边界一起简化，不会出现缝隙；各段弧的两端总保留。minArea 按 2 的整数次幂分档缓存：绘图区大小变一点不重算。
    /// </summary>
    public MapTopology Simplify(double minArea)
    {
        if (!(minArea > 0) || !double.IsFinite(minArea)) return this;
        var level = Math.Pow(2, Math.Floor(Math.Log2(minArea)));
        var byLevel = Simplified.GetValue(this, _ => []);
        lock (byLevel)
        {
            if (byLevel.TryGetValue(level, out var cached)) return cached;
            var weighted = Presimplified.GetValue(this, topology => topology.Presimplify());
            var result = new MapTopology([.. weighted.Arcs.Select(arc => arc.Where(point => point.Weight >= level).ToArray())], objects, Bbox);
            byLevel[level] = result;
            return result;
        }
    }

    // 各点的权重（同 topojson-simplify 的 presimplify）：三角形面积用最小堆从小到大去掉，去掉一点后重算两边的；两端是无穷大
    MapTopology Presimplify()
    {
        var heap = new AreaHeap();
        var arcs = Arcs.Select(source =>
        {
            var arc = source.Select(point => new[] { point.X, point.Y, 0 }).ToArray();
            var n = arc.Length - 1;
            var triangles = new List<Triangle>();
            var maxArea = 0.0;
            for (var i = 1; i < n; i++)
            {
                var triangle = new Triangle(arc[i - 1], arc[i], arc[i + 1]);
                triangle.B[2] = triangle.Area();
                triangles.Add(triangle);
                heap.Push(triangle);
            }
            // 两端总保留
            if (n >= 0)
            {
                arc[0][2] = double.PositiveInfinity;
                arc[n][2] = double.PositiveInfinity;
            }
            for (var i = 0; i < triangles.Count; i++)
            {
                triangles[i].Previous = i > 0 ? triangles[i - 1] : null;
                triangles[i].Next = i + 1 < triangles.Count ? triangles[i + 1] : null;
            }
            while (heap.Pop() is { } triangle)
            {
                // 这一点的面积比之前去掉的小时取之前的：去掉它之前，之前那些一定已经去掉了
                if (triangle.B[2] < maxArea) triangle.B[2] = maxArea;
                else maxArea = triangle.B[2];
                if (triangle.Previous is { } previous)
                {
                    previous.Next = triangle.Next;
                    previous.C = triangle.C;
                    heap.Update(previous);
                }
                if (triangle.Next is { } next)
                {
                    next.Previous = triangle.Previous;
                    next.A = triangle.A;
                    heap.Update(next);
                }
            }
            return arc.Select(point => (point[0], point[1], point[2])).ToArray();
        }).ToList();
        return new MapTopology(arcs, objects, Bbox);
    }

    sealed class Triangle(double[] a, double[] b, double[] c)
    {
        public double[] A = a, B = b, C = c;
        public Triangle? Previous, Next;
        public int Slot;

        public double Area() => Math.Abs((A[0] - C[0]) * (B[1] - A[1]) - (A[0] - B[0]) * (C[1] - A[1])) / 2;
    }

    // 最小堆，按中间点的面积（同 topojson-simplify 的 minAreaHeap，相等时的先后也相同）
    sealed class AreaHeap
    {
        readonly List<Triangle> array = [];
        int size;

        static double Compare(Triangle a, Triangle b) => a.B[2] - b.B[2];

        void Set(int i, Triangle triangle)
        {
            if (i < array.Count) array[i] = triangle;
            else array.Add(triangle);
            triangle.Slot = i;
        }

        public void Push(Triangle triangle)
        {
            Set(size, triangle);
            Up(triangle, size++);
        }

        public Triangle? Pop()
        {
            if (size <= 0) return null;
            var removed = array[0];
            if (--size > 0)
            {
                var last = array[size];
                Set(0, last);
                Down(last, 0);
            }
            return removed;
        }

        public void Update(Triangle triangle)
        {
            Remove(triangle);
            triangle.B[2] = triangle.Area();
            Push(triangle);
        }

        void Remove(Triangle removed)
        {
            var i = removed.Slot;
            if (i >= size || array[i] != removed) return;
            if (i != --size)
            {
                var last = array[size];
                Set(i, last);
                if (Compare(last, removed) < 0) Up(last, i);
                else Down(last, i);
            }
        }

        void Up(Triangle triangle, int i)
        {
            while (i > 0)
            {
                var j = ((i + 1) >> 1) - 1;
                var parent = array[j];
                if (Compare(triangle, parent) >= 0) break;
                Set(i, parent);
                Set(i = j, triangle);
            }
        }

        void Down(Triangle triangle, int i)
        {
            while (true)
            {
                var r = (i + 1) << 1;
                var l = r - 1;
                var j = i;
                var child = array[j];
                if (l < size && Compare(array[l], child) < 0) child = array[j = l];
                if (r < size && Compare(array[r], child) < 0) child = array[j = r];
                if (j == i) break;
                Set(i, child);
                Set(i = j, triangle);
            }
        }
    }
}
