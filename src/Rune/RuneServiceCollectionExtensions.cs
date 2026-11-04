using Microsoft.Extensions.DependencyInjection;
using Rune.Styles;

namespace Rune;

/// <summary>注册 Rune 的服务。</summary>
public static class RuneServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Rune 需要的服务（样式登记表等）。服务端项目与 WebAssembly 项目各调用一次：
    /// <code>builder.Services.AddRune();</code>
    /// </summary>
    public static IServiceCollection AddRune(this IServiceCollection services)
    {
        services.AddScoped<StyleRegistry>();
        return services;
    }
}
