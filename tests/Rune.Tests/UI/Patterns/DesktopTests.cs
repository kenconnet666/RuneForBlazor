using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;
using Rune.UI;

namespace Rune.Tests.UI;

// 桌面模式：注册了 IDesktopWindow 时 AppShell 的顶栏就是窗口的标题栏，结束边是窗口按钮；没注册时一切照旧
public class DesktopTests : BunitContext
{
    sealed class FakeWindow : IDesktopWindow
    {
        public DesktopWindowState State { get; set; } = new(Maximized: false, Active: true, FullScreen: false);
        public event Action? StateChanged;
        public List<string> Calls { get; } = [];

        public Task MinimizeAsync() => Record("minimize");
        public Task ToggleMaximizeAsync() => Record("maximize");
        public Task CloseAsync() => Record("close");

        public void Change(DesktopWindowState state)
        {
            State = state;
            StateChanged?.Invoke();
        }

        Task Record(string call)
        {
            Calls.Add(call);
            return Task.CompletedTask;
        }
    }

    readonly FakeWindow window = new();

    public DesktopTests()
    {
        Services.AddRuneUI().AddSingleton<IDesktopWindow>(window);
        SetRendererInfo(new RendererInfo("WebView", isInteractive: true));
    }

    static readonly RenderFragment Brand = builder =>
    {
        builder.OpenComponent<AppShellBrand>(0);
        builder.AddAttribute(1, nameof(AppShellBrand.Href), "");
        builder.AddAttribute(2, nameof(AppShellBrand.ChildContent), (RenderFragment)(t => t.AddContent(0, "采购")));
        builder.CloseComponent();
    };

    [Fact]
    public void 没有窗口服务时_窗口按钮不渲染_外壳照旧()
    {
        using var browser = new BunitContext();
        browser.Services.AddRuneUI();
        browser.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        Assert.Empty(browser.Render<WindowControls>().Nodes);

        var shell = browser.Render<AppShell>(p => p.Add(x => x.Brand, Brand));
        Assert.Null(shell.Find("div").GetAttribute("data-desktop"));
        Assert.Empty(shell.FindAll("[data-window-command]"));
    }

    [Fact]
    public void 窗口按钮_点了交给窗口_最大化后是还原_失去焦点变淡()
    {
        var cut = Render<WindowControls>();

        Assert.Equal("窗口", cut.Find("[role=group]").GetAttribute("aria-label"));
        Assert.Equal(["最小化", "最大化", "关闭"], cut.FindAll("button").Select(b => b.GetAttribute("aria-label")));
        foreach (var command in new[] { "minimize", "maximize", "close" }) cut.Find($"[data-window-command={command}]").Click();
        Assert.Equal(["minimize", "maximize", "close"], window.Calls);

        cut.InvokeAsync(() => window.Change(new(Maximized: true, Active: false, FullScreen: false)));
        cut.WaitForAssertion(() => Assert.Equal("还原", cut.Find("[data-window-command=maximize]").GetAttribute("aria-label")));
        Assert.Equal("false", cut.Find("[role=group]").GetAttribute("data-active"));
    }

    [Fact]
    public void 英文语言包的窗口按钮名称()
    {
        Assert.Equal("Minimize", Lang.EnUs.Desktop.Minimize);
        Assert.Equal("最小化", (Lang.ZhCn with { }).Desktop.Minimize);
        Assert.Equal("关掉", (Lang.ZhCn with { Desktop = Lang.ZhCn.Desktop with { Close = "关掉" } }).Desktop.Close);
    }

    [Fact]
    public void 桌面模式_顶栏是拖动区域_里面能点的不可拖_结束边是窗口按钮_失去焦点时外壳标记出来()
    {
        var shell = Render<AppShell>(p => p.Add(x => x.Brand, Brand).Add(x => x.Header, (RenderFragment)(b => b.AddMarkupContent(0, "<button>搜索</button>"))));

        var root = shell.Find("div");
        Assert.True(root.HasAttribute("data-desktop"));
        Assert.Null(root.GetAttribute("data-window-active"));
        // 窗口按钮是顶栏的最后一项
        Assert.Equal("group", shell.Find("header").LastElementChild!.GetAttribute("role"));

        var css = Render<RuneStyles>().Find("style[data-rune]").TextContent;
        Assert.Contains("app-region:drag", css);
        Assert.Contains(" :is(a, button, input, select, textarea, label, summary, [tabindex], [role], [contenteditable], [popover], dialog){app-region:no-drag", css);

        shell.InvokeAsync(() => window.Change(new(Maximized: false, Active: false, FullScreen: false)));
        shell.WaitForAssertion(() => Assert.Equal("false", shell.Find("div").GetAttribute("data-window-active")));
    }
}
