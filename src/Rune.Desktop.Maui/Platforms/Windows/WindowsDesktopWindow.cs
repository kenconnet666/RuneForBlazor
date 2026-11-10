using System.Runtime.InteropServices;
using Microsoft.Maui.Storage;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Rune.UI;
using Thickness = Microsoft.UI.Xaml.Thickness;
using Visibility = Microsoft.UI.Xaml.Visibility;
using WebView2 = Microsoft.UI.Xaml.Controls.WebView2;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Rune.Desktop.Windows;

/// <summary>
/// Windows（WinUI 3）上的窗口：系统标题栏的高度收成 0（保留窗口外框），由页面里 AppShell 的顶栏充当标题栏
/// （WebView2 把 CSS 的 <c>app-region: drag</c> 区域当成标题栏：拖动、双击最大化、右键系统菜单、拖到屏幕边缘贴靠都是系统做的）。
/// </summary>
sealed class WindowsDesktopWindow(RuneDesktopOptions options) : IDesktopWindow
{
    const string BoundsKey = "rune.desktop.window";

    WinUIWindow? window;
    OverlappedPresenter? presenter;
    bool active = true;
    // 最近一次不是最大化、最小化时的位置与大小：最大化着关掉时记下的是它
    global::Windows.Graphics.RectInt32? normalBounds;

    public DesktopWindowState State => new(
        presenter?.State == OverlappedPresenterState.Maximized,
        active,
        window?.AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen);

    public event Action? StateChanged;

    public Task MinimizeAsync() => Run(() => presenter?.Minimize());

    public Task ToggleMaximizeAsync() => Run(() =>
    {
        if (presenter?.State == OverlappedPresenterState.Maximized) presenter.Restore();
        else presenter?.Maximize();
    });

    // 先藏起窗口再关：关的时候 WebView 先拆掉，窗口还在屏幕上会露出一帧默认底色（亮色系统是白的）
    public Task CloseAsync() => Run(() =>
    {
        window?.AppWindow.Hide();
        window?.Close();
    });

    DesktopWindowState notified;

    // 只在状态真的变了时通知：拖动边框调整大小时每一步都有 DidSizeChange，通知了 AppShell 就整页重新渲染，跟不上窗口，边缘闪烁
    void NotifyIfChanged()
    {
        var state = State;
        if (state == notified) return;
        notified = state;
        StateChanged?.Invoke();
    }

    /// <summary>窗口刚建好（MAUI 的根视图还没建）。</summary>
    public void Attach(WinUIWindow created)
    {
        if (window is not null) return;
        window = created;
        presenter = window.AppWindow.Presenter as OverlappedPresenter;
        // 保留 Windows 11 的窗口外框（细边、圆角、阴影、边缘调整大小），只把标题栏的高度收成 0：系统的窗口按钮也随之不见。
        // 不用 SetBorderAndTitleBar(true, false)：那样系统画的是老式的粗边框。内容延伸进标题栏（MAUI 默认已打开）
        window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        if (options.RememberWindow) RestoreBounds();
        RememberNormalBounds();
        notified = State;

        window.Activated += (_, e) =>
        {
            active = e.WindowActivationState != WindowActivationState.Deactivated;
            NotifyIfChanged();
        };
        window.AppWindow.Changed += (_, e) =>
        {
            if (e.DidPositionChange || e.DidSizeChange) RememberNormalBounds();
            if (e.DidPresenterChange || e.DidSizeChange) NotifyIfChanged();
        };
        // 用户关窗口（标题栏的关闭、Alt+F4、任务栏）：记下位置，先藏起窗口再拆（同 CloseAsync）；别处取消了关闭的不藏
        window.AppWindow.Closing += (sender, e) =>
        {
            if (e.Cancel) return;
            if (options.RememberWindow) SaveBounds();
            sender.Hide();
        };
        HideMauiTitleBar(window);
        CloakUntilFirstFrame(window);
    }

    // 显示窗口之前保底的等待：内容迟迟没画出来也不能让窗口一直看不见
    static readonly TimeSpan RevealTimeout = TimeSpan.FromSeconds(2);
    Microsoft.UI.Dispatching.DispatcherQueueTimer? revealTimer;

    /// <summary>
    /// 窗口刚建好时 MAUI 的内容还没放进去，WinUI 先画一段黑（约 0.3 秒）：先用 DWM 把窗口隐身（cloak：窗口照常存在、能激活、在任务栏，
    /// 只是屏幕上看不见），MAUI 的内容加载后再过两帧（底色与内容已经画出来）现身。保底 <see cref="RevealTimeout"/> 后一定现身。
    /// </summary>
    void CloakUntilFirstFrame(WinUIWindow target)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(target);
        SetCloak(hwnd, true);
        var frames = 0;
        var shown = false;
        void Show()
        {
            if (shown) return;
            shown = true;
            CompositionTarget.Rendering -= OnRendering;
            revealTimer?.Stop();
            SetCloak(hwnd, false);
        }
        void OnRendering(object? sender, object e)
        {
            if (target.Content is FrameworkElement { IsLoaded: true } && ++frames >= 2) Show();
        }
        CompositionTarget.Rendering += OnRendering;
        revealTimer = target.DispatcherQueue.CreateTimer();
        revealTimer.Interval = RevealTimeout;
        revealTimer.IsRepeating = false;
        revealTimer.Tick += (_, _) => Show();
        revealTimer.Start();
    }

    const int DwmwaCloak = 13;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    static void SetCloak(IntPtr hwnd, bool cloaked)
    {
        var value = cloaked ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DwmwaCloak, ref value, sizeof(int));
    }

    /// <summary>WebView2 建好后按桌面应用设置（每个 BlazorWebView 一次）。</summary>
    public void ConfigureWebView(WebView2 view)
    {
        // 页面第一次绘制之前 WebView2 的合成层是它自己的底色（暗色系统是 #202020，DefaultBackgroundColor 管不到这段）：
        // 先完全透明，页面真的画出来了（导航完成后再过两帧，启动画面已在屏幕上）再显示；这之前透出应用给页面设的背景，启动时颜色不跳
        view.Opacity = 0;
        view.CoreWebView2Initialized += (_, _) =>
        {
            var core = view.CoreWebView2;
            async void Reveal(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs e)
            {
                core.NavigationCompleted -= Reveal;
                try
                {
                    // ExecuteScriptAsync 不等 Promise；开发者工具协议的 Runtime.evaluate 能等
                    await core.CallDevToolsProtocolMethodAsync("Runtime.evaluate",
                        """{"expression":"new Promise(r => requestAnimationFrame(() => requestAnimationFrame(() => r(true))))","awaitPromise":true}""");
                }
                finally
                {
                    view.Opacity = 1;
                }
            }
            core.NavigationCompleted += Reveal;
            // WinUI 托管的 WebView2 只认能对应到系统光标的 CSS 光标：grab、grabbing 会退回成箭头，换成系统的手形（Rune.UI 的 Cursors）
            _ = core.AddScriptToExecuteOnDocumentCreatedAsync(CursorOverrides);
            var settings = core.Settings;
            // 页面里 app-region: drag 的区域当标题栏（AppShell 桌面模式的顶栏）
            settings.IsNonClientRegionSupportEnabled = true;
            // 指着链接时左下角的网址：桌面应用里没有
            settings.IsStatusBarEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
            if (!options.BrowserFeatures)
            {
                settings.AreBrowserAcceleratorKeysEnabled = false;
                settings.IsZoomControlEnabled = false;
                core.ContextMenuRequested += OnContextMenuRequested;
            }
            // 页面请求全屏（AppShell 的全屏、视频）：窗口本身全屏，不只是 WebView 里铺满
            core.ContainsFullScreenElementChanged += (_, _) => SetFullScreen(core.ContainsFullScreenElement);
        };
    }

    const string CursorOverrides = """
        document.addEventListener('DOMContentLoaded', () => {
          const style = document.createElement('style');
          style.textContent = ':root { --rune-cursor-grab: pointer; --rune-cursor-grabbing: pointer; }';
          document.head.append(style);
        });
        """;

    // 网页的右键菜单：只在输入框、选中的文字上给（剪切、复制、粘贴、全选），去掉后退、刷新、另存为、打印、检查这些浏览器的项
    static readonly HashSet<string> BrowserItems = ["back", "forward", "reload", "saveAs", "print", "createQrCode", "inspectElement", "share", "webCapture", "other"];

    static void OnContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        if (!e.ContextMenuTarget.IsEditable && !e.ContextMenuTarget.HasSelection)
        {
            e.Handled = true;
            return;
        }
        foreach (var item in e.MenuItems.Where(item => BrowserItems.Contains(item.Name)).ToList()) e.MenuItems.Remove(item);
    }

    void SetFullScreen(bool fullScreen)
    {
        if (window is null || presenter is null) return;
        if (fullScreen) window.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        else window.AppWindow.SetPresenter(presenter);
    }

    void RestoreBounds()
    {
        if (Preferences.Get(BoundsKey, null) is not { } saved) return;
        var parts = saved.Split(',');
        if (parts.Length != 5 || !parts.Take(4).All(p => int.TryParse(p, out _))) return;
        var rect = new global::Windows.Graphics.RectInt32(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
        // 保存时的显示器拔掉了：不恢复位置，免得窗口落在看不见的地方
        var area = DisplayArea.GetFromRect(rect, DisplayAreaFallback.None);
        if (area is null) return;
        window!.AppWindow.MoveAndResize(rect);
        if (parts[4] == "1") presenter!.Maximize();
    }

    void RememberNormalBounds()
    {
        if (window is null || presenter?.State != OverlappedPresenterState.Restored || window.AppWindow.Presenter.Kind != AppWindowPresenterKind.Overlapped) return;
        var (position, size) = (window.AppWindow.Position, window.AppWindow.Size);
        normalBounds = new(position.X, position.Y, size.Width, size.Height);
    }

    void SaveBounds()
    {
        if (normalBounds is not { } r) return;
        var maximized = presenter?.State == OverlappedPresenterState.Maximized ? 1 : 0;
        Preferences.Set(BoundsKey, $"{r.X},{r.Y},{r.Width},{r.Height},{maximized}");
    }

    Task Run(Action action)
    {
        if (window is null) return Task.CompletedTask;
        var done = new TaskCompletionSource();
        if (!window.DispatcherQueue.TryEnqueue(() =>
            {
                action();
                done.SetResult();
            }))
            done.SetResult();
        return done.Task;
    }

    /// <summary>
    /// MAUI 自己画的那条标题栏（AppTitleBarContainer）与给它让出的上边距：MAUI 本该在窗口去掉标题栏时收起，
    /// 但它比较 WM_STYLECHANGING 的 wParam 时把无符号的 GWL_STYLE 当成 -16，永远不成立；而且改标题、换主题时还会恢复，
    /// 还会按它那条的高度改系统标题栏的高度。
    /// 每次布局后检查一遍（元素找到后缓存，检查很便宜）。
    /// </summary>
    static void HideMauiTitleBar(WinUIWindow window)
    {
        FrameworkElement? container = null, content = null, grid = null;
        var none = new Thickness(0);
        void Check(object? sender, object e)
        {
            if (window.Content is not FrameworkElement root) return;
            container ??= Find(root, "AppTitleBarContainer");
            content ??= Find(root, "AppTitleBarContentControl");
            grid ??= Find(root, "ContentGrid");
            if (container is { Visibility: Visibility.Visible }) container.Visibility = Visibility.Collapsed;
            if (content is { Visibility: Visibility.Visible }) content.Visibility = Visibility.Collapsed;
            if (grid is not null && grid.Margin != none) grid.Margin = none;
            // MAUI 按它那条标题栏的高度改过系统标题栏的高度（Standard、Tall）：改回 0
            if (window.AppWindow.TitleBar.PreferredHeightOption != TitleBarHeightOption.Collapsed)
                window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        }
        // 窗口刚建好时 MAUI 还没放进根视图：第一次激活时挂上
        void Hook(object sender, WindowActivatedEventArgs e)
        {
            if (window.Content is not FrameworkElement root) return;
            window.Activated -= Hook;
            root.LayoutUpdated += Check;
        }
        window.Activated += Hook;
    }

    static FrameworkElement? Find(DependencyObject node, string name)
    {
        if (node is FrameworkElement { Name: var n } element && n == name) return element;
        if (node is WebView2) return null;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            if (Find(VisualTreeHelper.GetChild(node, i), name) is { } found) return found;
        return null;
    }
}
