using Bunit;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>
/// 主题编辑器与缩略图（同 vue-tsx-pro 的 ThemeCustomizer、ThemeThumbnail）：选色块写回设置、当前取值写在旋钮旁；
/// 对比度按套用后的主题检查；导出的代码以基础主题开头；恢复默认在没改时不可用。缩略图套上那套主题的类、对读屏隐藏。
/// </summary>
public class ThemeCustomizerTests : BunitContext
{
    public ThemeCustomizerTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void 缩略图套上主题的类_对读屏隐藏()
    {
        var cut = Render<ThemeThumbnail>(p => p.Add(x => x.Theme, Themes.Forest));

        var root = cut.Find("[data-scope=\"theme-thumbnail\"]");
        Assert.Equal("true", root.GetAttribute("aria-hidden"));
        Assert.Equal("forest", root.GetAttribute("data-theme"));
        // 主题类按内容哈希，以 rt 开头
        Assert.Contains(root.ClassList, name => name.StartsWith("rt", StringComparison.Ordinal));
    }

    [Fact]
    public void 选色块写回设置_当前取值写在旋钮旁_改了以后能恢复默认()
    {
        ThemeSettings settings = new();
        var cut = Render<ThemeCustomizer>(p => p
            .Add(x => x.Base, Themes.Light)
            .Bind(x => x.Settings, settings, v => settings = v));

        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Trim() == "恢复默认").HasAttribute("disabled"));
        // 内置主题全部通过对比度检查
        Assert.Contains(Lang.ZhCn.ThemeCustomizer.ContrastOk(ThemeContrast.Pairs.Count), cut.Markup);
        Assert.Contains("basedOn: Themes.Light", cut.Markup);

        cut.Find("button[title^=\"绯红\"]").Click();
        Assert.Equal(AccentScale.Crimson, settings.Accent);

        cut.Render(p => p.Add(x => x.Settings, settings));
        Assert.Contains("绯红 #e93d82", cut.Markup);
        Assert.Contains("[Tones.Accent.Solid] = &quot;#e93d82&quot;", cut.Markup);
        Assert.False(cut.FindAll("button").Single(b => b.TextContent.Trim() == "恢复默认").HasAttribute("disabled"));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "大").Click();
        Assert.Equal(ThemeRadius.Large, settings.Radius);
        Assert.Equal(AccentScale.Crimson, settings.Accent);
    }

    [Fact]
    public void 暗色主题的色块取暗色色阶_下载链接带着代码()
    {
        var cut = Render<ThemeCustomizer>(p => p.Add(x => x.Base, Themes.Dark).Add(x => x.Settings, new ThemeSettings { Neutral = NeutralScale.Mauve }));

        Assert.Contains("background-color: " + ThemeCustomization.Swatch(AccentScale.Crimson, dark: true), cut.Find("button[title^=\"绯红\"]").InnerHtml);
        var download = cut.Find("a[download=\"MyTheme.cs\"]");
        Assert.StartsWith("data:text/plain;charset=utf-8,", download.GetAttribute("href"));
        Assert.Contains(Uri.EscapeDataString("basedOn: Themes.Dark"), download.GetAttribute("href"));
    }
}
