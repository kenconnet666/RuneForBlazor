using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>图片、图片组与图片预览：结构、名称与切换（平移缩放在 JS 里，见端到端测试）。</summary>
public class ImageTests : BunitContext
{
    public ImageTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 不能预览时外层是span_未声明的属性落到img上_尺寸写成变量()
    {
        var cut = Render<Image>(p => p
            .Add(x => x.Src, "/a.png").Add(x => x.Alt, "支架正面")
            .Add(x => x.Width, (CssLength)160).Add(x => x.Height, (CssLength)120)
            .AddUnmatched("data-testid", "photo"));
        var root = cut.Find("span");
        Assert.Contains("--rune-image-width:160px", root.GetAttribute("style"));
        Assert.Contains("--rune-image-height:120px", root.GetAttribute("style"));
        var image = cut.Find("img");
        Assert.Equal(("/a.png", "支架正面", "photo"), (image.GetAttribute("src"), image.GetAttribute("alt"), image.GetAttribute("data-testid")));
        Assert.Equal(("lazy", "async"), (image.GetAttribute("loading"), image.GetAttribute("decoding")));
        Assert.True(image.HasAttribute("data-rune-image"));
        // 失败的占位平时由 CSS 藏起来；露出来时是一张图，名称是 Alt
        var fallback = cut.Find("[role=img]");
        Assert.Equal(("支架正面", "图片加载失败"), (fallback.GetAttribute("aria-label"), fallback.GetAttribute("aria-description")));
        Assert.Empty(cut.FindAll("button, dialog"));
    }

    [Fact]
    public void 单张可以预览时是按钮_直接打开自己的预览_大图懒加载()
    {
        var cut = Render<Image>(p => p
            .Add(x => x.Src, "/thumb.png").Add(x => x.Alt, "支架正面").Add(x => x.Preview, true)
            .Add(x => x.PreviewSrc, "/full.png").Add(x => x.DownloadName, "bracket.png"));
        var button = cut.Find("button[aria-haspopup=dialog]");
        Assert.Equal("查看大图：支架正面", button.GetAttribute("aria-label"));
        var dialog = cut.Find("dialog");
        Assert.Equal(dialog.Id, button.GetAttribute("data-rune-open"));
        Assert.Equal(("图片预览", "false"), (dialog.GetAttribute("aria-label"), dialog.GetAttribute("data-open")));

        var viewport = cut.Find("[data-rune-lightbox-viewport]");
        Assert.Equal(("img", "支架正面", "0"), (viewport.GetAttribute("role"), viewport.GetAttribute("aria-label"), viewport.GetAttribute("tabindex")));
        var full = cut.Find("img[data-rune-lightbox-image]");
        Assert.Equal(("/full.png", "lazy"), (full.GetAttribute("src"), full.GetAttribute("loading")));
        var download = cut.Find("a[download]");
        Assert.Equal(("/full.png", "bracket.png"), (download.GetAttribute("href"), download.GetAttribute("download")));
        // 只有一张：没有计数、没有左右切换；关着时不预先加载
        Assert.Equal("", cut.Find("[aria-live=polite]").TextContent);
        Assert.Empty(cut.FindAll("[data-rune-lightbox-step]"));
        Assert.Empty(cut.FindAll("img[hidden]"));
    }

    [Fact]
    public void 组里的图共用一个预览_从点的那张开始_到头为止()
    {
        var cut = Render<ImageGroup>(p => p.Add(x => x.ChildContent, Photos("甲", "乙", "丙")));
        Assert.Single(cut.FindAll("dialog"));
        Assert.Empty(cut.FindAll("[data-rune-open]"));

        cut.FindAll("button[aria-haspopup=dialog]")[1].Click();
        Assert.Equal("true", cut.Find("dialog").GetAttribute("data-open"));
        Assert.Equal("第 2 张，共 3 张", cut.Find("[aria-live=polite]").TextContent);
        Assert.Equal("/乙.png", cut.Find("img[data-rune-lightbox-image]").GetAttribute("src"));
        // 打开时预先加载前后两张
        Assert.Equal(["/甲.png", "/丙.png"], cut.FindAll("img[hidden]").Select(i => i.GetAttribute("src")));

        cut.Find("[data-rune-lightbox-step=next]").Click();
        Assert.Equal("/丙.png", cut.Find("img[data-rune-lightbox-image]").GetAttribute("src"));
        Assert.Empty(cut.FindAll("[data-rune-lightbox-step=next]"));
        Assert.Equal("上一张", cut.Find("[data-rune-lightbox-step=previous]").GetAttribute("aria-label"));

        // Esc 关上（JS 发来 rune-toggle）：写回组里
        cut.Find("dialog").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = false, Reason = "escape" });
        Assert.Equal("false", cut.Find("dialog").GetAttribute("data-open"));
    }

    [Fact]
    public void Loop时到头从另一头继续()
    {
        var cut = Render<ImageGroup>(p => p.Add(x => x.Loop, true).Add(x => x.ChildContent, Photos("甲", "乙")));
        cut.FindAll("button[aria-haspopup=dialog]")[1].Click();
        cut.Find("[data-rune-lightbox-step=next]").Click();
        Assert.Equal("/甲.png", cut.Find("img[data-rune-lightbox-image]").GetAttribute("src"));
        cut.Find("[data-rune-lightbox-step=previous]").Click();
        Assert.Equal("/乙.png", cut.Find("img[data-rune-lightbox-image]").GetAttribute("src"));
    }

    [Fact]
    public void 受控的预览_IndexChanged与OnOpenChange()
    {
        var index = 0;
        OverlayOpenChange? change = null;
        LightboxItem[] items = [new("/a.png", "甲") { Downloadable = false }, new("/b.png", "乙")];
        var cut = Render<Lightbox>(p => p
            .Add(x => x.Items, items).Add(x => x.Open, true)
            .Add(x => x.IndexChanged, (int i) => index = i)
            .Add(x => x.OnOpenChange, (OverlayOpenChange e) => change = e));
        Assert.Empty(cut.FindAll("a[download]"));
        cut.Find("[data-rune-lightbox-step=next]").Click();
        Assert.Equal(1, index);

        cut.Find("dialog").TriggerEvent("onrunetoggle", new OverlayToggleEventArgs { Open = false, Reason = "close" });
        Assert.Equal(new OverlayOpenChange(false, OverlayChangeReason.Close), change);
    }

    static RenderFragment Photos(params string[] names) => builder =>
    {
        foreach (var name in names)
        {
            builder.OpenComponent<Image>(0);
            builder.AddAttribute(1, nameof(Image.Src), $"/{name}.png");
            builder.AddAttribute(2, nameof(Image.Alt), name);
            builder.AddAttribute(3, nameof(Image.Preview), true);
            builder.CloseComponent();
        }
    };
}
