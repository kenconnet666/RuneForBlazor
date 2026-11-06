# Rune.Benchmarks

每次渲染都走的路径的性能基准（BenchmarkDotNet）：样式定义的查找、属性合并、样式登记、参数比较，以及整页的渲染（只数批次、没有 DOM 的渲染器）。
CI 只构建、不运行；改了这些路径时在本机跑一遍，前后对比。

```bash
dotnet run -c Release --project tests/Rune.Benchmarks -- --filter '*'
```

只跑一类：`--filter '*StyleBenchmarks*'`；快速看个大概：加 `--job short`。结果在 `BenchmarkDotNet.Artifacts/`（不提交）。

历次的数字、改法与待议项记在 `docs/PERFORMANCE.md`。
