using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Rune.Docs.Site;
using Rune.UI;

namespace Rune.Tests.Docs;

/// <summary>
/// 文档站排队渲染的内容（Queued）：预渲染时直接渲染、两头留标记；交互模式的第一页先是占位（预渲染的内容由 JS 放回去），
/// 按 JS 量出来的离视口的远近轮到时换成真正的内容；站内换页后没有占位。
/// </summary>
public class QueuedTests : BunitContext
{
    // 整套测试并行跑、机器忙的时候，让出主线程后的那一段可能排上一秒多
    static readonly TimeSpan Patient = TimeSpan.FromSeconds(10);

    readonly HoldsJs js = new();

    public QueuedTests()
    {
        Services.AddRuneUI();
        Services.AddScoped<RenderQueue>();
        // bUnit 自带的 JS 对“还没给结果”的调用直接抛异常，模拟不了“还在视口外、先不回答”：用自己的
        Services.AddSingleton<IJSRuntime>(js);
    }

    // 队列问 JS 不带来渲染，WaitForAssertion（只在渲染后重查）等不到：自己隔一会儿看一眼
    async Task AskedAsync(int count)
    {
        for (var waited = 0; js.Asked.Count < count && waited < Patient.TotalMilliseconds; waited += 20) await Task.Delay(20, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(count, js.Asked.Count);
    }

    IRenderedComponent<Queued> RenderQueued(string? hold = "basic") =>
        Render<Queued>(p => p.Add(x => x.Hold, hold).AddChildContent("<button>示例</button>"));

    IRenderedComponent<ContainerFragment> RenderSections(params string[] names) => Render(builder =>
    {
        foreach (var name in names)
        {
            builder.OpenComponent<Queued>(0);
            builder.SetKey(name);
            builder.AddAttribute(1, nameof(Queued.Hold), name);
            builder.AddAttribute(2, nameof(Queued.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, $"<p>{name}</p>")));
            builder.CloseComponent();
        }
    });

    [Fact]
    public void 预渲染时直接渲染_内容两头是注释标记()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        var cut = RenderQueued();

        Assert.Equal("<!--docs-hold:basic--><button>示例</button><!--/docs-hold:basic-->", cut.Markup.Replace("\n", "").Replace("\r", ""));
        Assert.False(Services.GetRequiredService<RenderQueue>().Busy);
        // 没写名字的不留标记
        Assert.DoesNotContain("docs-hold", RenderQueued(hold: null).Markup);
    }

    [Fact]
    public async Task 第一页交互接管时先是占位_JS说轮到了才换成内容_队列排完()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var queue = Services.GetRequiredService<RenderQueue>();
        var busy = new List<bool>();
        queue.BusyChanged += () => busy.Add(queue.Busy);

        var cut = RenderQueued();
        // 占位不占排版的格子（display: contents），内容还没渲染
        var slot = cut.Find("[data-docs-hold=basic]");
        Assert.Equal("display:contents", slot.GetAttribute("style"));
        // 问的是还没渲染的那几段，文档站以外不限于视口附近
        await AskedAsync(1);
        Assert.Equal(["basic"], js.Asked[0].Names);
        Assert.False(js.Asked[0].NearOnly);
        // JS 还没回答：一直是占位
        Assert.Empty(cut.FindAll("button"));
        Assert.True(queue.Busy);

        js.Asked[0].Answer.SetResult(["basic"]);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("button")), Patient);
        Assert.Empty(cut.FindAll("[data-docs-hold]"));
        cut.WaitForAssertion(() => Assert.False(queue.Busy), Patient);
        Assert.Equal([true, false], busy);
    }

    [Fact]
    public void 第一页没写名字的直接渲染_不进队列()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var cut = RenderQueued(hold: null);

        Assert.Single(cut.FindAll("button"));
        Assert.False(Services.GetRequiredService<RenderQueue>().Busy);
        Assert.Empty(js.Asked);
    }

    [Fact]
    public async Task 几段按离视口的远近轮_没轮到的留着占位()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var cut = RenderSections("first", "second");

        // 第一次问两段：眼前的是 second
        await AskedAsync(1);
        Assert.Equal(["first", "second"], js.Asked[0].Names);
        js.Asked[0].Answer.SetResult(["second"]);
        cut.WaitForAssertion(() => Assert.Equal(["second"], cut.FindAll("p").Select(p => p.TextContent)), Patient);

        // 再问剩下的 first：还在视口外，先不回答，它一直留着占位
        await AskedAsync(2);
        Assert.Equal(["first"], js.Asked[1].Names);
        Assert.NotNull(cut.Find("[data-docs-hold=first]"));

        // 滚到附近了
        js.Asked[1].Answer.SetResult(["first"]);
        cut.WaitForAssertion(() => Assert.Equal(["first", "second"], cut.FindAll("p").Select(p => p.TextContent)), Patient);
        Assert.Empty(cut.FindAll("[data-docs-hold]"));
    }

    [Fact]
    public async Task 等着的时候又来了新的占位_作废重问_新来的也轮得到()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var first = Render<Queued>(p => p.Add(x => x.Hold, "first").AddChildContent("<p>first</p>"));
        await AskedAsync(1);

        // API 表的数据后到：它的占位这时才出现
        var late = Render<Queued>(p => p.Add(x => x.Hold, "late").AddChildContent("<p>late</p>"));
        await AskedAsync(2);
        Assert.True(js.Asked[0].Answer.Task.IsCanceled);
        Assert.Equal(["first", "late"], js.Asked[1].Names);

        js.Asked[1].Answer.SetResult(["late"]);
        late.WaitForAssertion(() => Assert.Single(late.FindAll("p")), Patient);
        Assert.NotNull(first.Find("[data-docs-hold=first]"));
    }

    [Fact]
    public async Task 文档站只渲染视口附近的_这个设置原样交给JS()
    {
        Services.AddSingleton(new RenderQueueOptions(NearViewportOnly: true));
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        RenderQueued();
        await AskedAsync(1);
        Assert.True(js.Asked[0].NearOnly);
    }

    [Fact]
    public void JS用不了时按登记的顺序全部渲染()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        // 调用会抛异常（同电路断了、初始化器还没装上）
        js.Broken = true;

        var cut = RenderSections("first", "second");

        cut.WaitForAssertion(() => Assert.Equal(["first", "second"], cut.FindAll("p").Select(p => p.TextContent)), Patient);
        Assert.Empty(cut.FindAll("[data-docs-hold]"));
        cut.WaitForAssertion(() => Assert.False(Services.GetRequiredService<RenderQueue>().Busy), Patient);
    }

    [Fact]
    public async Task 还没轮到页面就换走了_队列不再等它()
    {
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var queue = Services.GetRequiredService<RenderQueue>();

        RenderQueued();
        await AskedAsync(1);
        Assert.True(queue.Busy);

        await DisposeComponentsAsync();
        Assert.False(queue.Busy);
    }

    /// <summary>只认 runeDocsNearHolds 的 JS：记下每次问的是哪几段，由测试决定什么时候回答、回答什么。</summary>
    sealed class HoldsJs : IJSRuntime
    {
        public List<(string[] Names, bool NearOnly, TaskCompletionSource<string[]> Answer)> Asked { get; } = [];

        public bool Broken { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (Broken || identifier != "runeDocsNearHolds") throw new JSException($"没有 {identifier}");
            var answer = new TaskCompletionSource<string[]>();
            // 同真的 JS 运行时：调用方作废了，这次调用就算取消
            cancellationToken.Register(() => answer.TrySetCanceled(cancellationToken));
            Asked.Add(((string[])args![0]!, (bool)args[1]!, answer));
            return new ValueTask<TValue>((Task<TValue>)(object)answer.Task);
        }
    }
}
