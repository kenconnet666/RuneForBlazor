using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Rune.Tests.Components.TestComponents;
using Rune.UI;

namespace Rune.Tests.Components;

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
    public void 父级重新渲染时_只引用组件成员的内容不变_子组件跳过()
    {
        var host = Render<FragmentHost>();
        var children = host.FindComponents<FragmentChild>();

        host.Find("#plain").Click();

        Assert.Equal(2, host.Instance.Renders);
        Assert.All(children, child => Assert.Equal(1, child.RenderCount));
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
}
