using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>横幅与结果页：关闭经 JS 离场后才报告、播报方式；结果页的状态默认值与显式写的优先、状态码对读屏隐藏。</summary>
public class BannerResultTests : BunitContext
{
    public BannerResultTests() => Services.AddRuneUI();

    [Fact]
    public void 横幅_写了OnClose才有关闭按钮_离场结束才报告()
    {
        var closed = false;
        var cut = Render<Banner>(p => p
            .Add(x => x.Title, "系统维护")
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closed = true))
            .AddChildContent("今晚暂停服务"));

        var root = cut.Find("[data-scope=banner][data-part=root]");
        Assert.Equal("系统维护", cut.Find("strong").TextContent);
        Assert.True(root.HasAttribute("data-rune-dismissible"));
        // 关闭按钮保留自己的部件标记（同 vue-tsx-pro），由 JS 先播放离场
        Assert.True(cut.Find("button[data-rune-dismiss]").GetAttribute("data-scope") == "button");
        root.TriggerEvent("onrunedismiss", EventArgs.Empty);
        Assert.True(closed);

        var plain = Render<Banner>(p => p.Add(x => x.Live, AlertLive.Polite).AddChildContent("已归档"));
        Assert.Empty(plain.FindAll("button"));
        Assert.Equal("status", plain.Find("div").GetAttribute("role"));
    }

    [Fact]
    public void 结果页_状态给出默认文字与状态码_显式写的优先()
    {
        var cut = Render<Result>(p => p.Add(x => x.Status, ResultStatus.Forbidden));
        Assert.Equal("403", cut.Find("[data-part=code]").TextContent);
        Assert.Equal("true", cut.Find("[data-part=code]").GetAttribute("aria-hidden"));
        Assert.Equal("没有访问权限", cut.Find("[data-part=title]").TextContent);
        Assert.Equal("warning", cut.Find("[data-part=root]").GetAttribute("data-tone"));

        var custom = Render<Result>(p => p
            .Add(x => x.Status, ResultStatus.Forbidden)
            .Add(x => x.Description, b => b.AddContent(0, "需要“财务查看”权限")));
        Assert.Equal("没有访问权限", custom.Find("[data-part=title]").TextContent);
        Assert.Equal("需要“财务查看”权限", custom.Find("[data-part=description]").TextContent);

        var success = Render<Result>(p => p.Add(x => x.Title, b => b.AddContent(0, "提交成功")));
        Assert.Equal("success", success.Find("[data-part=root]").GetAttribute("data-tone"));
        Assert.Empty(success.FindAll("[data-part=code]"));
    }
}
