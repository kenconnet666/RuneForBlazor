namespace Rune.UI;

/// <summary>分页里的一项：页码，或省略号（<see cref="Page"/> 为 null）。</summary>
/// <param name="Page">页码，从 1 开始；省略号时为 null。</param>
/// <param name="Key">渲染用的 key：页码本身，或 start-ellipsis / end-ellipsis（两个省略号可能同时出现，要区分）。</param>
public readonly record struct PaginationItem(int? Page, string Key);

/// <summary>
/// 分页的页码序列（同 vue-tsx-pro 的 paginationItems，算法同 MUI 的 usePagination）：首尾各留 boundaries 页、当前页左右各留 siblings 页，
/// 中间用省略号；序列长度固定（页数够多时总是 2 × boundaries + 2 × siblings + 3 项），翻页时按钮不跳动。
/// <code>PaginationItems.Of(page: 6, pageCount: 20)   // 1、…、5、6、7、…、20</code>
/// </summary>
public static class PaginationItems
{
    /// <summary>要显示的页码与省略号，依次排列。</summary>
    public static IReadOnlyList<PaginationItem> Of(int page, int pageCount, int siblings = 1, int boundaries = 1)
    {
        var startPages = Range(1, Math.Min(boundaries, pageCount));
        var endPages = Range(Math.Max(pageCount - boundaries + 1, boundaries + 1), pageCount);
        // 当前页这一组的起止：靠近两端时向另一侧多借几页，保持总项数不变
        var siblingsStart = Math.Max(Math.Min(page - siblings, pageCount - boundaries - siblings * 2 - 1), boundaries + 2);
        var siblingsEnd = Math.Min(
            Math.Max(page + siblings, boundaries + siblings * 2 + 2),
            endPages.Count > 0 ? endPages[0] - 2 : pageCount - 1);

        var items = new List<PaginationItem>();
        items.AddRange(startPages.Select(Page));
        // 省略号只代替两页以上；只隔一页时直接显示那一页
        if (siblingsStart > boundaries + 2) items.Add(new(null, "start-ellipsis"));
        else if (boundaries + 1 < pageCount - boundaries) items.Add(Page(boundaries + 1));
        items.AddRange(Range(siblingsStart, siblingsEnd).Select(Page));
        if (siblingsEnd < pageCount - boundaries - 1) items.Add(new(null, "end-ellipsis"));
        else if (pageCount - boundaries > boundaries) items.Add(Page(pageCount - boundaries));
        items.AddRange(endPages.Select(Page));
        return items;
    }

    static PaginationItem Page(int page) => new(page, page.ToString(System.Globalization.CultureInfo.InvariantCulture));

    static List<int> Range(int from, int to) => to < from ? [] : [.. Enumerable.Range(from, to - from + 1)];
}

/// <summary>当前页显示的是哪几条（PaginationBar 的 TotalContent 拿到它）。</summary>
/// <param name="From">第一条的序号，从 1 开始；没有数据时是 0。</param>
/// <param name="To">最后一条的序号。</param>
/// <param name="Total">总条数。</param>
public readonly record struct PageRange(int From, int To, int Total);
