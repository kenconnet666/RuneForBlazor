using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Tests.UI;

/// <summary>Element 与 FocusAsync（同 Blazor 的 InputBase.Element）：聚焦的是控件的主元素；包了一层的组件转给里面；静态渲染时什么也不做。</summary>
public class FocusTests : BunitContext
{
    public FocusTests()
    {
        Services.AddRuneUI();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    void AssertFocused(AngleSharp.Dom.IElement element) =>
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(element);

    [Fact]
    public async Task 输入框聚焦里面的input_不是外壳()
    {
        var cut = Render<TextField>(p => p.AddUnmatched("aria-label", "名字"));
        await cut.InvokeAsync(() => cut.Instance.FocusAsync());
        AssertFocused(cut.Find("input"));
    }

    [Fact]
    public async Task 选择框聚焦触发按钮_按钮聚焦自己()
    {
        var select = Render<Select<string>>(p => p.Add(x => x.Items, ["a", "b"]).AddUnmatched("aria-label", "字母"));
        await select.InvokeAsync(() => select.Instance.FocusAsync());
        AssertFocused(select.Find("button[role=combobox]"));

        var button = Render<Button>(p => p.AddChildContent("保存"));
        await button.InvokeAsync(() => button.Instance.FocusAsync());
        Assert.Equal(2, JSInterop.Invocations.Count(i => i.Identifier == "Blazor._internal.domWrapper.focus"));
        JSInterop.Invocations.Last().Arguments[0].ShouldBeElementReferenceTo(button.Find("button"));
    }

    [Fact]
    public async Task 范围滑块聚焦起点_区间数字框聚焦第一个框_提及聚焦文本框()
    {
        var slider = Render<Slider<int>>(p => p.Add(x => x.Range, new NumberRange<int>(10, 80)).AddUnmatched("aria-label", "价格"));
        await slider.InvokeAsync(() => slider.Instance.FocusAsync());
        JSInterop.Invocations.Last().Arguments[0].ShouldBeElementReferenceTo(slider.FindAll("[role=slider]")[0]);

        var range = Render<NumberRangeField<int>>(p => p.AddUnmatched("aria-label", "金额"));
        await range.InvokeAsync(() => range.Instance.FocusAsync());
        JSInterop.Invocations.Last().Arguments[0].ShouldBeElementReferenceTo(range.FindAll("input")[0]);

        var mentions = Render<Mentions<string>>(p => p.Add(x => x.Items, ["周宁"]));
        await mentions.InvokeAsync(() => mentions.Instance.FocusAsync());
        JSInterop.Invocations.Last().Arguments[0].ShouldBeElementReferenceTo(mentions.Find("textarea"));
    }

    [Fact]
    public async Task 静态渲染时什么也不做()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = Render<TextField>(p => p.AddUnmatched("aria-label", "名字"));
        await cut.InvokeAsync(() => cut.Instance.FocusAsync());
        Assert.Empty(JSInterop.Invocations);
    }
}
