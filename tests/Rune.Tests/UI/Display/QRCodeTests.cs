using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>二维码：一条 path 的 svg（role="img"，默认名称“二维码”）、静区、logo、状态层与刷新。</summary>
public class QRCodeTests : BunitContext
{
    public QRCodeTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 一条path的svg_四周留静区_名称默认二维码()
    {
        var cut = Render<QRCode>(p => p.Add(x => x.Value, "https://example.com"));
        var svg = cut.Find("svg");
        Assert.Equal(("img", "二维码"), (svg.GetAttribute("role"), svg.GetAttribute("aria-label")));
        // 版本 2（25 个模块）加两边各 2 个模块的静区
        Assert.Equal("0 0 29 29", svg.GetAttribute("viewBox"));
        // 左上角的定位图形第一行是 7 个深色模块
        Assert.StartsWith("M2 2h7v1h-7z", cut.Find("path").GetAttribute("d"));

        var named = Render<QRCode>(p => p.Add(x => x.Value, "x").AddUnmatched("aria-label", "分享链接"));
        Assert.Equal("分享链接", named.Find("svg").GetAttribute("aria-label"));
        Assert.Null(named.Find("[data-part=root]").GetAttribute("aria-label"));
    }

    [Fact]
    public void 有logo时纠错默认H_logo底板对读屏隐藏()
    {
        var plain = Render<QRCode>(p => p.Add(x => x.Value, "https://example.com"));
        var branded = Render<QRCode>(p => p.Add(x => x.Value, "https://example.com").Add(x => x.LogoSrc, "/logo.svg"));
        Assert.NotEqual(plain.Find("path").GetAttribute("d"), branded.Find("path").GetAttribute("d"));
        Assert.Equal("true", branded.Find("[data-part=logo]").GetAttribute("aria-hidden"));
        Assert.Equal("/logo.svg", branded.Find("[data-part=logo] img").GetAttribute("src"));
    }

    [Fact]
    public void 状态层_已过期可以刷新_已扫描()
    {
        var refreshed = 0;
        var cut = Render<QRCode>(p => p.Add(x => x.Value, "ticket").Add(x => x.Status, QRCodeStatus.Expired).Add(x => x.OnRefresh, () => refreshed++));
        var overlay = cut.Find("[role=status]");
        Assert.Contains("二维码已过期", overlay.TextContent);
        cut.Find("[role=status] button").Click();
        Assert.Equal(1, refreshed);

        cut.Render(p => p.Add(x => x.Status, QRCodeStatus.Scanned));
        Assert.Empty(cut.FindAll("[role=status] button"));
        Assert.NotEqual("", cut.Find("[role=status]").TextContent.Trim());

        cut.Render(p => p.Add(x => x.Status, QRCodeStatus.Active));
        Assert.Equal("", cut.Find("[role=status]").TextContent.Trim());
    }
}
