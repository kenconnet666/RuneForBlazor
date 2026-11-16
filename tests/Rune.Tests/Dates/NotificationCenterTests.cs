using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.Dates;

/// <summary>
/// 通知中心（同 vue-tsx-pro 的 NotificationCenter、NotificationList）：按类型筛选、只看未读、筛空与没有通知分开说；
/// 点一条报告已读与点击，全部标为已读只报告未读的；铃铛的名称带未读数，点了一条面板收起。
/// </summary>
public class NotificationCenterTests : BunitContext
{
    public NotificationCenterTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-21T10:00:00+08:00");

    static readonly IReadOnlyList<NotificationItem> Items =
    [
        new("1", "新的待办：资料审核", Now.AddMinutes(-18)) { Type = "todo", Href = "/flows/1" },
        new("2", "被退回：方案设计", Now.AddHours(-2)) { Type = "returned", Tone = Tone.Warning },
        new("3", "会签已完成", Now.AddDays(-1)) { Type = "todo", Read = true },
    ];

    static readonly IReadOnlyList<NotificationType> Types = [new("todo", "待办"), new("returned", "退回")];

    static IEnumerable<string> Titles(IRenderedComponent<NotificationList> cut) =>
        cut.FindAll("[data-part=\"itemTitle\"]").Select(t => t.TextContent.Replace("未读", "").Trim());

    [Fact]
    public void 按类型筛选_只看未读_筛空时说没有符合条件的()
    {
        var cut = Render<NotificationList>(p => p.Add(x => x.Items, Items).Add(x => x.Types, Types).Add(x => x.Now, Now));

        Assert.Equal(["新的待办：资料审核", "被退回：方案设计", "会签已完成"], Titles(cut));
        // 未读的标题前念出“未读”，时间相对现在
        Assert.Contains("未读", cut.FindAll("[data-part=\"itemTitle\"]")[0].TextContent);
        Assert.Equal("18分钟前", cut.FindAll("time")[0].TextContent.Trim());

        cut.FindAll("[role=\"radio\"], button[aria-pressed]").First(b => b.TextContent.Trim() == "待办").Click();
        Assert.Equal(["新的待办：资料审核", "会签已完成"], Titles(cut));

        cut.Find("input[role=\"switch\"]").Change(true);
        Assert.Equal(["新的待办：资料审核"], Titles(cut));

        cut.FindAll("[role=\"radio\"], button[aria-pressed]").First(b => b.TextContent.Trim() == "退回").Click();
        cut.Render(p => p.Add(x => x.Items, [.. Items.Select(i => i with { Read = true })]));
        Assert.Contains(Lang.ZhCn.Patterns.NotificationsNoMatch, cut.Markup);
    }

    [Fact]
    public void 点一条报告已读与点击_有地址是链接_全部标为已读只报告未读的()
    {
        var read = new List<IReadOnlyList<string>>();
        NotificationItem? clicked = null;
        var cut = Render<NotificationList>(p => p
            .Add(x => x.Items, Items)
            .Add(x => x.OnRead, ids => read.Add(ids))
            .Add(x => x.OnItemClick, item => clicked = item));

        var items = cut.FindAll("[data-part=\"item\"]");
        Assert.Equal("a", items[0].TagName.ToLowerInvariant());
        Assert.Equal("/flows/1", items[0].GetAttribute("href"));
        Assert.Equal("button", items[1].TagName.ToLowerInvariant());

        items[1].Click();
        Assert.Equal(["2"], read.Single());
        Assert.Equal("2", clicked?.Id);

        // 读过的再点：只报告点击
        cut.FindAll("[data-part=\"item\"]")[2].Click();
        Assert.Single(read);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "全部标为已读").Click();
        Assert.Equal(["1", "2"], read[1]);
    }

    [Fact]
    public void 没有通知时显示Empty_没写时是默认的说明()
    {
        var cut = Render<NotificationList>(p => p.Add(x => x.Items, []));
        Assert.Contains(Lang.ZhCn.Patterns.NotificationsEmpty, cut.Markup);
        Assert.True(cut.FindAll("button").First(b => b.TextContent.Trim() == "全部标为已读").HasAttribute("disabled"));

        cut.Render(p => p.Add(x => x.Empty, b => b.AddContent(0, "都处理完了")));
        Assert.Contains("都处理完了", cut.Markup);
    }

    [Fact]
    public void 铃铛的名称带未读数_点了一条面板收起()
    {
        var open = true;
        var cut = Render<NotificationCenter>(p => p
            .Add(x => x.Items, Items)
            .Add(x => x.Now, Now)
            .Add(x => x.Open, open)
            .Add(x => x.OpenChanged, v => open = v));

        Assert.Equal("通知，未读 2 条", cut.Find("button[aria-haspopup]").GetAttribute("aria-label"));
        cut.FindAll("[data-part=\"item\"]")[1].Click();
        Assert.False(open);
    }
}
