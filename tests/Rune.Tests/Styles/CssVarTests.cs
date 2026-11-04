using Rune.Styles;

namespace Rune.Tests.Styles;

public class CssVarTests : IDisposable
{
    readonly IDisposable scope = StyleRegistry.Enter(new StyleRegistry());

    public void Dispose() => scope.Dispose();

    static readonly CssVar<CssLength> fillWidth = CssVar.Length("fill-width");

    [Fact]
    public void 样式里引用变量_值写在元素的style上()
    {
        var compiled = StyleCompiler.Compile([new Style { Width = fillWidth }])!;
        var attrs = new Attrs { Vars = [fillWidth.Set(40)] }.ToDictionary(p => p.Key, p => p.Value);

        Assert.Contains("width:var(--fill-width);", compiled.Css);
        Assert.Equal("--fill-width:40px", attrs["style"]);
    }

    [Fact]
    public void 行内样式与变量写在一起时都保留()
    {
        var attrs = new Attrs { InlineStyle = "opacity:0.5", Vars = [fillWidth.Set("50%")] }.ToDictionary(p => p.Key, p => p.Value);

        Assert.Equal("opacity:0.5;--fill-width:50%", attrs["style"]);
    }

    [Fact]
    public void 关键字常量与属性类型一致()
    {
        var css = StyleCompiler.Compile([new Style { Display = Display.Flex, Position = Position.Sticky, Width = CssWide.Inherit }])!.Css;

        Assert.Contains("display:flex;", css);
        Assert.Contains("position:sticky;", css);
        Assert.Contains("width:inherit;", css);
    }
}
