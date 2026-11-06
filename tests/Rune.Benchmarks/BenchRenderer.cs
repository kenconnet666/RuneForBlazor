using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Rune.Benchmarks;

/// <summary>
/// 只有 Blazor 自己的渲染与比较、没有输出的渲染器（交互模式）：测出来的是组件与 Rune 的开销。
/// bUnit 每次渲染都要重新生成整页 HTML、再解析成 DOM，组件一多它自己的开销就盖过了要测的东西。
/// </summary>
sealed class BenchRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
{
    /// <summary>交给显示的批次数。</summary>
    public int Batches { get; private set; }

    /// <summary>这些批次里重新渲染过的组件数。</summary>
    public int RenderedComponents { get; private set; }

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    protected override RendererInfo RendererInfo { get; } = new("Server", isInteractive: true);

    /// <summary>挂上根组件并按参数渲染（在渲染器的线程上）。</summary>
    public Task<int> RenderAsync<T>(IDictionary<string, object?> parameters) where T : IComponent =>
        Dispatcher.InvokeAsync(async () =>
        {
            // 经渲染器创建：做属性注入（[Inject]），同真实的根组件
            var id = AssignRootComponentId(InstantiateComponent(typeof(T)));
            await RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters));
            return id;
        });

    /// <summary>给根组件新的参数（在渲染器的线程上）。</summary>
    public Task SetParametersAsync(int id, IDictionary<string, object?> parameters) =>
        Dispatcher.InvokeAsync(() => RenderRootComponentAsync(id, ParameterView.FromDictionary(parameters)));

    protected override void HandleException(Exception exception) => ExceptionDispatchInfo.Throw(exception);

    protected override Task UpdateDisplayAsync(in RenderBatch batch)
    {
        Batches++;
        RenderedComponents += batch.UpdatedComponents.Count;
        return Task.CompletedTask;
    }
}
