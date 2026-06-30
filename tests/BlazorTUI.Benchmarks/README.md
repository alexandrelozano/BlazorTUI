# BlazorTUI benchmarks

This project contains manual performance benchmarks for the core rendering and state paths.

Run all benchmarks from the repository root:

```powershell
dotnet run --project .\tests\BlazorTUI.Benchmarks\BlazorTUI.Benchmarks.csproj -c Release
```

Run one benchmark class:

```powershell
dotnet run --project .\tests\BlazorTUI.Benchmarks\BlazorTUI.Benchmarks.csproj -c Release -- --filter *RenderingBenchmarks*
```

The normal CI build compiles this project but does not execute the benchmarks.
