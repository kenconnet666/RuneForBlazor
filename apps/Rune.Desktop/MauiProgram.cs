using Microsoft.AspNetCore.Components.WebView.Maui;
using Rune.Docs.Server;
using Rune.Docs.Site;
using Rune.UI;

namespace Rune.Desktop;

/// <summary>
/// 桌面壳的启动：一个窗口，整页是 BlazorWebView，根组件是 <see cref="Components.Routes"/>。
/// UseRuneDesktop（RuneForBlazor.Desktop.Maui）去掉系统标题栏，AppShell 的顶栏成为窗口的标题栏。
/// </summary>
public static class MauiProgram
{
    /// <summary>建好 MAUI 应用。</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseRuneDesktop();

        builder.Services.AddMauiBlazorWebView();
#if DEBUG
        // F12 打开 WebView2 的开发者工具
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif
        builder.Services.AddRuneUI(o => o.ThemeChoices = ThemeChoices.Builtin);
        builder.Services.AddDocsContent().AddDocsServer();
        if (SelfCheck.Report is not null) SelfCheck.AddLogging(builder.Logging);
        return builder.Build();
    }
}

/// <summary>应用：一个窗口。</summary>
public sealed class App : Application
{
    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var webView = new BlazorWebView
        {
            HostPage = "wwwroot/index.html",
            RootComponents = { new RootComponent { Selector = "#app", ComponentType = typeof(Components.Routes) } },
        };
        if (SelfCheck.Report is not null) SelfCheck.Attach(webView);
        // 窗口刚打开、页面还没画出来时（WebView 在那之前是透明的，见 RuneForBlazor.Desktop.Maui）：底色同 Rune 内置亮、暗主题的 Background、
        // 跟随系统明暗，中间是标志；页面里的启动画面在同一个位置画同一个标志，接着呼吸，内容出来后淡出
        var logo = new Microsoft.Maui.Controls.Image { Source = "startup_logo.png", WidthRequest = 56, HeightRequest = 56, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        var page = new ContentPage { Content = new Microsoft.Maui.Controls.Grid { Children = { logo, webView } } };
        page.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#f9f9fb"), Color.FromArgb("#111113"));
        return new Window(page) { Title = "Rune" };
    }
}
