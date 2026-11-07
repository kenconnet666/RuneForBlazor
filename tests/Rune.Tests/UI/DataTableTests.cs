using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

// 虚拟滚动、点行在 JS 里（js/virtual.js 的表格模式、js/table.js），这里测 C# 的一半：列的收集与显示、排序、勾选、合计、空状态与骨架
public class DataTableTests : BunitContext
{
    public DataTableTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    enum Status { Draft, Paid }

    sealed record Order(string Id, [property: Display(Name = "客户")] string Customer, decimal Amount, Status Status, DateOnly Created);

    static readonly StatusMap<Status> Statuses = new()
    {
        [Status.Draft] = new("草稿"),
        [Status.Paid] = new("已付款", Tone.Success),
    };

    static readonly Order[] Orders =
    [
        new("A-3", "启航", 1200.5m, Status.Paid, new(2026, 3, 1)),
        new("A-1", "北辰", 80m, Status.Draft, new(2026, 1, 9)),
        new("A-2", "安澜", 330m, Status.Paid, new(2026, 2, 14)),
    ];

    IRenderedComponent<DataTable<Order>> RenderOrders(Action<ComponentParameterCollectionBuilder<DataTable<Order>>>? configure = null) =>
        Render<DataTable<Order>>(p =>
        {
            p.Add(x => x.Items, Orders).Add(x => x.GetKey, o => o.Id).AddUnmatched("aria-label", "订单");
            p.AddChildContent<PropertyColumn<Order, string>>(c => c.Add(x => x.Property, o => o.Id).Add(x => x.Width, 100).Add(x => x.Sticky, ColumnSticky.Start));
            p.AddChildContent<PropertyColumn<Order, string>>(c => c.Add(x => x.Property, o => o.Customer).Add(x => x.Sortable, true));
            p.AddChildContent<PropertyColumn<Order, decimal>>(c => c.Add(x => x.Property, o => o.Amount).Add(x => x.Type, ColumnType.Currency).Add(x => x.Sortable, true).Add(x => x.Summary, ColumnSummary.Sum));
            p.AddChildContent<StatusColumn<Order, Status>>(c => c.Add(x => x.Property, o => o.Status).Add(x => x.Statuses, Statuses).Add(x => x.Title, "状态"));
            configure?.Invoke(p);
        });

    static string[] Column(IRenderedComponent<DataTable<Order>> cut, int position) =>
        [.. cut.FindAll("tbody tr[data-key]").Select(row => row.Children[position].TextContent.Trim())];

    [Fact]
    public void 列标题取Display_值按类型格式化_数字靠结束边_状态显示成标记_合计精确相加()
    {
        var cut = RenderOrders();

        Assert.Equal(["Id", "客户", "Amount", "状态"], cut.FindAll("thead th").Select(th => th.TextContent.Trim()));
        Assert.Equal(["A-3", "A-1", "A-2"], Column(cut, 0));
        Assert.Equal("¥1,200.50", Column(cut, 2)[0]);
        Assert.Equal("已付款", cut.Find("tbody tr [data-status]").TextContent.Trim());
        Assert.Equal(("合计", "¥1,610.50"), (cut.Find("tfoot td").TextContent.Trim(), cut.FindAll("tfoot td")[2].TextContent.Trim()));
        Assert.Equal("5", cut.Find("table").GetAttribute("aria-rowcount"));
        Assert.Contains("inset-inline-start:0px", cut.Find("tbody tr[data-key] td").GetAttribute("style"));
    }

    [Fact]
    public void 点表头_正序倒序再回到默认顺序_本地按语言排序()
    {
        ColumnSort? sort = null;
        var cut = RenderOrders(p => p.Add(x => x.Sort, sort).Add(x => x.SortChanged, v => sort = v));

        cut.FindAll("thead button")[0].Click();
        Assert.Equal(new ColumnSort("customer", SortDirection.Ascending), sort);
        cut.Render(p => p.Add(x => x.Sort, sort));
        // 中文按拼音：安澜、北辰、启航
        Assert.Equal(["安澜", "北辰", "启航"], Column(cut, 1));
        Assert.Equal("ascending", cut.FindAll("thead th")[1].GetAttribute("aria-sort"));

        cut.FindAll("thead button")[0].Click();
        cut.Render(p => p.Add(x => x.Sort, sort));
        Assert.Equal(["启航", "北辰", "安澜"], Column(cut, 1));

        cut.FindAll("thead button")[0].Click();
        Assert.Null(sort);
    }

    [Fact]
    public void 服务端排序时只写回排序_不在本地排()
    {
        var cut = RenderOrders(p => p.Add(x => x.Sort, new ColumnSort("amount", SortDirection.Descending)).Add(x => x.ManualSort, true));

        Assert.Equal(["A-3", "A-1", "A-2"], Column(cut, 0));
        Assert.Equal("descending", cut.FindAll("thead th")[2].GetAttribute("aria-sort"));
    }

    [Fact]
    public void 多选_勾一行_全选跳过不能勾的行_再点取消()
    {
        IReadOnlyList<string> selected = [];
        var cut = RenderOrders(p => p.Add(x => x.SelectionMode, SelectionMode.Multiple)
            .Add(x => x.Selected, selected).Add(x => x.SelectedChanged, v => selected = v)
            .Add(x => x.IsRowSelectable, o => o.Status != Status.Draft));

        cut.FindAll("tbody input[type=checkbox]")[0].Change(true);
        Assert.Equal(["A-3"], selected);

        cut.Render(p => p.Add(x => x.Selected, selected));
        Assert.Equal("selected", cut.Find("tbody tr[data-key]").GetAttribute("data-state"));
        cut.Find("thead input[type=checkbox]").Change(true);
        Assert.Equal(["A-3", "A-2"], selected);

        cut.Render(p => p.Add(x => x.Selected, selected));
        cut.Find("thead input[type=checkbox]").Change(false);
        Assert.Empty(selected);
    }

    [Fact]
    public void 没有数据时显示空状态_加载中换成骨架()
    {
        var cut = Render<DataTable<Order>>(p => p.Add(x => x.Items, []).Add(x => x.GetKey, o => o.Id).Add(x => x.EmptyText, "还没有订单")
            .AddChildContent<PropertyColumn<Order, string>>(c => c.Add(x => x.Property, o => o.Id)));

        Assert.Contains("还没有订单", cut.Find("tbody").TextContent);

        cut.Render(p => p.Add(x => x.Loading, true));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr[aria-hidden]")), TimeSpan.FromSeconds(2));
        Assert.Equal("true", cut.Find("table").GetAttribute("aria-busy"));
    }

    [Fact]
    public void 表头筛选_列出出现过的值_状态按顺序显示名字_本地筛选()
    {
        IReadOnlyDictionary<string, IReadOnlyList<string>> filters = new Dictionary<string, IReadOnlyList<string>>();
        var cut = Render<DataTable<Order>>(p =>
        {
            p.Add(x => x.Items, Orders).Add(x => x.GetKey, o => o.Id).Add(x => x.Filters, filters).Add(x => x.FiltersChanged, v => filters = v);
            p.AddChildContent<PropertyColumn<Order, string>>(c => c.Add(x => x.Property, o => o.Customer).Add(x => x.Filter, ColumnFilterKind.Values));
            p.AddChildContent<StatusColumn<Order, Status>>(c => c.Add(x => x.Property, o => o.Status).Add(x => x.Statuses, Statuses).Add(x => x.Filter, ColumnFilterKind.Values));
            p.AddChildContent<PropertyColumn<Order, string>>(c => c.Add(x => x.Property, o => o.Id).Add(x => x.Filter, ColumnFilterKind.Keyword));
        });

        var buttons = cut.FindComponents<ColumnFilterButton>();
        Assert.Equal(["安澜", "北辰", "启航"], buttons[0].Instance.Options!.Select(o => o.Label));
        Assert.Equal(["草稿", "已付款"], buttons[1].Instance.Options!.Select(o => o.Label));
        Assert.Null(buttons[2].Instance.Options);

        cut.InvokeAsync(() => buttons[1].Instance.ValuesChanged.InvokeAsync(["Paid"]));
        cut.Render(p => p.Add(x => x.Filters, filters));
        Assert.Equal(["A-3", "A-2"], cut.FindAll("tbody tr[data-key]").Select(row => row.GetAttribute("data-key")));

        cut.InvokeAsync(() => cut.FindComponents<ColumnFilterButton>()[2].Instance.ValuesChanged.InvokeAsync(["a-2"]));
        cut.Render(p => p.Add(x => x.Filters, filters));
        Assert.Equal(["A-2"], cut.FindAll("tbody tr[data-key]").Select(row => row.GetAttribute("data-key")));
    }

    [Fact]
    public void 列菜单_固定到结尾_前后移动_隐藏_列宽盖过列定义()
    {
        IReadOnlyDictionary<string, ColumnSticky?> pins = new Dictionary<string, ColumnSticky?>();
        IReadOnlyList<string> order = [];
        IReadOnlyList<string> hidden = [];
        var cut = RenderOrders(p => p.Add(x => x.ColumnMenu, true)
            .Add(x => x.ColumnPins, pins).Add(x => x.ColumnPinsChanged, v => pins = v)
            .Add(x => x.ColumnOrder, order).Add(x => x.ColumnOrderChanged, v => order = v)
            .Add(x => x.HiddenColumns, hidden).Add(x => x.HiddenColumnsChanged, v => hidden = v)
            .Add(x => x.ColumnWidths, new Dictionary<string, double> { ["customer"] = 200 }));
        string[] Headers() => [.. cut.FindAll("thead th").Select(th => th.GetAttribute("data-column")!)];
        Task Act(string key, ColumnMenuAction action) =>
            cut.InvokeAsync(() => cut.FindComponents<ColumnMenuButton>().Single(m => m.Instance.Label == key).Instance.OnAction.InvokeAsync(action));

        Assert.Equal("width:200px", cut.Find("col[data-column=customer]").GetAttribute("style"));

        Act("客户", ColumnMenuAction.PinEnd);
        cut.Render(p => p.Add(x => x.ColumnPins, pins));
        Assert.Equal(["id", "amount", "status", "customer"], Headers());
        Assert.Contains("inset-inline-end:0px", cut.Find("th[data-column=customer]").GetAttribute("style"));

        Act("Amount", ColumnMenuAction.MoveForward);
        cut.Render(p => p.Add(x => x.ColumnOrder, order));
        Assert.Equal(["id", "status", "amount", "customer"], Headers());

        Act("状态", ColumnMenuAction.Hide);
        cut.Render(p => p.Add(x => x.HiddenColumns, hidden));
        Assert.Equal(["id", "amount", "customer"], Headers());
    }

    [Fact]
    public void 分组表头_相邻同组的列共用标题_没分组的列跨两行()
    {
        var cut = Render<DataTable<Order>>(p =>
        {
            p.Add(x => x.Items, Orders).Add(x => x.GetKey, o => o.Id);
            p.AddChildContent<PropertyColumn<Order, string>>(c => c.Add(x => x.Property, o => o.Id));
            p.AddChildContent<PropertyColumn<Order, string>>(c => c.Add(x => x.Property, o => o.Customer).Add(x => x.Group, "基本"));
            p.AddChildContent<PropertyColumn<Order, decimal>>(c => c.Add(x => x.Property, o => o.Amount).Add(x => x.Group, "基本"));
        });

        var rows = cut.FindAll("thead tr");
        Assert.Equal(2, rows.Count);
        Assert.Equal(("2", "Id"), (rows[0].Children[0].GetAttribute("rowspan"), rows[0].Children[0].TextContent.Trim()));
        Assert.Equal(("colgroup", "2", "基本"), (rows[0].Children[1].GetAttribute("scope"), rows[0].Children[1].GetAttribute("colspan"), rows[0].Children[1].TextContent.Trim()));
        Assert.Equal(["客户", "Amount"], rows[1].Children.Select(th => th.TextContent.Trim()));
        Assert.Equal("3", cut.Find("tbody tr[data-key]").GetAttribute("aria-rowindex"));
    }

    [Fact]
    public void 详情行_展开按钮指向详情_没有详情的行不能展开()
    {
        IReadOnlyList<string> expanded = [];
        var cut = RenderOrders(p => p.Add(x => x.DetailContent, o => b => b.AddContent(0, $"{o.Id} 的详情"))
            .Add(x => x.HasDetail, o => o.Status == Status.Paid)
            .Add(x => x.ExpandedRows, expanded).Add(x => x.ExpandedRowsChanged, v => expanded = v));

        var toggles = cut.FindAll("tbody button[aria-expanded]");
        Assert.Equal(2, toggles.Count);
        toggles[0].Click();
        Assert.Equal(["A-3"], expanded);

        cut.Render(p => p.Add(x => x.ExpandedRows, expanded));
        var detail = cut.Find("tbody tr[data-detail] td");
        Assert.Equal("A-3 的详情", detail.TextContent.Trim());
        Assert.Equal(detail.Id, cut.Find("tbody button[aria-expanded=true]").GetAttribute("aria-controls"));
        // 详情也算一行：后面的行号顺延
        Assert.Equal("4", cut.FindAll("tbody tr[data-key]")[1].GetAttribute("aria-rowindex"));
        Assert.Equal("6", cut.Find("table").GetAttribute("aria-rowcount"));
    }
}
