using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>认证页版式（同 vue-tsx-pro 的 AuthLayout）：有品牌区时分两栏，工具在表单区里，表单是 main。</summary>
public class AuthLayoutTests : BunitContext
{
    public AuthLayoutTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    [Fact]
    public void 有品牌区时分栏_工具在表单区的上角()
    {
        var cut = Render<AuthLayout>(p => p
            .Add(x => x.Hero, (RenderFragment)(b => b.AddMarkupContent(0, "<p>口号</p>")))
            .Add(x => x.Tools, (RenderFragment)(b => b.AddMarkupContent(0, "<button>主题</button>")))
            .AddChildContent("<form>登录</form>"));

        var root = cut.Find("""[data-scope="auth-layout"][data-part="root"]""");
        Assert.Equal(["hero", "main"], root.Children.Select(c => c.GetAttribute("data-part")));
        var main = cut.Find("main");
        Assert.Equal("tools", main.Children[0].GetAttribute("data-part"));
        Assert.Equal("登录", main.QuerySelector("form")!.TextContent);

        var registry = Services.GetRequiredService<Rune.Styles.StyleRegistry>();
        Assert.Contains(registry.Rules, r => r.Contains("grid-template-columns:minmax(0, 1.15fr) minmax(0, 1fr)"));
    }

    [Fact]
    public void 没有品牌区时表单居中_不分栏()
    {
        var cut = Render<AuthLayout>(p => p.AddChildContent("<form>登录</form>"));

        Assert.Equal(["main"], cut.Find("""[data-scope="auth-layout"]""").Children.Select(c => c.GetAttribute("data-part")));
        Assert.Empty(cut.FindAll("""[data-part="tools"]"""));
    }
}
