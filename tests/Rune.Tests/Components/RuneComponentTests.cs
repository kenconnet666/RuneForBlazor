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
    public void 父级重新渲染时_内容片段可能读了普通字段_子组件照常重新渲染()
    {
        var host = Render<FragmentHost>();
        var children = host.FindComponents<FragmentChild>();

        host.Find("#plain").Click();

        // 内容片段的输出取决于父级渲染时的状态（普通字段变了不会通知），不能按委托相等跳过
        Assert.Equal(2, host.Instance.Renders);
        Assert.All(children, child => Assert.Equal(2, child.RenderCount));
        Assert.Contains("1", host.Find("#plain").TextContent);
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
}
