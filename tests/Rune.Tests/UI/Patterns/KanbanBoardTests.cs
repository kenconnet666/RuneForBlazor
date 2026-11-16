using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 看板（同 vue-tsx-pro 的 KanbanBoard）：按列分卡片与计数；不能放的列与原因先算好交给 JS；
/// JS 交来的 move、reject、reorder 报告给使用方（列内排序写回整个列表）；拿起时卡片内容拿到 Dragging。
/// </summary>
public class KanbanBoardTests : BunitContext
{
    public KanbanBoardTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    sealed record Ticket(string Id, string Stage);

    static readonly IReadOnlyList<KanbanColumn> Columns = [new("todo", "待处理"), new("doing", "处理中"), new("done", "已完成") { Tone = Tone.Success }];

    static readonly IReadOnlyList<Ticket> Tickets = [new("T1", "todo"), new("T2", "todo"), new("T3", "doing"), new("T4", "todo")];

    static readonly List<string> Order = ["todo", "doing", "done"];

    static string? Adjacent(Ticket ticket, string to) => Math.Abs(Order.IndexOf(to) - Order.IndexOf(ticket.Stage)) == 1 ? null : "只能挪到相邻的一列";

    IRenderedComponent<KanbanBoard<Ticket>> RenderBoard(Action<ComponentParameterCollectionBuilder<KanbanBoard<Ticket>>>? extra = null) =>
        Render<KanbanBoard<Ticket>>(p =>
        {
            p.Add(x => x.Columns, Columns)
                .Add(x => x.Items, Tickets)
                .Add(x => x.GetKey, t => t.Id)
                .Add(x => x.GetColumn, t => t.Stage)
                .Add(x => x.CanMove, Adjacent)
                .Add(x => x.ChildContent, card => b => b.AddContent(0, card.Dragging ? $"{card.Item.Id}（拖动中）" : card.Item.Id));
            extra?.Invoke(p);
        });

    [Fact]
    public void 按列分卡片_计数_空列显示提示_不能放的列与原因交给JS()
    {
        var cut = RenderBoard();

        var columns = cut.FindAll("[data-rune-kanban-column]");
        Assert.Equal(["todo", "doing", "done"], columns.Select(c => c.GetAttribute("data-rune-kanban-column")));
        Assert.Equal(["T1", "T2", "T4"], columns[0].QuerySelectorAll("[data-rune-kanban-card]").Select(c => c.GetAttribute("data-rune-kanban-card")));
        Assert.Equal("3", columns[0].QuerySelector("[data-part=\"count\"]")!.TextContent.Trim());
        Assert.Contains(Lang.ZhCn.Patterns.KanbanEmpty, columns[2].TextContent);
        // 列名是列的名称；把手指向操作说明
        Assert.Equal("待处理", cut.Find($"#{columns[0].GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("移动 T1", cut.Find("[data-rune-kanban-card=\"T1\"] [data-rune-kanban-handle]").GetAttribute("aria-label"));

        var deny = JsonSerializer.Deserialize<Dictionary<string, string>>(cut.Find("[data-rune-kanban-card=\"T1\"]").GetAttribute("data-deny")!);
        Assert.Equal(new Dictionary<string, string> { ["done"] = "只能挪到相邻的一列" }, deny);
        // 没写 OnReorder：列内不能排序
        Assert.False(cut.Find("[data-rune-kanban]").HasAttribute("data-reorderable"));
    }

    [Fact]
    public async Task JS交来的放下_报告移动与拒绝_列内排序写回整个列表()
    {
        KanbanMove<Ticket>? moved = null;
        KanbanReject<Ticket>? rejected = null;
        IReadOnlyList<Ticket>? reordered = null;
        var cut = RenderBoard(p => p
            .Add(x => x.OnMove, m => moved = m)
            .Add(x => x.OnReject, r => rejected = r)
            .Add(x => x.OnReorder, list => reordered = list));
        var root = () => cut.Find("[data-rune-kanban]");

        await cut.InvokeAsync(() => root().TriggerEvent("onrunecommand", new CommandEventArgs { Name = "move", Text = "T1\ndoing\n0" }));
        Assert.Equal(new KanbanMove<Ticket>(Tickets[0], "doing", 0), moved);

        await cut.InvokeAsync(() => root().TriggerEvent("onrunecommand", new CommandEventArgs { Name = "reject", Text = "T2\ndone" }));
        Assert.Equal(new KanbanReject<Ticket>(Tickets[1], "done", "只能挪到相邻的一列"), rejected);

        // T4 移到待处理的第一位：插到 T1 前面；别的列不动
        await cut.InvokeAsync(() => root().TriggerEvent("onrunecommand", new CommandEventArgs { Name = "reorder", Text = "T4\n0" }));
        Assert.Equal(["T4", "T1", "T2", "T3"], reordered!.Select(t => t.Id));
        // T1 移到列尾：接在列的最后一张后面
        await cut.InvokeAsync(() => root().TriggerEvent("onrunecommand", new CommandEventArgs { Name = "reorder", Text = "T1\n2" }));
        Assert.Equal(["T2", "T3", "T4", "T1"], reordered!.Select(t => t.Id));
        Assert.True(root().HasAttribute("data-reorderable"));
    }

    [Fact]
    public async Task 拿起时卡片内容拿到Dragging_放下后恢复()
    {
        var cut = RenderBoard();
        var root = () => cut.Find("[data-rune-kanban]");

        await cut.InvokeAsync(() => root().TriggerEvent("onrunecommand", new CommandEventArgs { Name = "grab", Text = "T3" }));
        Assert.Contains("T3（拖动中）", cut.Find("[data-rune-kanban-card=\"T3\"]").TextContent);
        Assert.Equal("dragging", cut.Find("[data-rune-kanban-card=\"T3\"]").GetAttribute("data-state"));

        await cut.InvokeAsync(() => root().TriggerEvent("onrunecommand", new CommandEventArgs { Name = "release" }));
        Assert.DoesNotContain("拖动中", cut.Markup);
    }
}
