using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

// 格子的显示在 JS 里跟着输入框走，这里测 C# 的一半：过滤、填满时回调、初次渲染的格子与 input 的属性
public class OtpFieldTests : BunitContext
{
    public OtpFieldTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    static string Slots(IRenderedComponent<OtpField> cut) =>
        string.Concat(cut.FindAll("[data-index]").Select(s => s.GetAttribute("data-char") is { Length: > 0 } c ? c : "_"));

    [Fact]
    public void 只留数字_截到位数_填满时调用OnComplete()
    {
        string? completed = null;
        var cut = Render<OtpField>(p => p.Add(x => x.Value, "").Add(x => x.Length, 4).Add(x => x.OnComplete, v => completed = v));
        var input = cut.Find("input");
        Assert.Null(input.GetAttribute("maxlength"));
        Assert.Equal(("numeric", @"\d{4}", "one-time-code"),
            (input.GetAttribute("inputmode"), input.GetAttribute("pattern"), input.GetAttribute("autocomplete")));

        cut.Find("input").Input("1a2");
        Assert.Equal("12__", Slots(cut));
        Assert.Null(completed);

        cut.Find("input").Input("12 3456");
        Assert.Equal("1234", Slots(cut));
        Assert.Equal("1234", completed);
    }

    [Fact]
    public void 字母数字转大写_分组之间有分隔_遮罩显示圆点()
    {
        var cut = Render<OtpField>(p => p.Add(x => x.Value, "").Add(x => x.Length, 6).Add(x => x.Charset, OtpCharset.Alphanumeric).Add(x => x.Groups, [3, 3]));
        cut.Find("input").Input("ab-1");
        Assert.Equal("AB1___", Slots(cut));
        Assert.Single(cut.FindAll("[data-rune-otp] > span"));
        Assert.Equal("text", cut.Find("input").GetAttribute("inputmode"));

        var mask = Render<OtpField>(p => p.Add(x => x.Value, "42").Add(x => x.Mask, true));
        Assert.Equal("••____", Slots(mask));
        Assert.Equal("password", mask.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void 在Field里_input拿Field的id与说明_出错时格子标红()
    {
        var cut = Render<Field>(p => p.Add(x => x.Label, "验证码").Add(x => x.Error, "已过期").AddChildContent<OtpField>(o => o.Add(x => x.Value, "12")));
        var input = cut.Find("input");
        Assert.Equal(cut.Find("label").GetAttribute("for"), input.Id);
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.True(cut.Find("[data-rune-otp]").HasAttribute("data-invalid"));
        Assert.All(cut.FindAll("[data-index]"), slot => Assert.Equal("true", slot.GetAttribute("aria-hidden")));
    }
}
