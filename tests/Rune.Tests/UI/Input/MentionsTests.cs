using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>提及：文字里的提及怎么认、文本框交给 JS 的约定、@ 后面的文字过滤列表、提到的人随文字更新、MentionText。</summary>
public class MentionsTests : BunitContext
{
    public MentionsTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static readonly ListboxOption[] people = [new("a", "周一航"), new("b", "许悠"), new("c", "林小满"), new("d", "林小")];

    static IEnumerable<string?> Labels(IRenderedComponent<Mentions<ListboxOption>> cut) =>
        cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label"));

    [Fact]
    public void 提及取最长的名字_前面是字母数字的不算_提到的人按第一次出现去重()
    {
        var segments = MentionSegments.Of("请 @林小满 看看，抄送 lin@林小 与 @林小", people);
        Assert.Equal(["请 ", "@林小满", " 看看，抄送 lin@林小 与 ", "@林小"], segments.Select(s => s.Text));
        Assert.Equal(["c", "d"], segments.Where(s => s.Mention is not null).Select(s => s.Mention!.Value));

        Assert.Equal(["b", "a"], MentionSegments.Values("@许悠 @周一航 @许悠", people));
        Assert.Empty(MentionSegments.Of(null, people));
    }

    [Fact]
    public void 文本框是列表宿主_弹层贴着光标_列表与弹层由属性指出()
    {
        var cut = Render<Mentions<ListboxOption>>(p => p.Add(x => x.Items, people).AddUnmatched("aria-label", "留言"));

        var textarea = cut.Find("textarea");
        var panel = cut.Find("[popover]");
        Assert.Equal("@", textarea.GetAttribute("data-rune-mentions"));
        Assert.Equal("list", textarea.GetAttribute("aria-autocomplete"));
        Assert.Equal("text", textarea.GetAttribute("data-rune-list"));
        Assert.Equal(cut.Find("[role=listbox]").Id, textarea.GetAttribute("aria-controls"));
        Assert.Equal(panel.Id, textarea.GetAttribute("data-rune-popover"));
        Assert.Equal(textarea.Id, panel.GetAttribute("data-rune-mentions-panel"));
        Assert.Equal(textarea.Id, panel.GetAttribute("data-caret-anchor"));
        Assert.Equal("留言", textarea.GetAttribute("aria-label"));
    }

    [Fact]
    public void 按At后面的文字过滤_清空后是全部()
    {
        var queries = new List<string>();
        var cut = Render<Mentions<ListboxOption>>(p => p
            .Add(x => x.Items, people)
            .Add(x => x.OnQuery, EventCallback.Factory.Create<string>(this, queries.Add)));

        cut.Find("textarea").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "mention-query", Text = "林" });
        Assert.Equal(["林小满", "林小"], Labels(cut));
        Assert.Equal("林", cut.Find("[role=listbox]").GetAttribute("data-rune-query"));

        cut.Find("textarea").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "mention-query", Text = "" });
        Assert.Equal(4, cut.FindAll("[role=option]").Count);
        Assert.Equal(["林", ""], queries);
    }

    [Fact]
    public void 提到的人随文字更新_删掉名字就不再算()
    {
        string? text = null;
        IReadOnlyList<string>? mentioned = null;
        var cut = Render<Mentions<ListboxOption>>(p => p
            .Add(x => x.Items, people)
            .Add(x => x.ValueChanged, EventCallback.Factory.Create<string?>(this, v => text = v))
            .Add(x => x.MentionedChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => mentioned = v)));
        // 一开始没有文字：不报告
        Assert.Null(mentioned);

        cut.Find("textarea").Change("@林小满 和 @许悠 看一下，邮件 x@许悠");
        Assert.Equal("@林小满 和 @许悠 看一下，邮件 x@许悠", text);
        Assert.Equal(["c", "b"], mentioned);

        cut.Find("textarea").Change("@许悠 看一下");
        Assert.Equal(["b"], mentioned);
    }

    [Fact]
    public async Task 远程搜索_选中过的人在结果换掉之后仍认得出()
    {
        IReadOnlyList<string>? mentioned = null;
        var cut = Render<Mentions<ListboxOption>>(p => p
            .Add(x => x.Load, (query, _) => Task.FromResult(people.Where(o => o.Label.Contains(query, StringComparison.Ordinal))))
            .Add(x => x.MentionedChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => mentioned = v)));

        await cut.Find("textarea").TriggerEventAsync("onrunecommand", new CommandEventArgs { Name = "mention-query", Text = "周" });
        cut.WaitForAssertion(() => Assert.Equal(["周一航"], Labels(cut)), TimeSpan.FromSeconds(2));
        cut.Find("[role=option][data-label='周一航']").Click();

        await cut.Find("textarea").TriggerEventAsync("onrunecommand", new CommandEventArgs { Name = "mention-query", Text = "许" });
        cut.WaitForAssertion(() => Assert.Equal(["许悠"], Labels(cut)), TimeSpan.FromSeconds(2));

        // 远程结果回来后的那次渲染在后台线程排队：同步的 Change 可能排在它后面才处理，等它
        cut.Find("textarea").Change("@周一航 你好");
        cut.WaitForAssertion(() => Assert.Equal(["a"], mentioned), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void MentionText高亮提及_其余是原样的文字()
    {
        var cut = Render<MentionText>(p => p.Add(x => x.Text, "请 @林小满 看看\n谢谢").Add(x => x.Options, people));

        Assert.Equal("请 @林小满 看看\n谢谢", cut.Find("span").TextContent);
        Assert.Equal(["@林小满"], cut.FindAll("span span").Select(s => s.TextContent));
    }
}
