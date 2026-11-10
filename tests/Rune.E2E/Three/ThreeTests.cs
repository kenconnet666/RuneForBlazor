namespace Rune.E2E;

/// <summary>
/// 3D 效果（Rune.Three）：画布进入视口后开始渲染，画质切到 Off 时换成降级内容、切回来重建；参数变了不往返地交给场景；
/// 拖动地球、加载模型、放彩纸；换效果时释放上下文。三种渲染模式下 JS 一侧是同一套（data-rune-three）。
/// CI 上没有显卡，WebGL 是软件渲染、很吃 CPU：这一组不和别的测试同时跑（同一分片里别的测试会被拖到超时）。
/// </summary>
[Collection(nameof(ThreeTests))]
public class ThreeTests(DemoServer demo) : E2ETest(demo)
{
    // 画布的渲染状态（JS 写在根元素的 data-state 上）
    static ILocator Canvas(ILocator scope) => scope.Locator("[data-rune-three]");

    [Theory]
    [InlineData("static")]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 画布_进入视口后渲染_静态SSR也一样(string mode)
    {
        var page = await OpenAsync(mode, "three-canvas");
        var basic = page.Locator("#basic-preview");
        await basic.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(Canvas(basic)).ToHaveAttributeAsync("data-state", "running");
        await Assertions.Expect(Canvas(basic).Locator("> canvas")).ToHaveAttributeAsync("data-part", "canvas");
        // 还没滚到的画布不创建渲染器
        await Assertions.Expect(Canvas(page.Locator("#pointer-preview"))).ToHaveAttributeAsync("data-state", "idle");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 画质切到Off显示降级内容_切回来重建_暂停与状态报告(string mode)
    {
        var page = await OpenAsync(mode, "three-canvas");
        var demo = page.Locator("#quality-preview");
        await demo.ScrollIntoViewIfNeededAsync();
        var canvas = Canvas(demo);
        await Assertions.Expect(canvas).ToHaveAttributeAsync("data-state", "running");
        // 场景经 rune-command 报告的粒子数；OnStatusChange 报告的状态
        await Assertions.Expect(demo.Locator("small").Filter(new() { HasText = "个粒子" })).ToBeVisibleAsync();
        await Assertions.Expect(demo.Locator("[data-scope=badge]")).ToHaveTextAsync("running");

        await demo.GetByRole(AriaRole.Button, new() { Name = "关" }).ClickAsync();
        await Assertions.Expect(canvas).ToHaveAttributeAsync("data-state", "fallback");
        await Assertions.Expect(canvas.Locator("> canvas")).ToHaveCountAsync(0);
        await Assertions.Expect(demo.GetByText("特效已关闭", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(demo.Locator("[data-scope=badge]")).ToHaveTextAsync("fallback");

        await demo.GetByRole(AriaRole.Button, new() { Name = "低" }).ClickAsync();
        await Assertions.Expect(canvas).ToHaveAttributeAsync("data-state", "running");
        await Assertions.Expect(demo.GetByText("特效已关闭", new() { Exact = true })).ToBeHiddenAsync();
        await Assertions.Expect(demo.Locator("small").Filter(new() { HasText = "low · 4800 个粒子" })).ToBeVisibleAsync();

        await demo.GetByRole(AriaRole.Switch, new() { Name = "暂停" }).ClickAsync();
        await Assertions.Expect(canvas).ToHaveAttributeAsync("data-state", "paused");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 换效果时释放上下文_换色板重建场景(string mode)
    {
        var page = await OpenAsync(mode, "shader-background");
        var performance = page.Locator("#performance-preview");
        await performance.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(Canvas(performance)).ToHaveAttributeAsync("data-state", "running");
        var before = await ActiveContextsAsync(page);
        foreach (var name in new[] { "极光", "粒子场", "线框地形", "网格渐变" })
        {
            await performance.GetByRole(AriaRole.Button, new() { Name = name }).ClickAsync();
            await Assertions.Expect(Canvas(performance)).ToHaveAttributeAsync("data-state", "running");
        }
        // 换掉的效果交还了上下文：数量没有越换越多
        Assert.Equal(before, await ActiveContextsAsync(page));

        var controls = page.Locator("#mesh-gradient-controls-preview");
        await controls.ScrollIntoViewIfNeededAsync();
        await controls.GetByRole(AriaRole.Button, new() { Name = "日落" }).ClickAsync();
        await Assertions.Expect(Canvas(controls)).ToHaveAttributeAsync("data-options", new System.Text.RegularExpressions.Regex("series3"));
        await Assertions.Expect(Canvas(controls)).ToHaveAttributeAsync("data-state", "running");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 地球_拖动时是抓住的光标_松手后恢复(string mode)
    {
        var page = await OpenAsync(mode, "globe");
        var host = page.Locator("#network-preview [data-rune-three-host]");
        await host.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(Canvas(host)).ToHaveAttributeAsync("data-state", "running");
        var box = (await host.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width / 2 + 60, box.Y + box.Height / 2, new() { Steps = 5 });
        await Assertions.Expect(host).ToHaveAttributeAsync("data-dragging", "");
        Assert.Equal("grabbing", await host.EvaluateAsync<string>("e => getComputedStyle(e).cursor"));
        await page.Mouse.UpAsync();
        await Assertions.Expect(host).Not.ToHaveAttributeAsync("data-dragging", "");
        Assert.Equal("grab", await host.EvaluateAsync<string>("e => getComputedStyle(e).cursor"));
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 模型_加载完调用OnLoad_方向键能转(string mode)
    {
        var page = await OpenAsync(mode, "model-viewer");
        var product = page.Locator("#product-preview");
        await product.ScrollIntoViewIfNeededAsync();
        var viewer = product.Locator("[data-rune-three-host]");
        await Assertions.Expect(viewer).ToHaveAttributeAsync("data-status", "loaded", new() { Timeout = 20_000 });
        await Assertions.Expect(viewer).Not.ToHaveAttributeAsync("aria-busy", "true");
        await Assertions.Expect(product.GetByText("模型已加载", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(product.GetByRole(AriaRole.Button, new() { Name = "加入购物车" })).ToBeEnabledAsync();
        await Assertions.Expect(viewer).ToHaveAttributeAsync("role", "img");
        await viewer.FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 模型_地址错了显示提示_调用OnError(string mode)
    {
        // 这页的模型（奖杯、程序生成的透光与清漆材质）在 CI 的软件渲染下一直在转、占满主线程，出错的那个等不到回调：
        // 都不让加载（也走出错），页面上没有要画的模型，只看地址错了的那个
        var page = await OpenAsync(mode, "model-viewer", async context =>
        {
            await context.RouteAsync("**/trophy.glb", route => route.AbortAsync());
            await context.RouteAsync("**/ObjectsExample.razor.js", route => route.AbortAsync());
        });
        var objects = page.Locator("#objects-preview");
        await objects.ScrollIntoViewIfNeededAsync();
        var viewers = objects.Locator("[data-rune-three-host]");
        await Assertions.Expect(viewers.Nth(2)).ToHaveAttributeAsync("data-status", "error", new() { Timeout = 20_000 });
        await Assertions.Expect(viewers.Nth(2).GetByText("模型加载失败", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(objects.GetByText("地址错了（OnError 拿到了原因）", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Theory]
    [InlineData("server")]
    [InlineData("wasm")]
    public async Task 彩纸_放的时候才有画布_落完收起调用OnDone_关掉特效时直接收尾(string mode)
    {
        var page = await OpenAsync(mode, "confetti");
        var demo = page.Locator("#palette-preview");
        await demo.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(Canvas(demo)).ToHaveCountAsync(0);
        await demo.GetByRole(AriaRole.Button, new() { Name = "放一次" }).ClickAsync();
        await Assertions.Expect(Canvas(demo)).ToHaveAttributeAsync("data-state", "running");
        await Assertions.Expect(demo.GetByText("已落完 1 次", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(Canvas(demo)).ToHaveCountAsync(0);

        await demo.GetByRole(AriaRole.Switch, new() { Name = "动态特效" }).ClickAsync();
        await demo.GetByRole(AriaRole.Button, new() { Name = "放一次" }).ClickAsync();
        await Assertions.Expect(demo.GetByText("已落完 2 次", new() { Exact = true })).ToBeVisibleAsync();

        // 铺满视口的放在顶层（原生 popover）
        var celebrate = page.Locator("#celebrate-preview");
        await celebrate.ScrollIntoViewIfNeededAsync();
        await celebrate.GetByRole(AriaRole.Button, new() { Name = "完成任务" }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-rune-three-layer]:popover-open")).ToHaveCountAsync(1);
    }

    static Task<int> ActiveContextsAsync(IPage page) =>
        page.EvaluateAsync<int>("async () => (await import(new URL('_content/RuneForBlazor.Three/js/index.js', document.baseURI).href)).activeThreeContexts()");
}

/// <summary>3D 的端到端测试单独一组、不与别的测试并行。</summary>
[CollectionDefinition(nameof(ThreeTests), DisableParallelization = true)]
public sealed class ThreeTestsCollection;
