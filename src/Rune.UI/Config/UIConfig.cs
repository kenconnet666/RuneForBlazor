using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;

namespace Rune.UI;

/// <summary>
/// 组件库的配置：主题、组件的定制（默认值、按部件与按档位取值的样式）。应用级的来自 <see cref="RuneUIServiceCollectionExtensions.AddRuneUI"/>，
/// <see cref="RuneProvider"/> 在其中一片区域里叠加修改。组件经 <see cref="UIComponent.Config"/> 读取。
/// </summary>
/// <param name="Theme">主题（亮色）。</param>
/// <param name="DarkTheme">暗色主题；为 null 时始终用 <paramref name="Theme"/>。</param>
/// <param name="ColorScheme">亮暗模式。</param>
/// <param name="Components">组件的定制（<see cref="ComponentConfigs"/>）。</param>
/// <param name="Ripple">可点击的组件（按钮、菜单项）按下时的波纹，默认开启；组件的 Ripple 参数可以单独覆盖。</param>
public sealed record UIConfig(Theme Theme, Theme? DarkTheme, ColorScheme ColorScheme, ComponentConfigs Components, bool Ripple = true)
{
    /// <summary>组件默认值（由 <see cref="Components"/> 合成）：组件上没写的参数取这里的值。</summary>
    public ComponentDefaults Defaults => Components.Defaults;

    /// <summary>样式定制（由 <see cref="Components"/> 合成）：配方据此在组件的样式之后追加。</summary>
    public ComponentStyles Styles => Components.Styles;

    /// <summary>语言：组件里的文案（关闭、清空、共 N 条）与地区信息，默认简体中文。</summary>
    public Lang Lang { get; init; } = Lang.ZhCn;

    /// <summary>
    /// 语言是配置出来的（AddRuneUI 写了 Lang / Languages，或某一层 RuneProvider 写了 Lang），不是默认值：
    /// 这时 RuneProvider 才往包裹元素上写 lang、dir；没配置时沿用宿主页面自己的。
    /// </summary>
    public bool LangConfigured { get; init; }

    /// <summary>
    /// 另配的文字匹配（如拼音：“bj”搜到“北京”）：选项过滤时与内置匹配（按语言忽略大小写与重音）任一命中都算；null 时只用内置的。
    /// </summary>
    public TextMatcher? TextMatcher { get; init; }

    /// <summary>用户可以选的几套主题（RuneProvider 的 Choices）；ThemeSwitcher 列出它们。没有时为 null。</summary>
    public ThemeChoices? ThemeChoices { get; init; }

    /// <summary>工作日历（法定节假日与调休）：DeadlineBadge 这类按工作日算的组件用它；默认只按周末。</summary>
    public WorkCalendar WorkCalendar { get; init; } = WorkCalendar.Weekdays;

    /// <summary>代码高亮器：CodeBlock 用它上色；null 时在浏览器里用 Shiki。</summary>
    public CodeHighlighter? Highlighter { get; init; }

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

    /// <summary>组件的定制：默认值、按部件与按档位取值的样式，每个组件一项（见 <see cref="ComponentConfigs"/>）。</summary>
    public ComponentConfigs Components { get; set; } = ComponentConfigs.Empty;

    /// <summary>可点击的组件按下时的波纹，默认开启。</summary>
    public bool Ripple { get; set; } = true;

    /// <summary>语言，默认简体中文（<see cref="Lang.ZhCn"/>）；英文界面用 <see cref="Lang.EnUs"/>。</summary>
    public Lang Lang
    {
        get;
        set
        {
            field = value;
            LangConfigured = true;
        }
    } = Lang.ZhCn;

    // 写过 Lang（或给了 Languages）：语言是配置出来的，最外层的 RuneProvider 把 lang、dir 写到包裹元素上
    internal bool LangConfigured { get; private set; }

    /// <summary>
    /// 按界面文化选语言：每个请求（Server 是每个电路）按当时的 <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>
    /// （通常由 ASP.NET Core 的 RequestLocalization 设定）从这几个里选（<see cref="Lang.Match"/>），都不匹配时用 <see cref="Lang"/>。
    /// 不写时一直用 <see cref="Lang"/>。<c>o.Languages = [Lang.ZhCn, Lang.EnUs];</c>
    /// </summary>
    public IReadOnlyList<Lang>? Languages { get; set; }

    /// <inheritdoc cref="UIConfig.TextMatcher"/>
    public TextMatcher? TextMatcher { get; set; }

    /// <summary>
    /// 用户可以选的几套主题（ThemeSwitcher 切换，记在浏览器里）：最外层的 RuneProvider 输出每套的变量，ThemeBootScript 按它防闪烁。
    /// 写在这里而不是 RuneProvider 上：交互模式的根接不到布局里级联下来的配置，服务注册的配置三种渲染模式都拿得到。
    /// </summary>
    public ThemeChoices? ThemeChoices { get; set; }

    /// <inheritdoc cref="UIConfig.WorkCalendar"/>
    public WorkCalendar WorkCalendar { get; set; } = WorkCalendar.Weekdays;

    /// <inheritdoc cref="UIConfig.Highlighter"/>
    public CodeHighlighter? Highlighter { get; set; }

    /// <summary>
    /// 应用级主题（含可选主题）的变量放进外部样式表，不内联在每个页面里：带十几套可选主题时变量有一百多 KB，
    /// 放进样式表后浏览器只下载一次、长期缓存。打开时 DI 里有 <see cref="Rune.Styles.ThemeStylesheet"/>：
    /// 应用按它的文件名提供这个文件，在 head 里放 <see cref="ThemeStylesheetLink"/>。服务端与 WebAssembly 要一致。
    /// </summary>
    public bool ThemeStylesheet { get; set; }
}

/// <summary>注册组件库的服务。</summary>
public static class RuneUIServiceCollectionExtensions
{
    static readonly GlobalRule PartBoxSizing = new(":where([data-part])", new Style { BoxSizing = BoxSizing.BorderBox });

    /// <summary>
    /// 注册组件库（含 Rune 运行时）与应用级配置。服务端项目与 WebAssembly 项目各调用一次，配置要一致：
    /// <code>
    /// builder.Services.AddRuneUI(o => o.Components = new() { new ComponentConfig&lt;CardDefaults, CardPart&gt; { Defaults = new() { Variant = CardVariant.Outline } } });
    /// </code>
    /// </summary>
    // 自定义事件（RuneEvents.cs 的 [EventHandler]）的参数由 Blazor 按反射从 JSON 反序列化，但 [EventHandler] 没有告诉裁剪器要留下这些类型的构造函数与属性：
    // WebAssembly 发布裁剪后反序列化失败（DeserializeNoConstructor），rune-command 等事件全部不生效（虚拟滚动不再换行）。每个应用都调用这里，挂在这里一定保留
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties, typeof(OverlayToggleEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties, typeof(CloseRequestEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties, typeof(CommandEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties, typeof(CopiedEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties, typeof(ResizeEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties, typeof(SlideEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties, typeof(ReorderEventArgs))]
    public static IServiceCollection AddRuneUI(this IServiceCollection services, Action<RuneUIOptions>? configure = null)
    {
        var options = new RuneUIOptions();
        configure?.Invoke(options);
        services.AddRune();
        var config = new UIConfig(options.Theme, options.DarkTheme, options.ColorScheme, options.Components, options.Ripple) { Lang = options.Lang, LangConfigured = options.LangConfigured || options.Languages is { Count: > 0 }, TextMatcher = options.TextMatcher, ThemeChoices = options.ThemeChoices, WorkCalendar = options.WorkCalendar, Highlighter = options.Highlighter };
        if (options.Languages is { Count: > 0 } languages)
        {
            // 文化随请求变：每个作用域（请求、电路）第一次用到时按当时的界面文化选一次，同一作用域里一直是同一个对象
            var byLang = languages.Append(options.Lang).Distinct().ToDictionary(l => l, l => config with { Lang = l });
            services.AddScoped(_ => byLang[Lang.Match(System.Globalization.CultureInfo.CurrentUICulture, languages) ?? options.Lang]);
        }
        else
        {
            services.AddSingleton(config);
        }
        if (options.ThemeStylesheet)
            services.AddSingleton(options.ThemeChoices is { } choices
                ? ThemeStyles.Stylesheet(choices.SystemLight, choices.SystemDark, ColorScheme.System, choices.Themes, choices.SystemContrast)
                : ThemeStyles.Stylesheet(options.Theme, options.DarkTheme, options.ColorScheme));
        // 部件一律 border-box：使用方给部件写的宽高就是整个部件的宽高（同 vue-tsx-pro 313f0e23）
        services.AddSingleton(PartBoxSizing);
        services.AddSingleton(AspectRatioStyles.Fill);
        services.AddSingleton(AspectRatioStyles.Cover);
        services.AddScoped<ToastService>();
        services.AddScoped<DialogService>();
        services.AddScoped<RecentColors>();
        return services;
    }
}
