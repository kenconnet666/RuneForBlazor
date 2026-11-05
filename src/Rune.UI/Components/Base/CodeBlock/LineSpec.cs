namespace Rune.UI;

/// <summary>行号的写法：<c>"1,3-5"</c>（从 1 开始，逗号分隔，区间用短横线）。CodeBlock 的行高亮、diff 用。</summary>
public static class LineSpec
{
    /// <summary><c>"1,3-5"</c> → {1, 3, 4, 5}；写错的片段跳过；null 时是空集合。</summary>
    public static IReadOnlySet<int> Parse(string? spec)
    {
        var lines = new HashSet<int>();
        if (spec is null) return lines;
        foreach (var part in spec.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var range = part.Split('-', 2, StringSplitOptions.TrimEntries);
            if (!int.TryParse(range[0], out var from)) continue;
            var to = from;
            if (range.Length == 2 && !int.TryParse(range[1], out to)) continue;
            for (var line = from; line <= to; line++) lines.Add(line);
        }
        return lines;
    }
}
