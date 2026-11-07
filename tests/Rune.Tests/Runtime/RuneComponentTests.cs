using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Tests.TestComponents;
using Rune.UI;

namespace Rune.Tests.Runtime;

public class RuneComponentTests : BunitContext
{
    readonly TestStore store = new();

    public RuneComponentTests()
    {
        Services.AddRuneUI();
        Services.AddSingleton(store);
    }

    [Fact]
    public async Task 状态变化只重新渲染读了它的组件()
    {
        var host = Render<ReaderHost>();
        var readers = host.FindComponents<StoreReader>();

        await host.InvokeAsync(() => store.A++);

        Assert.Equal("1", readers[0].Find("span").TextContent);
        Assert.Equal(2, readers[0].RenderCount);
        Assert.Equal(1, readers[1].RenderCount);
        Assert.Equal(1, host.Instance.Renders);
    }

    [Fact]
    public void 父级重新渲染时_读了父级字段的内容片段是新实例_子组件照常重新渲染()
    {
        var host = Render<FragmentHost>();
        var children = host.FindComponents<FragmentChild>();

        host.Find("#plain").Click();

        // 内容片段的输出取决于父级渲染时的状态（普通字段变了不会通知）：每次渲染新建的片段一律当作变了
        Assert.Equal(2, host.Instance.Renders);
        Assert.Equal(2, children[1].RenderCount);
        Assert.Equal(2, children[2].RenderCount);
        Assert.Contains("普通字段 1", children[2].Markup);
    }

    [Fact]
    public void 同一个内容片段实例_没有变_子组件跳过()
    {
        var host = Render<FragmentHost>();
        var constant = host.FindComponents<FragmentChild>()[0];
        var cascaded = host.FindComponent<CascadeChild>();

        host.Find("#plain").Click();

        // 只有常量的片段由编译器缓存成同一个委托；级联值通知时 Blazor 把上次的参数（同一个实例）再交一遍
        Assert.Equal(1, constant.RenderCount);
        Assert.Equal(1, cascaded.RenderCount);
        Assert.Contains("固定：固定内容", cascaded.Markup);
    }

    [Fact]
    public void 父级重新渲染时_只有基本类型参数且没变_子组件跳过()
    {
        var host = Render<ReaderHost>();
        var readers = host.FindComponents<StoreReader>();

        host.Render();

        Assert.All(readers, reader => Assert.Equal(1, reader.RenderCount));
    }

    [Fact]
    public void 可变对象参数照常跟着父级重新渲染()
    {
        var host = Render<FragmentHost>();
        var list = host.FindComponent<ListChild>();

        host.Find("#plain").Click();

        Assert.Equal(2, list.RenderCount);
    }

    [Fact]
    public async Task 内容里读到的状态归子组件所有()
    {
        var host = Render<FragmentHost>();
        var child = host.FindComponents<FragmentChild>()[1];

        await host.InvokeAsync(() => host.Instance.Local.Value++);

        Assert.Contains("本地状态 1", child.Markup);
        Assert.Equal(2, child.RenderCount);
        Assert.Equal(1, host.Instance.Renders);
    }

    [Fact]
    public void 父级去掉了全部未声明的属性_元素上也去掉()
    {
        var cut = Render<TextSpan>(p => p.AddUnmatched("data-changed", "").AddChildContent("值"));
        Assert.True(cut.Find("span").HasAttribute("data-changed"));

        // Blazor 在没有未声明的属性时不写 AdditionalAttributes：不清空的话上一次的会留下来
        cut.Render(p => p.AddChildContent("值"));
        Assert.False(cut.Find("span").HasAttribute("data-changed"));
    }
}
