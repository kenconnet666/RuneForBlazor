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
    public void 值是null的属性不表态_留着前面几层的()
    {
        // 使用方写了 title="@error"、aria-label="@label"，值是 null：组件收到的属性表里有这个键、值是 null
        var user = Attrs.From(new Dictionary<string, object> { ["title"] = null!, ["aria-label"] = null!, ["onclick"] = null! }, sx: null);
        var merged = ToDictionary(Attrs.Merge(
            new() { Title = "默认", AriaLabel = "关闭", OnClick = _ => Task.CompletedTask },
            user,
            new() { Role = "button" }));

        Assert.Equal(("默认", "关闭", "button"), (merged["title"], merged["aria-label"], merged["role"]));
        Assert.IsType<Func<MouseEventArgs, Task>>(merged["onclick"]);
    }

    [Fact]
    public void aria_describedby各层的id拼在一起_去掉重复的()
    {
        // Field 的说明、错误（受管层）与使用方自己写的提示都念得到
        var merged = ToDictionary(Attrs.Merge(
            new() { AriaDescribedBy = "hint" },
            new() { AriaDescribedBy = "mine  hint" },
            new() { AriaDescribedBy = "description error mine" }));
        Assert.Equal("hint mine description error", merged["aria-describedby"]);

        // 只有一层写了：原样
        Assert.Equal("a  b", ToDictionary(Attrs.Merge(null, new() { AriaDescribedBy = "a  b" }))["aria-describedby"]);
    }

    [Fact]
    public void 嵌套的部件属性按同样的规则再合并_不整个替换()
    {
        var flip = new Style { Color = "red" };
        var merged = Attrs.MergeProps(
            new() { ["PropsIcon"] = new Attrs { Sx = flip, ["FlipInRtl"] = true, Title = "默认" } },
            new() { ["PropsIcon"] = new Attrs { Title = "使用方", ["Size"] = 20 } });

        var icon = Assert.IsType<Attrs>(merged["PropsIcon"]);
        Assert.Equal((true, "使用方", 20), (icon["FlipInRtl"], icon["title"], icon["Size"]));
        Assert.Equal([flip], icon.Sx!);

        // 不是部件属性的同名对象照旧后面的层盖过前面的
        var other = new Attrs { Title = "后" };
        Assert.Same(other, Attrs.MergeProps(new() { ["Options"] = new Attrs { Title = "前" } }, new() { ["Options"] = other })["Options"]);
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
    public void 两层各有样式时按层分段_每段一个类_后面的层盖过前面的()
    {
        var merged = ToDictionary(Attrs.Merge(
            new() { Sx = new Style { Color = "red" }, Class = "host" },
            new() { Sx = new Style { Color = "blue" } }));

        var classes = ((string)merged["class"]).Split(' ');
        Assert.Equal("host", classes[0]);
        Assert.Equal(
            [$"@layer rune.s0{{.{classes[1]}{{color:red;}}}}", $"@layer rune.s1{{.{classes[2]}{{color:blue;}}}}"],
            registry.Rules);
    }

    [Fact]
    public void 只有默认层样式时每次返回同一个结果()
    {
        var style = new Style { Color = "red" };

        Assert.Same(Attrs.Merge(style), Attrs.Merge(style));
    }

    [Fact]
    public void 同样的几层再合并时返回同一个结果_换了作用域照样登记规则()
    {
        var defaults = new Attrs { Sx = new Style { Color = "red" }, ["data-part"] = "root" };
        var managed = new Attrs { AriaLabel = "关闭" };

        var first = Attrs.Merge(defaults, null, managed);
        Assert.Same(first, Attrs.Merge(defaults, null, managed));
        Assert.NotSame(first, Attrs.Merge(defaults, null, new Attrs { AriaLabel = "关闭" }));

        // 另一个请求、电路：结果取缓存，规则仍要登记进它的作用域
        var other = new StyleRegistry();
        using (StyleRegistry.Enter(other)) Assert.Same(first, Attrs.Merge(defaults, null, managed));
        Assert.Single(other.Rules);
    }

    [Fact]
    public void 交给子组件的几层没变时是同一个对象_子组件按引用比较Sx就会跳过渲染()
    {
        var part = new Attrs { Sx = new Style { Color = "red" } };
        var user = new Attrs { Sx = new Style { Width = CssLength.Percent(100) }, Id = "icon" };

        var props = Attrs.MergeProps(part, user);
        Assert.Same(props, Attrs.MergeProps(part, user));
        Assert.Same(Attrs.Layer(part, user), Attrs.Layer(part, user));
        Assert.NotSame(props, Attrs.Layer(part, user));
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
