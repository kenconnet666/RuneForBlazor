using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Rune.Styles;

namespace Rune.Tests.Merging;

public class AttrsMergeTests : IDisposable
{
    readonly StyleRegistry registry = new();
    readonly IDisposable scope;

    // 合并样式要登记规则：测试里手动设置当前登记表（组件渲染时由 RuneComponent 设置）
    public AttrsMergeTests() => scope = StyleRegistry.Enter(registry);

    public void Dispose() => scope.Dispose();

    static Dictionary<string, object> ToDictionary(Attrs attrs) => attrs.ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public void 后面的层覆盖前面的_class依次拼接()
    {
        var merged = ToDictionary(Attrs.Merge(
            new() { Id = "default", Class = "a", Title = "默认" },
            new() { Id = "user", Class = "b" },
            new() { Id = "managed" }));

        Assert.Equal("managed", merged["id"]);
        Assert.Equal("a b", merged["class"]);
        Assert.Equal("默认", merged["title"]);
    }

    [Fact]
    public void Layer把几层合成一层_样式留在Sx上_再合并到元素上时照常生效()
    {
        var red = new Style { Color = "red" };
        var wide = new Style { Width = CssLength.Percent(100) };
        var layer = Attrs.Layer(new() { Sx = red, Title = "默认" }, new() { Sx = wide, Id = "user" });

        Assert.Equal([red, wide], layer.Sx!);
        Assert.DoesNotContain(ToDictionary(layer).Keys, key => key.Equals("Sx", StringComparison.OrdinalIgnoreCase));

        // 子组件把它当作使用方的一层合并到元素上：样式进了类，属性都在
        var merged = ToDictionary(Attrs.Merge(new() { Sx = new Style { Display = Display.Flex } }, layer));
        Assert.Equal(("user", "默认"), (merged["id"], merged["title"]));
        var rules = string.Concat(registry.Rules).Replace(" ", "");
        Assert.Contains("color:red", rules);
        Assert.Contains("width:100%", rules);
    }

    [Fact]
    public void aria的布尔值写成字符串()
    {
        var merged = ToDictionary(Attrs.Merge(new() { AriaExpanded = false, Hidden = true }));

        Assert.Equal("false", merged["aria-expanded"]);
        Assert.Equal(true, merged["hidden"]);
    }

    [Fact]
    public void 样式合成一个类并登记规则()
    {
        var merged = ToDictionary(Attrs.Merge(
            new() { Sx = new Style { Color = "red" }, Class = "host" },
            new() { Sx = new Style { Color = "blue" } }));

        var classes = ((string)merged["class"]).Split(' ');
        Assert.Equal("host", classes[0]);
        var rule = Assert.Single(registry.Rules);
        Assert.Contains($".{classes[1]}{{color:blue;}}", rule);
    }

    [Fact]
    public void 只有默认层样式时每次返回同一个结果()
    {
        var style = new Style { Color = "red" };

        Assert.Same(Attrs.Merge(style), Attrs.Merge(style));
    }

    [Fact]
    public void 不在组件渲染里合并样式会报错()
    {
        using var outside = StyleRegistry.Enter(null!);

        Assert.Throws<InvalidOperationException>(() => Attrs.Merge(new Style { Color = "red" }));
    }

    [Fact]
    public async Task 两层的事件都执行_各自由所属的组件处理()
    {
        var calls = new List<string>();
        var inner = new FakeComponent("组件", calls);
        var outer = new FakeComponent("使用方", calls);

        var merged = ToDictionary(Attrs.Merge(new() { OnClick = inner.OnClick }, new() { OnClick = outer.OnClick }));
        await ((Func<MouseEventArgs, Task>)merged["onclick"])(new MouseEventArgs());

        Assert.Equal(["组件 处理", "组件 点击", "使用方 处理", "使用方 点击"], calls);
    }

    /// <summary>实现 IHandleEvent 的“组件”：记录事件是不是经它自己处理（处理后组件会重新渲染）。</summary>
    sealed class FakeComponent(string name, List<string> calls) : IHandleEvent
    {
        public Task OnClick(MouseEventArgs _)
        {
            calls.Add($"{name} 点击");
            return Task.CompletedTask;
        }

        public Task HandleEventAsync(EventCallbackWorkItem item, object? arg)
        {
            calls.Add($"{name} 处理");
            return item.InvokeAsync(arg);
        }
    }
}
