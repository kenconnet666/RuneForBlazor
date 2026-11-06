using BenchmarkDotNet.Running;

// 按类、按方法挑：dotnet run -c Release -- --filter '*StyleBenchmarks*'
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
