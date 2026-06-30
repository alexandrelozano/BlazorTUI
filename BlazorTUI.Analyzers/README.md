# BlazorTUI.Analyzers

Optional Roslyn analyzers for projects that consume BlazorTUI.

Install it as a private development dependency:

```xml
<PackageReference Include="BlazorTUI.Analyzers" Version="0.8.15" PrivateAssets="all" />
```

Diagnostics:

- `BTUI001`: duplicate BlazorTUI control or container name in the analyzed scope.
- `BTUI002`: invalid width, height, or length literal.
- `BTUI003`: duplicate menu, item, or tree node name in the analyzed scope.
- `BTUI004`: `SetFocus` targets a control name that is not created in the analyzed scope.
