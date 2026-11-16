using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.Dates;

/// <summary>
/// 留言（同 vue-tsx-pro 的 CommentThread、CommentComposer）：回复只分一层；不给 OnSend 时只读；
/// 回复的输入框带“@作者 ”、以“回复 某人”为名，发送时带上 ReplyTo；失败时保留内容并提示；Ctrl + Enter 发送、Esc 取消。
/// </summary>
public class CommentThreadTests : BunitContext
{
    public CommentThreadTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-24T09:00:00+08:00");

    static readonly IReadOnlyList<CommentItem> Comments =
    [
        new("c1", "华辰-李工", "强度校核见附件。@方圆 麻烦确认", Now.AddDays(-3))
        {
            Attachments = [new("支架强度校核.pdf") { Url = "/files/1", Size = 1_240_000 }],
            Replies = [new("c2", "方圆", "已重算", Now.AddDays(-2)) { Edited = true }],
        },
        new("c3", "陆川", "交期能不能保证？", Now.AddHours(-1)),
    ];

    static readonly IReadOnlyList<ListboxOption> People = [new("E2007", "方圆")];

    [Fact]
    public void 只读时没有输入框与回复按钮_回复嵌在那一串里_附件带大小()
    {
        var cut = Render<CommentThread>(p => p.Add(x => x.Comments, Comments).Add(x => x.People, People).Add(x => x.Now, Now));

        Assert.Equal(["华辰-李工", "方圆", "陆川"], cut.FindAll("article").Select(a => a.GetAttribute("aria-label")));
        Assert.Equal("1 条回复", cut.Find("ul[aria-label$=\"回复\"]").GetAttribute("aria-label"));
        Assert.Contains("已编辑", cut.FindAll("article")[1].TextContent);
        Assert.Equal("/files/1", cut.Find("[data-part=\"attachment\"] a").GetAttribute("href"));
        // @名字按 People 高亮
        Assert.Contains("@方圆", cut.Find("[data-part=\"content\"] span").TextContent);
        Assert.Empty(cut.FindAll("textarea"));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "回复");
    }

    [Fact]
    public async Task 回复的输入框带作者_发送时带上那一串_成功后收起()
    {
        CommentDraft? sent = null;
        var cut = Render<CommentThread>(p => p
            .Add(x => x.Comments, Comments)
            .Add(x => x.Now, Now)
            .Add(x => x.OnSend, draft => sent = draft));

        // 回复一条回复：仍挂在第一串下面
        cut.FindAll("article")[1].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "回复").Click();
        var reply = cut.FindAll("textarea")[0];
        Assert.Equal("@方圆 ", reply.GetAttribute("value"));
        Assert.Equal("回复 方圆", reply.GetAttribute("aria-label"));
        Assert.NotNull(reply.GetAttribute("data-rune-focus-end"));

        await cut.InvokeAsync(() => reply.Input("@方圆 收到"));
        await cut.InvokeAsync(() => cut.FindAll("textarea")[0].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "send" }));

        Assert.Equal("@方圆 收到", sent?.Content);
        Assert.Equal("c1", sent?.ReplyTo);
        // 收起回复的输入框，只剩最下面的一个
        Assert.Single(cut.FindAll("textarea"));
    }

    [Fact]
    public async Task 发送失败时保留内容并提示_Esc取消回复()
    {
        var cut = Render<CommentThread>(p => p
            .Add(x => x.Comments, Comments)
            .Add(x => x.OnSend, _ => throw new InvalidOperationException("网络错误")));
        var composer = cut.FindAll("textarea").Last();

        await cut.InvokeAsync(() => composer.Input("新的一条"));
        cut.FindAll("button").Last(b => b.TextContent.Trim() == "发送").Click();

        Assert.Equal(Lang.ZhCn.Patterns.CommentSendFailed, cut.Find("[role=\"alert\"]").TextContent.Trim());
        Assert.Equal("新的一条", cut.FindAll("textarea").Last().GetAttribute("value"));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "回复").Click();
        Assert.Equal(2, cut.FindAll("textarea").Count);
        await cut.InvokeAsync(() => cut.FindAll("textarea")[0].TriggerEvent("onrunecommand", new CommandEventArgs { Name = "cancel" }));
        Assert.Single(cut.FindAll("textarea"));
    }

    [Fact]
    public void 输入框_空的不能发送_有作者时有头像()
    {
        var cut = Render<CommentComposer>(p => p
            .Add(x => x.OnSend, _ => { })
            .Add(x => x.Author, new CommentAuthor("方圆")));

        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Trim() == "发送").HasAttribute("disabled"));
        Assert.Single(cut.FindAll("[data-scope=\"avatar\"]"));
        Assert.Equal("send", cut.Find("textarea").GetAttribute("data-rune-ctrl-enter"));
        // 不是回复：没有取消
        Assert.Null(cut.Find("textarea").GetAttribute("data-rune-cancel"));
    }
}
