using Microsoft.Extensions.DependencyInjection;
using Rune.Docs.Components.Config.Signals.Demo;

namespace Rune.Docs.Site;

/// <summary>文档内容要的服务（本页目录的标题、示例里注入的共享状态）：文档站与渲染模式测试台的服务端、WebAssembly 各调用一次。</summary>
public static class DocsServices
{
    /// <summary>注册示例用到的服务。</summary>
    public static IServiceCollection AddDocsContent(this IServiceCollection services) =>
        services.AddScoped<CounterStore>().AddScoped<DocHeadings>();
}
