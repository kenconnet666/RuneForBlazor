using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 组件库的配置：主题、组件默认值、按部件的样式定制。应用级的来自 <see cref="RuneUIServiceCollectionExtensions.AddRuneUI"/>，
/// <see cref="RuneProvider"/> 在其中一片区域里叠加修改。组件经 <see cref="UIComponent.Config"/> 读取。
/// </summary>
/// <param name="Theme">主题（亮色）。</param>
/// <param name="DarkTheme">暗色主题；为 null 时始终用 <paramref name="Theme"/>。</param>
/// <param name="ColorScheme">亮暗模式。</param>
/// <param name="Defaults">组件默认值。</param>
/// <param name="Styles">按部件的样式定制。</param>
/// <param name="Ripple">可点击的组件（按钮、菜单项）按下时的波纹，默认开启；组件的 Ripple 参数可以单独覆盖。</param>
public sealed record UIConfig(Theme Theme, Theme? DarkTheme, ColorScheme ColorScheme, ComponentDefaults Defaults, ComponentStyles Styles, bool Ripple = true)
{
    /// <summary>语言：组件里的文案（关闭、清空、共 N 条）与地区信息，默认简体中文。</summary>
    public Lang Lang { get; init; } = Lang.ZhCn;

    /// <summary>
    /// 另配的文字匹配（如拼音：“bj”搜到“北京”）：选项过滤时与内置匹配（按语言忽略大小写与重音）任一命中都算；null 时只用内置的。
    /// </summary>
    public TextMatcher? TextMatcher { get; init; }

    /// <summary>用户可以选的几套主题（RuneProvider 的 Choices）；ThemeSwitcher 列出它们。没有时为 null。</summary>
    public ThemeChoices? ThemeChoices { get; init; }

    /// <summary>当前配置下的文字匹配：内置匹配（按 <see cref="Lang"/> 的语言）或 <see cref="TextMatcher"/> 任一命中。</summary>
    public bool Matches(string text, string query) =>
        TextMatching.For(Lang.Code)(text, query) || (TextMatcher?.Invoke(text, query) ?? false);
}

/// <summary>应用级配置（<see cref="RuneUIServiceCollectionExtensions.AddRuneUI"/> 里设置）。</summary>
public sealed class RuneUIOptions
{
    /// <summary>主题（亮色），默认 <see cref="Themes.Light"/>。</summary>
    public Theme Theme { get; set; } = Themes.Light;

    /// <summary>暗色主题，默认 <see cref="Themes.Dark"/>；设为 null 时不切换暗色。</summary>
    public Theme? DarkTheme { get; set; } = Themes.Dark;

    /// <summary>亮暗模式，默认跟随系统。</summary>
    public ColorScheme ColorScheme { get; set; } = ColorScheme.System;

    /// <summary>组件默认值。</summary>
    public ComponentDefaults Defaults { get; set; } = ComponentDefaults.Empty;

    /// <summary>按部件的样式定制。</summary>
    public ComponentStyles Styles { get; set; } = ComponentStyles.Empty;

    /// <summary>可点击的组件按下时的波纹，默认开启。</summary>
    public bool Ripple { get; set; } = true;

    /// <summary>语言，默认简体中文（<see cref="Lang.ZhCn"/>）；英文界面用 <see cref="Lang.EnUs"/>。</summary>
    public Lang Lang { get; set; } = Lang.ZhCn;

    /// <inheritdoc cref="UIConfig.TextMatcher"/>
    public TextMatcher? TextMatcher { get; set; }

    /// <summary>
    /// 用户可以选的几套主题（ThemeSwitcher 切换，记在浏览器里）：最外层的 RuneProvider 输出每套的变量，ThemeBootScript 按它防闪烁。
    /// 写在这里而不是 RuneProvider 上：交互模式的根接不到布局里级联下来的配置，服务注册的配置三种渲染模式都拿得到。
    /// </summary>
    public ThemeChoices? ThemeChoices { get; set; }
}

/// <summary>注册组件库的服务。</summary>
public static class RuneUIServiceCollectionExtensions
{
    /// <summary>
    /// 注册组件库（含 Rune 运行时）与应用级配置。服务端项目与 WebAssembly 项目各调用一次，配置要一致：
    /// <code>
    /// builder.Services.AddRuneUI(o => o.Defaults = new() { new CardDefaults { Variant = CardVariant.Outline } });
    /// </code>
    /// </summary>
    public static IServiceCollection AddRuneUI(this IServiceCollection services, Action<RuneUIOptions>? configure = null)
    {
        var options = new RuneUIOptions();
        configure?.Invoke(options);
        services.AddRune();
        services.AddSingleton(new UIConfig(options.Theme, options.DarkTheme, options.ColorScheme, options.Defaults, options.Styles, options.Ripple) { Lang = options.Lang, TextMatcher = options.TextMatcher, ThemeChoices = options.ThemeChoices });
        services.AddScoped<ToastService>();
        services.AddScoped<DialogService>();
        return services;
    }
}
