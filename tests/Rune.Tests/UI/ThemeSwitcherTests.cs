using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class ThemeSwitcherTests : BunitContext
{
    public ThemeSwitcherTests()
    {
        Services.AddRuneUI(o => o.ThemeChoices = new ThemeChoices([Themes.Light, Themes.Dark, Themes.Paper]));
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 单选菜单项_选中的写aria_checked_点了另一项改值()
    {
        string? value = "name";
        var cut = Render<MenuRadioGroup>(p => p
            .Add(x => x.Label, "排序")
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, (string? v) => value = v)
            .Add(x => x.ChildContent, b =>
            {
                foreach (var (seq, item) in new[] { (0, "name"), (10, "date") })
                {
                    b.OpenComponent<MenuRadioItem>(seq);
                    b.AddAttribute(seq + 1, nameof(MenuRadioItem.Value), item);
                    b.AddAttribute(seq + 2, nameof(MenuRadioItem.ChildContent), (RenderFragment)(t => t.AddContent(0, item)));
                    b.CloseComponent();
                }
            }));

        var group = cut.Find("[role=group]");
        Assert.Equal("排序", cut.Find($"#{group.GetAttribute("aria-labelledby")}").TextContent);
        var items = cut.FindAll("[role=menuitemradio]");
        Assert.Equal(["true", "false"], items.Select(i => i.GetAttribute("aria-checked")));

        items[1].Click();
        Assert.Equal("date", value);
    }

    [Fact]
    public void 列出跟随系统与各套主题_文字取语言包_每项带主题名给JS()
    {
        var cut = Render<ThemeSwitcher>();
        var menu = cut.Find("[role=menu]");
        Assert.True(menu.HasAttribute("data-rune-theme-switcher"));
        var items = cut.FindAll("[data-rune-theme-choice]");
        Assert.Equal(["system", "light", "dark", "paper"], items.Select(i => i.GetAttribute("data-rune-theme-choice")));
        Assert.Equal(["跟随系统", "亮色", "暗色", "纸张"], items.Select(i => i.TextContent.Trim()));
        Assert.All(items, i => Assert.NotNull(i.QuerySelector("svg[data-rune-choice-icon]")));

        var trigger = cut.Find($"[popovertarget='{menu.Id}']");
        Assert.Equal("主题", trigger.GetAttribute("aria-label"));
        Assert.NotNull(trigger.QuerySelector("svg[data-rune-theme-icon]"));

        cut.Render(p => p.Add(x => x.System, false).Add(x => x.Labels, new Dictionary<string, string> { ["paper"] = "护眼" }));
        Assert.Equal(["亮色", "暗色", "护眼"], cut.FindAll("[data-rune-theme-choice]").Select(i => i.TextContent.Trim()));
    }

    [Fact]
    public void 开机脚本带上每套主题的底色_最外层的Provider输出可选主题()
    {
        var boot = Render<ThemeBootScript>();
        var config = boot.Find("script#rune-theme-config");
        using var json = JsonDocument.Parse(config.TextContent);
        Assert.Equal("rune-theme", json.RootElement.GetProperty("key").GetString());
        Assert.Equal("dark", json.RootElement.GetProperty("system").GetProperty("dark").GetString());
        // 没写 SystemContrast：不按对比度区分
        Assert.False(json.RootElement.GetProperty("system").TryGetProperty("contrast", out _));
        var paper = json.RootElement.GetProperty("themes").GetProperty("paper");
        Assert.Equal("light", paper.GetProperty("colorScheme").GetString());
        Assert.StartsWith("#", paper.GetProperty("background").GetString());

        var provider = Render<RuneProvider>(p => p.AddChildContent("<p>内容</p>"));
        using (Rune.Styles.StyleRegistry.Enter(new Rune.Styles.StyleRegistry()))
        {
            var choices = Services.GetRequiredService<UIConfig>().ThemeChoices!;
            var expected = Rune.Styles.ThemeStyles.ClassFor(choices.SystemLight, choices.SystemDark, Rune.Styles.ColorScheme.System, choices.Themes, choices.SystemContrast);
            Assert.Contains(expected, provider.Find("div").ClassList);
        }
    }

    [Fact]
    public void 全部内置主题跟随系统时_系统要求增强对比度用高对比的亮暗两套()
    {
        using var json = JsonDocument.Parse(ThemeChoices.Builtin.ToJson());
        var contrast = json.RootElement.GetProperty("system").GetProperty("contrast");

        Assert.Equal(("high-contrast", "high-contrast-dark"), (contrast.GetProperty("light").GetString(), contrast.GetProperty("dark").GetString()));
        Assert.True(json.RootElement.GetProperty("themes").TryGetProperty("high-contrast-dark", out _));
    }
}
