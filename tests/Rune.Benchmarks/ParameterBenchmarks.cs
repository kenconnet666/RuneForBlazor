using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components;
using Rune.UI;

namespace Rune.Benchmarks;

/// <summary>父级重新渲染时每个子组件都要做的参数比较（参数没变就跳过渲染）：一个常见的按钮的参数。</summary>
[MemoryDiagnoser]
public class ParameterBenchmarks
{
    readonly IconButton button = new();
    ParameterView parameters;

    [GlobalSetup]
    public void Setup()
    {
        parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(IconButton.Icon)] = BuiltInIcons.X,
            [nameof(IconButton.Label)] = "关闭",
            [nameof(IconButton.Variant)] = ButtonVariant.Ghost,
            [nameof(IconButton.Size)] = Size.Sm,
            [nameof(IconButton.Disabled)] = false,
            ["onclick"] = EventCallback.Empty,
        });
        parameters.SetParameterProperties(button);
    }

    [Benchmark(Description = "参数比较（没变）")]
    public bool Unchanged() => ParameterChangeDetector.HasChanged(button, parameters, button.AdditionalAttributes);
}
