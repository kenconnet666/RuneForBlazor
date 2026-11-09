using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>手写签名：画布是 role="img"，读屏的状态；JS 写完一笔发来的值写回；只读时没有工具栏；随表单提交。</summary>
public class SignaturePadTests : BunitContext
{
    public SignaturePadTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 画布是img_未签名_写完一笔的值写回()
    {
        string? value = null;
        var cut = Render<SignaturePad>(p => p
            .Add(x => x.Name, "signature")
            .Add(x => x.ValueChanged, (string? v) => value = v)
            .AddUnmatched("aria-label", "签收人签字"));
        var canvas = cut.Find("canvas");
        Assert.Equal(("img", "签收人签字"), (canvas.GetAttribute("role"), canvas.GetAttribute("aria-label")));
        var status = cut.Find($"#{canvas.GetAttribute("aria-describedby")}");
        Assert.Equal("未签名", status.TextContent);
        var root = cut.Find("[data-rune-signature]");
        Assert.True(root.HasAttribute("data-writable"));
        Assert.Null(root.GetAttribute("aria-label"));

        root.TriggerEvent("onrunecommand", new CommandEventArgs { Name = "signature", Text = "data:image/png;base64,AAAA" });
        Assert.Equal("data:image/png;base64,AAAA", value);
        cut.Render(p => p.Add(x => x.Value, value));
        Assert.Equal("已签名", cut.Find($"#{canvas.GetAttribute("aria-describedby")}").TextContent);
        Assert.Equal(value, cut.Find("input[type=hidden][name=signature]").GetAttribute("value"));
        Assert.Equal(value, cut.Find("[data-rune-signature]").GetAttribute("data-value"));

        cut.Find("[data-rune-signature]").TriggerEvent("onrunecommand", new CommandEventArgs { Name = "signature", Text = "" });
        Assert.Null(value);
    }

    [Fact]
    public void 只读时没有工具栏_不能写()
    {
        var cut = Render<SignaturePad>(p => p.Add(x => x.Value, "data:image/png;base64,AAAA").Add(x => x.ReadOnly, true));
        Assert.False(cut.Find("[data-rune-signature]").HasAttribute("data-writable"));
        Assert.Empty(cut.FindAll("[data-signature-action]"));
    }
}
