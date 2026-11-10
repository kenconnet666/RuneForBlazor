using Microsoft.AspNetCore.Components.WebView.Maui;
using Rune.Docs.Server;
using Rune.Docs.Site;
using Rune.UI;

namespace Rune.Desktop;

/// <summary>桌面壳的启动：一个窗口，整页是 BlazorWebView，根组件是 <see cref="Components.Routes"/>。</summary>
public static class MauiProgram
{
    /// <summary>建好 MAUI 应用。</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

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
        return new Window(new ContentPage { Content = webView }) { Title = "Rune" };
    }
}
