namespace Rune.UI;

/// <summary>矩形：左上角与宽高。</summary>
/// <param name="X">左。</param>
/// <param name="Y">上。</param>
/// <param name="Width">宽。</param>
/// <param name="Height">高。</param>
public readonly record struct LabelRect(double X, double Y, double Width, double Height)
{
    /// <summary>两个矩形相交（边贴边不算）。</summary>
    public bool Overlaps(LabelRect other) => X < other.X + other.Width && other.X < X + Width && Y < other.Y + other.Height && other.Y < Y + Height;

    /// <summary>整个落在 <paramref name="bounds"/> 里。</summary>
    public bool Inside(LabelRect bounds) => X >= bounds.X && Y >= bounds.Y && X + Width <= bounds.X + bounds.Width && Y + Height <= bounds.Y + bounds.Height;
}

/// <summary>要标注的点。</summary>
/// <param name="X">中心横坐标。</param>
/// <param name="Y">中心纵坐标。</param>
/// <param name="Radius">标记的半径：标签放在它外面。</param>
/// <param name="Width">标签的宽。</param>
/// <param name="Height">标签的高。</param>
/// <param name="Priority">优先级：大的先放（地图按数值）；相同时按原来的顺序。</param>
public readonly record struct LabelAnchor(double X, double Y, double Radius, double Width, double Height, double Priority = 0);

/// <summary>一个标签放在哪。</summary>
/// <param name="Box">标签的矩形。</param>
/// <param name="Leader">退到了外一圈：画一条引线连回标记。</param>
public readonly record struct LabelPlacement(LabelRect Box, bool Leader);

/// <summary>
/// 点标注的避让（同 vue-tsx-pro 的 placeLabels）：给每个锚点（地图上的地点）找一个不压住别的标签、标记的位置。
/// 参照制图学的点标注（Imhof 的方位优先级）与 ECharts labelLayout 的 moveOverlap、Mapbox 的 variable anchor：
/// 按优先级贪心放置，先试紧挨着标记的 8 个方位，都放不下时一圈圈往外退、画引线连回标记，仍放不下就不显示。坐标都是屏幕像素。
/// </summary>
public static class LabelLayout
{
    // 方位的优先级：下（地图上地名常见的位置）、上、右、左，然后四个斜角
    static readonly (int X, int Y)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    // 锚点在某个方位、某个距离上的标签矩形：标签靠向标记的那条边（或那个角）离标记 distance
    static LabelRect BoxAt(LabelAnchor anchor, (int X, int Y) direction, double distance)
    {
        var (dx, dy) = direction;
        // 斜角按 45° 摆，两个方向各走 distance / √2
        var step = dx != 0 && dy != 0 ? distance * Math.Sqrt(0.5) : distance;
        var x = anchor.X + dx * step - (dx == 1 ? 0 : dx == -1 ? anchor.Width : anchor.Width / 2);
        var y = anchor.Y + dy * step - (dy == 1 ? 0 : dy == -1 ? anchor.Height : anchor.Height / 2);
        return new(x, y, anchor.Width, anchor.Height);
    }

    /// <summary>给每个锚点找位置；返回与 <paramref name="anchors"/> 一一对应，null 时不显示。</summary>
    /// <param name="anchors">要标注的点。</param>
    /// <param name="bounds">标签要落在这个范围里（绘图区）；不给时不限。</param>
    /// <param name="soft">尽量不压住的矩形（地图上的区域名）：都避不开时可以压住。</param>
    /// <param name="gap">标签与标记的间距。</param>
    /// <param name="ring">往外退时每圈远多少。</param>
    /// <param name="rings">往外退几圈。</param>
    public static LabelPlacement?[] Place(IReadOnlyList<LabelAnchor> anchors, LabelRect? bounds = null, IReadOnlyList<LabelRect>? soft = null,
        double gap = 3, double ring = 16, int rings = 2)
    {
        soft ??= [];
        var markers = anchors.Select(anchor => new LabelRect(anchor.X - anchor.Radius, anchor.Y - anchor.Radius, anchor.Radius * 2, anchor.Radius * 2)).ToList();
        var placed = new List<LabelRect>();
        var result = new LabelPlacement?[anchors.Count];
        bool Free(LabelRect box) => (bounds is not { } b || box.Inside(b)) && !markers.Any(box.Overlaps) && !placed.Any(box.Overlaps);

        // OrderByDescending 是稳定排序：优先级相同时按原来的顺序
        foreach (var index in Enumerable.Range(0, anchors.Count).OrderByDescending(i => anchors[i].Priority))
        {
            var anchor = anchors[index];
            // 紧挨着一圈，再一圈圈往外；每圈先找不压区域名的，再退而求其次
            for (var level = 0; level <= rings; level++)
            {
                var distance = anchor.Radius + gap + ring * level;
                var candidates = Directions.Select(direction => BoxAt(anchor, direction, distance)).ToList();
                var box = candidates.Where(item => Free(item) && !soft.Any(item.Overlaps)).Cast<LabelRect?>().FirstOrDefault()
                    ?? candidates.Where(Free).Cast<LabelRect?>().FirstOrDefault();
                if (box is not { } found) continue;
                placed.Add(found);
                result[index] = new LabelPlacement(found, level > 0);
                break;
            }
        }
        return result;
    }
}
