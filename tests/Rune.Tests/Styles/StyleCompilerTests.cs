using Rune.Styles;

namespace Rune.Tests.Styles;

public class StyleCompilerTests
{
    static CompiledStyle Compile(params Style[] styles) => StyleCompiler.Compile(styles)!;

    [Fact]
    public void 起始样式包在外面_排在同一选择器的普通规则后面()
    {
        var compiled = Compile(new Style
        {
            Opacity = 0,
            [When.PopoverOpen] = new() { Opacity = 1, [When.Starting] = new() { Opacity = 0 } },
            [Pseudo.Backdrop] = new() { Opacity = 0.5 },
        });
        var c = compiled.ClassName;

        var open = compiled.Css.IndexOf($".{c}:popover-open{{opacity:1;}}", StringComparison.Ordinal);
        var starting = compiled.Css.IndexOf($"@starting-style{{.{c}:popover-open{{opacity:0;}}}}", StringComparison.Ordinal);
        Assert.True(open >= 0 && starting > open);
        Assert.Contains($".{c}::backdrop{{opacity:0.5;}}", compiled.Css);
    }

    [Fact]
    public void 父元素的状态写成is选择器()
    {
        var compiled = Compile(new Style { [When.Parent(When.Hover)] = new() { Opacity = 1 } });

        Assert.Contains($".{compiled.ClassName}:is(:hover > *){{opacity:1;}}", compiled.Css);
        Assert.Throws<ArgumentException>(() => When.Parent(Media.Up(768)));
    }

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
    public void 容器查询_supports_回退值_带标记的祖先状态()
    {
        var marker = Css.Marker("rune-test-option");
        var compiled = Compile(new Style
        {
            Height = Css.Fallback("100vh", "100dvh"),
            [Containers.MinWidth(CssLength.Em(30))] = new() { Display = Display.Grid },
            [Media.Supports("(field-sizing: content)")] = new() { Display = Display.Block },
            [When.Inside(When.Selected, marker)] = new() { Visibility = Visibility.Visible },
        });

        Assert.Contains("height:100vh;height:100dvh;", compiled.Css);
        Assert.Contains($"@container (min-width: 30em){{.{compiled.ClassName}{{display:grid;}}}}", compiled.Css);
        Assert.Contains($"@supports (field-sizing: content){{.{compiled.ClassName}{{display:block;}}}}", compiled.Css);
        Assert.Contains($".{compiled.ClassName}:is(.rune-test-option:is([aria-selected=\"true\"],[data-selected]) *){{visibility:visible;}}", compiled.Css);
        // @supports 先写，@container 后写（后写的覆盖先写的）
        Assert.True(compiled.Css.IndexOf("@supports", StringComparison.Ordinal) < compiled.Css.IndexOf("@container", StringComparison.Ordinal));
        Assert.Equal("rune-test-option", marker.Attrs["class"]);
        Assert.Throws<ArgumentException>(() => Css.Marker("Not A Class"));
    }

    [Fact]
    public void 带单位的写法_颜色关键字_只收文字的属性()
    {
        var css = Compile(new Style
        {
            Width = CssLength.Percent(100),
            MinHeight = CssLength.Em(1.5),
            MaxWidth = CssLength.Rem(20),
            Margin = CssLength.Em(0),
            Inset = CssLength.Auto,
            Rotate = CssValue.Deg(180),
            BackgroundColor = CssColors.Transparent,
            BorderColor = CssColors.CurrentColor,
            Display = Display.Flex,
            Cursor = CssWide.Inherit,
        }).Css;

        Assert.Contains("width:100%;", css);
        Assert.Contains("min-height:1.5em;", css);
        Assert.Contains("max-width:20rem;", css);
        Assert.Contains("margin:0;", css);
        Assert.Contains("inset:auto;", css);
        Assert.Contains("rotate:180deg;", css);
        Assert.Contains("background-color:transparent;", css);
        Assert.Contains("border-color:currentcolor;", css);
        Assert.Contains("display:flex;", css);
        Assert.Contains("cursor:inherit;", css);
        // 值里没有数字的属性不接受数字（Display = 5 编译不过）
        Assert.Equal(typeof(CssKeyword?), typeof(Style).GetProperty(nameof(Style.Display))!.PropertyType);
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
