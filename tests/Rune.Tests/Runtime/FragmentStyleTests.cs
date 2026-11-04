using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.Tests.TestComponents;

namespace Rune.Tests.Runtime;

public class FragmentStyleTests : BunitContext
{
    public FragmentStyleTests() => Services.AddRune();

    [Fact]
    public void 非Rune组件执行的内容片段里也能用样式()
    {
        var cut = Render<PlainHost>();

        var span = cut.Find("span");
        Assert.False(string.IsNullOrEmpty(span.ClassName));
        Assert.Contains(Services.GetRequiredService<StyleRegistry>().Rules, rule => rule.Contains("color:teal"));
    }
}
