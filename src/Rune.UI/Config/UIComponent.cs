using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Rune.UI;

/// <summary>
/// 组件库里组件的基类：在 <see cref="RuneComponent"/> 之上提供配置（主题、默认值、样式定制）。
/// </summary>
/// <remarks>
/// 配置先取最近的 <see cref="RuneProvider"/>，没有时取应用级的（DI）。级联值不能跨渲染模式边界：
/// 交互区域里拿到的是应用级配置，需要局部修改时在交互区域里再放一个 RuneProvider。
/// </remarks>
public abstract class UIComponent : RuneComponent
{
    /// <summary>最近的 RuneProvider 给的配置。</summary>
    [CascadingParameter] UIConfig? ProvidedConfig { get; set; }

    /// <summary>应用级配置。</summary>
    [Inject] UIConfig AppConfig { get; set; } = default!;

    /// <summary>当前生效的配置。</summary>
    protected UIConfig Config => ProvidedConfig ?? AppConfig;

    /// <summary>是否在某个 RuneProvider 里面（同一渲染模式的区域内）。</summary>
    protected bool IsInsideProvider => ProvidedConfig is not null;

    [Inject] IServiceProvider Services { get; set; } = default!;

    /// <summary>“现在”的来源：DI 里注册了 <see cref="TimeProvider"/> 就用它（测试、截图给固定的时间），否则是系统时钟。</summary>
    protected TimeProvider TimeSource => Services.GetService<TimeProvider>() ?? TimeProvider.System;

    /// <summary>此刻在时区里的墙上时间，默认语言的时区。</summary>
    protected DateTimeOffset LocalNow(TimeZoneInfo? zone = null) => TimeZoneInfo.ConvertTime(TimeSource.GetUtcNow(), zone ?? LangFormat.ZoneOf(Config.Lang));

    /// <summary>今天：时区里的日期，默认语言的时区。</summary>
    protected DateOnly LocalToday(TimeZoneInfo? zone = null) => DateOnly.FromDateTime(LocalNow(zone).DateTime);
}
