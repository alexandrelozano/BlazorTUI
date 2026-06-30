using System.Collections.Immutable;
using BlazorTUI.Analyzers;
using BlazorTUI.TUI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BlazorTUI.Tests;

public sealed class AnalyzerTests
{
    [Fact]
    public async Task AnalyzerAcceptsValidBlazorTuiUsage()
    {
        string source = """
            using System.Drawing;
            using BlazorTUI.TUI;

            public static class Sample
            {
                public static void Build()
                {
                    var screen = new Screen(80, 25);
                    var save = new Button("save", "Save", 1, 1, 10, Color.Yellow, Color.Black);
                    var cancel = new Button("cancel", "Cancel", 1, 2, 12, Color.Yellow, Color.Black);
                    var item = new ContextMenuItem("refresh", "Refresh");
                    screen.SetFocus("save");
                }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = await GetBlazorTuiDiagnosticsAsync(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AnalyzerReportsDuplicateControlAndContainerNames()
    {
        string source = """
            using System.Drawing;
            using BlazorTUI.TUI;

            public static class Sample
            {
                public static void Build()
                {
                    var first = new Button("save", "Save", 1, 1, 10, Color.Yellow, Color.Black);
                    var second = new TextBox("save", "", 1, 2, 10, Color.Yellow, Color.Black);
                }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = await GetBlazorTuiDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(BlazorTuiUsageAnalyzer.DuplicateControlNameId, diagnostic.Id);
    }

    [Fact]
    public async Task AnalyzerReportsInvalidDimensionLiterals()
    {
        string source = """
            using System.Drawing;
            using BlazorTUI.TUI;

            public static class Sample
            {
                public static void Build()
                {
                    var screen = new Screen(80, 0);
                    var button = new Button("save", "Save", 1, 1, 0, Color.Yellow, Color.Black);
                    button.Width = 0;
                }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = await GetBlazorTuiDiagnosticsAsync(source);

        Assert.True(diagnostics.Count(diagnostic => diagnostic.Id == BlazorTuiUsageAnalyzer.InvalidDimensionId) >= 3);
    }

    [Fact]
    public async Task AnalyzerReportsDuplicateNamedItems()
    {
        string source = """
            using BlazorTUI.TUI;

            public static class Sample
            {
                public static void Build()
                {
                    var first = new ContextMenuItem("refresh", "Refresh");
                    var second = new ContextMenuItem("refresh", "Refresh again");
                }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = await GetBlazorTuiDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(BlazorTuiUsageAnalyzer.DuplicateItemNameId, diagnostic.Id);
    }

    [Fact]
    public async Task AnalyzerReportsMissingFocusTargetInConstructedScope()
    {
        string source = """
            using System.Drawing;
            using BlazorTUI.TUI;

            public static class Sample
            {
                public static void Build()
                {
                    var screen = new Screen(80, 25);
                    var save = new Button("save", "Save", 1, 1, 10, Color.Yellow, Color.Black);
                    screen.SetFocus("missing");
                }
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = await GetBlazorTuiDiagnosticsAsync(source);

        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Equal(BlazorTuiUsageAnalyzer.MissingFocusTargetId, diagnostic.Id);
    }

    private static async Task<IReadOnlyList<Diagnostic>> GetBlazorTuiDiagnosticsAsync(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "BlazorTUIAnalyzerTests",
            new[] { syntaxTree },
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        ImmutableArray<DiagnosticAnalyzer> analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new BlazorTuiUsageAnalyzer());
        ImmutableArray<Diagnostic> diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync();

        return diagnostics
            .Where(diagnostic => diagnostic.Id.StartsWith("BTUI", StringComparison.Ordinal))
            .OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToArray();
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
        HashSet<string> paths = trustedAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        paths.Add(typeof(Screen).Assembly.Location);

        return paths.Select(path => MetadataReference.CreateFromFile(path));
    }
}
