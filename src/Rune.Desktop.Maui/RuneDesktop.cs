using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.LifecycleEvents;
using Rune.UI;

namespace Rune.Desktop;

/// <summary>
/// 桌面应用的设置。
/// </summary>
public sealed class RuneDesktopOptions
{
    /// <summary>
    /// 保留浏览器的功能：网页的右键菜单（后退、刷新、检查）、浏览器快捷键（Ctrl+R、F5、Ctrl+P、F12、Ctrl+F）、Ctrl+滚轮缩放。
    /// 默认调试版保留（方便 F12 打开开发者工具）、正式版关掉；输入框与选中文字的右键菜单（剪切、复制、粘贴）一直保留。
    /// </summary>
    public bool BrowserFeatures { get; set; } = IsDebugBuild();

    /// <summary>记住窗口的大小、位置与是否最大化，下次打开时恢复，默认 true。</summary>
    public bool RememberWindow { get; set; } = true;

    // 应用（入口程序集）是调试版：编译器给调试版加了允许 JIT 跟踪的 DebuggableAttribute
    static bool IsDebugBuild() =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<DebuggableAttribute>()?.IsJITTrackingEnabled == true;
}

/// <summary>在 .NET MAUI 里用 Rune 做桌面应用。</summary>
public static class RuneDesktopExtensions
{
    /// <summary>
    /// 桌面应用（目前只支持 Windows）：注册 <see cref="IDesktopWindow"/>（AppShell 的顶栏因此成为窗口的标题栏，结束边出现窗口按钮），
    /// 去掉系统的标题栏，按桌面应用设置 WebView2（标题栏的拖动区域、全屏、右键菜单与浏览器快捷键）。
    /// <code>builder.UseMauiApp&lt;App&gt;().UseRuneDesktop();
    /// builder.Services.AddMauiBlazorWebView();
    /// builder.Services.AddRuneUI();</code>
    /// 应用只有一个窗口（<see cref="IDesktopWindow"/> 指第一个窗口）。
    /// </summary>
    public static MauiAppBuilder UseRuneDesktop(this MauiAppBuilder builder, Action<RuneDesktopOptions>? configure = null)
    {
        var options = new RuneDesktopOptions();
        configure?.Invoke(options);
#if WINDOWS
        var window = new Windows.WindowsDesktopWindow(options);
        builder.Services.AddSingleton<IDesktopWindow>(window);
        builder.ConfigureLifecycleEvents(events => events.AddWindows(w => w.OnWindowCreated(window.Attach)));
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("RuneDesktop", (handler, _) => window.ConfigureWebView(handler.PlatformView));
#endif
        return builder;
    }
}
