using Rune.UI;

namespace Rune.Docs.Site;

/// <summary>
/// 当前页的标题（本页目录的数据，同 vue-tsx-pro 文档站从 DOM 收集的 useHeadings）：示例小节、API 表、指南的小节在渲染时登记，
/// 离开页面时注销；布局里的 TableOfContents 先按分组、再按登记的顺序列出（示例按渲染顺序登记，就是阅读顺序：先左列后右列）。
/// 组件树里层级浅的先渲染：页面底部的 API 小节比示例先登记，所以它单独一组（<see cref="Last"/>），排在最后。
/// 文档内容的服务（<see cref="DocsServices.AddDocsContent"/>）里注册成 scoped：服务端每个请求一份，浏览器里整个应用一份。
/// </summary>
public sealed class DocHeadings
{
    /// <summary>排在页面最后的一组（API）。</summary>
    public const int Last = 1;

    readonly List<(object Owner, HeadingEntry Entry, int Group)> entries = [];

    /// <summary>登记的标题变了。</summary>
    public event Action? Changed;

    /// <summary>当前页的标题，按登记顺序。</summary>
    public IReadOnlyList<HeadingEntry> All => [.. entries.OrderBy(e => e.Group).Select(e => e.Entry)];

    /// <summary>登记一个标题（<paramref name="owner"/> 是登记它的组件，注销时用；<paramref name="group"/> 小的在前）；同一个组件再登记同一个 id 时替换。</summary>
    public void Add(object owner, HeadingEntry entry, int group = 0)
    {
        var index = entries.FindIndex(e => e.Owner == owner && e.Entry.Id == entry.Id);
        if (index >= 0)
        {
            if (entries[index].Entry == entry && entries[index].Group == group) return;
            entries[index] = (owner, entry, group);
        }
        else
        {
            entries.Add((owner, entry, group));
        }
        Changed?.Invoke();
    }

    /// <summary>注销一个组件登记的全部标题。</summary>
    public void Remove(object owner)
    {
        if (entries.RemoveAll(e => e.Owner == owner) > 0) Changed?.Invoke();
    }
}
