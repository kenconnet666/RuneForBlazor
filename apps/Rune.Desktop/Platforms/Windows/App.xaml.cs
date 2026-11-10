namespace Rune.Desktop.WinUI;

/// <summary>WinUI 的应用入口：交给 MAUI。</summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>WinUI 先建这个对象。</summary>
    public App() => InitializeComponent();

    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
