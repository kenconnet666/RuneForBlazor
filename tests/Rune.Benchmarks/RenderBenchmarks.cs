using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Rune.UI;

namespace Rune.Benchmarks;

/// <summary>
/// 整页的渲染（<see cref="BenchRenderer"/>，没有 DOM）：第一次渲染、父级重新渲染而子组件参数没变（应当跳过）、
/// 父级改了一个参数让所有按钮都重新渲染。
/// </summary>
[MemoryDiagnoser]
public class RenderBenchmarks
{
    ServiceProvider provider = default!;
    AsyncServiceScope scope;
    BenchRenderer renderer = default!;
    int grid;
    int version;
    bool disabled;

    /// <summary>每种组件各几个（按钮、徽标、图标）。</summary>
    [Params(100)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddRuneUI();
        provider = services.BuildServiceProvider();
        scope = provider.CreateAsyncScope();
        renderer = new BenchRenderer(scope.ServiceProvider);
        grid = renderer.RenderAsync<ButtonGrid>(Parameters()).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        renderer.Dispose();
        scope.Dispose();
        provider.Dispose();
    }

    [Benchmark(Description = "第一次渲染")]
    public int FirstRender()
    {
        using var fresh = new BenchRenderer(scope.ServiceProvider);
        fresh.RenderAsync<ButtonGrid>(Parameters()).GetAwaiter().GetResult();
        return fresh.RenderedComponents;
    }

    [Benchmark(Description = "父级重新渲染、子组件参数没变")]
    public void RerenderUnchanged()
    {
        version++;
        renderer.SetParametersAsync(grid, Parameters()).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "父级改参数、按钮都重新渲染")]
    public void RerenderChanged()
    {
        disabled = !disabled;
        renderer.SetParametersAsync(grid, Parameters()).GetAwaiter().GetResult();
    }

    Dictionary<string, object?> Parameters() => new()
    {
        [nameof(ButtonGrid.Count)] = Count,
        [nameof(ButtonGrid.Disabled)] = disabled,
        [nameof(ButtonGrid.Version)] = version,
    };
}
