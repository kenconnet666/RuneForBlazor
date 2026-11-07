using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rune.Docs.Components.Config.Signals.Demo;

namespace Rune.Docs.Site;

/// <summary>文档内容要的服务（本页目录的标题、排队渲染、示例里注入的共享状态、AuthorizeView 要的授权服务）：文档站与渲染模式测试台的服务端、WebAssembly 各调用一次。</summary>
public static class DocsServices
{
    /// <summary>
    /// 注册示例用到的服务。服务端传入配置：配置项 <c>FixedNow</c>（ISO 8601，命令行写 <c>--FixedNow=2026-10-21T02:00:00Z</c>）
    /// 把“现在”固定下来，对照与截图时服务端渲染的相对时间、倒计时、日历的今天与浏览器里一致（浏览器里由 Playwright 的 clock 固定同一时刻）。
    /// </summary>
    public static IServiceCollection AddDocsContent(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<CounterStore>().AddScoped<DocHeadings>().AddScoped<RenderQueue>().AddAuthorizationCore();
        if (configuration?["FixedNow"] is { } text)
        {
            var now = DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            DemoClock.Source = new FixedTimeProvider(now);
            services.AddSingleton(DemoClock.Source);
        }
        return services;
    }

    sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
