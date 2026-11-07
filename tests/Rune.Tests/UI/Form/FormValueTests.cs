using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

public class FormValueTests : BunitContext
{
    public FormValueTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 值按规范文本提交_数字是十进制_日期是ISO()
    {
        Assert.Equal("19.9", FormValue.Text(19.90m));
        Assert.Equal("0.00001", FormValue.Text(0.00001));
        Assert.Equal("1234567", FormValue.Text(1234567L));
        Assert.Equal("2026-11-05", FormValue.Text(new DateOnly(2026, 11, 5)));
        Assert.Equal("true", FormValue.Text(true));
    }

    [Fact]
    public void 列表每个值一个input_没有名字或不可用时不渲染()
    {
        var cut = Render<FormValue>(p => p.Add(x => x.Name, "tags").Add(x => x.Value, new[] { "a", "b" }));
        Assert.Equal(["a", "b"], cut.FindAll("input[type=hidden][name=tags]").Select(i => i.GetAttribute("value")));

        Assert.Empty(Render<FormValue>(p => p.Add(x => x.Value, "a")).FindAll("input"));
        Assert.Empty(Render<FormValue>(p => p.Add(x => x.Name, "a").Add(x => x.Value, "a").Add(x => x.Disabled, true)).FindAll("input"));
        Assert.Empty(Render<FormValue>(p => p.Add(x => x.Name, "a").Add(x => x.Value, null)).FindAll("input"));
    }
}
