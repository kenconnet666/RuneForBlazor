using Rune.Styles;

namespace Rune.Tests.Styles;

public class StyleCompilerTests
{
    static CompiledStyle Compile(params Style[] styles) => StyleCompiler.Compile(styles)!;

    [Fact]
    public void 数字按属性补单位()
    {
        var css = Compile(new Style { Padding = 8, LineHeight = 1.5, TransitionDuration = 200, Opacity = 0 }).Css;

        Assert.Contains("padding:8px;", css);
        Assert.Contains("line-height:1.5;", css);
        Assert.Contains("transition-duration:200ms;", css);
        Assert.Contains("opacity:0;", css);
    }

    [Fact]
    public void 后写的覆盖先写的同名属性_其他条件块保留()
    {
        var first = new Style { Color = "red", [When.Hover] = new() { Color = "blue" } };
        var second = new Style { Color = "green" };

        var compiled = Compile(first, second);

        Assert.DoesNotContain("red", compiled.Css);
        Assert.Contains($".{compiled.ClassName}{{color:green;}}", compiled.Css);
        Assert.Contains($".{compiled.ClassName}:hover{{color:blue;}}", compiled.Css);
    }

    [Fact]
    public void 基础块写在媒体查询之前_简写写在长属性之前()
    {
        var css = Compile(new Style
        {
            PaddingTop = 4,
            Padding = 8,
            [Media.Up(768)] = new() { Padding = 16 },
        }).Css;

        Assert.True(css.IndexOf("padding:8px", StringComparison.Ordinal) < css.IndexOf("padding-top:4px", StringComparison.Ordinal));
        Assert.True(css.IndexOf("padding-top:4px", StringComparison.Ordinal) < css.IndexOf("@media", StringComparison.Ordinal));
    }

    [Fact]
    public void 嵌套条件里的媒体查询包在外面()
    {
        var compiled = Compile(new Style { [When.Hover] = new() { [Media.Up(768)] = new() { Color = "blue" } } });

        Assert.Equal($"@media (min-width: 768px){{.{compiled.ClassName}:hover{{color:blue;}}}}", compiled.Css);
    }

    [Fact]
    public void 同一组实例返回同一个结果_内容相同的不同实例类名相同()
    {
        var style = new Style { Color = "teal" };

        Assert.Same(Compile(style), Compile(style));
        Assert.Equal(Compile(style).ClassName, Compile(new Style { Color = "teal" }).ClassName);
    }

    [Fact]
    public void 状态条件跟着aria与data属性()
    {
        var compiled = Compile(new Style { [When.Expanded] = new() { FontWeight = 600 } });

        Assert.Contains(""":is([aria-expanded="true"],[data-expanded])""", compiled.Css);
    }
}
