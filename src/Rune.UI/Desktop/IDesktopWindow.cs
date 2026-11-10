namespace Rune.UI;

/// <summary>
/// 桌面应用的窗口（Blazor Hybrid：.NET MAUI、WPF、WinForms 的 BlazorWebView）：最小化、最大化、关闭与窗口的状态。
/// 由桌面适配包注册（RuneForBlazor.Desktop.Maui 的 <c>AddRuneDesktop</c>）；注册了它，AppShell 的顶栏就是窗口的标题栏
/// （空白处拖动窗口，结束边是 <see cref="WindowControls"/>），浏览器里没有它，一切照旧。
/// </summary>
public interface IDesktopWindow
{
    /// <summary>当前状态。</summary>
    DesktopWindowState State { get; }

    /// <summary>状态变了（最大化、还原、获得或失去焦点、进出全屏）；可能在任意线程上触发。</summary>
    event Action? StateChanged;

    /// <summary>最小化。</summary>
    Task MinimizeAsync();

    /// <summary>最大化，已最大化时还原。</summary>
    Task ToggleMaximizeAsync();

    /// <summary>关闭窗口。</summary>
    Task CloseAsync();
}

/// <summary>窗口的状态。</summary>
/// <param name="Maximized">最大化了。</param>
/// <param name="Active">是前台的活动窗口（失去焦点时标题栏变淡）。</param>
/// <param name="FullScreen">全屏中（没有标题栏，由 AppShell 的全屏触发）。</param>
public readonly record struct DesktopWindowState(bool Maximized, bool Active, bool FullScreen);
