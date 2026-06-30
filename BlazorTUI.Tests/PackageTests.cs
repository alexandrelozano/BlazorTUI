using System.IO.Compression;
using System.Xml.Linq;

namespace BlazorTUI.Tests;

public class PackageTests
{
    [Fact]
    public void GeneratedPackageContainsExpectedMetadataAndAssets()
    {
        string root = FindRepositoryRoot();
        string projectPath = Path.Combine(root, "BlazorTUI", "BlazorTUI.csproj");
        XDocument project = XDocument.Load(projectPath);
        string version = project.Descendants("Version").Single().Value;
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        string packagePath = Path.Combine(root, "BlazorTUI", "bin", configuration, $"BlazorTUI.{version}.nupkg");
        string symbolsPackagePath = Path.Combine(root, "BlazorTUI", "bin", configuration, $"BlazorTUI.{version}.snupkg");
        Assert.True(File.Exists(packagePath), $"Package was not found at {packagePath}.");
        Assert.True(File.Exists(symbolsPackagePath), $"Symbols package was not found at {symbolsPackagePath}.");

        Assert.Equal("true", ReadProjectProperty(project, "Deterministic"));
        Assert.Equal("true", ReadProjectProperty(project, "ContinuousIntegrationBuild"));
        Assert.Equal("portable", ReadProjectProperty(project, "DebugType"));
        Assert.Equal("true", ReadProjectProperty(project, "PublishRepositoryUrl"));
        Assert.Equal("true", ReadProjectProperty(project, "EmbedUntrackedSources"));
        Assert.Equal("true", ReadProjectProperty(project, "IncludeSymbols"));
        Assert.Equal("snupkg", ReadProjectProperty(project, "SymbolPackageFormat"));
        Assert.Equal("true", ReadProjectProperty(project, "EnablePackageValidation"));
        Assert.Contains(project.Descendants("PackageReference"), packageReference =>
            packageReference.Attribute("Include")?.Value == "Microsoft.SourceLink.GitHub" &&
            packageReference.Attribute("PrivateAssets")?.Value == "All");

        using ZipArchive package = ZipFile.OpenRead(packagePath);
        Assert.Contains(package.Entries, entry => entry.FullName == "README.md");
        Assert.Contains(package.Entries, entry => entry.FullName == "icon.png");
        Assert.Contains(package.Entries, entry => entry.FullName == "lib/net10.0/BlazorTUI.dll");
        Assert.Contains(package.Entries, entry => entry.FullName == "staticwebassets/blazorTui.js");
        Assert.DoesNotContain(package.Entries, entry => entry.FullName.EndsWith("exampleJsInterop.js", StringComparison.Ordinal));
        string keyboardScript = ReadEntry(package, "staticwebassets/blazorTui.js");
        Assert.Contains("event.preventDefault()", keyboardScript);
        Assert.Contains("event.ctrlKey || event.metaKey", keyboardScript);
        Assert.Contains("pointerdown", keyboardScript);
        Assert.Contains("element.focus({ preventScroll: true })", keyboardScript);
        Assert.Contains("navigator.clipboard.readText()", keyboardScript);
        Assert.Contains("BlazorTUICopySelection", keyboardScript);
        Assert.Contains("BlazorTUIPaste", keyboardScript);
        Assert.Contains("BlazorTUIUndo", keyboardScript);
        Assert.Contains("BlazorTUIRedo", keyboardScript);
        Assert.Contains("BlazorTUIHandleShortcut", keyboardScript);
        Assert.Contains("BlazorTUIToggleCommandPalette", keyboardScript);
        Assert.Contains("clipboardCopyEnabled", keyboardScript);
        Assert.Contains("clipboardPasteEnabled", keyboardScript);
        Assert.Contains("commandPaletteEnabled", keyboardScript);
        Assert.Contains("BlazorTUIMoveTab", keyboardScript);
        Assert.Contains("JSON.parse(element.dataset.shortcuts", keyboardScript);
        Assert.Contains("findShortcut(element, event)", keyboardScript);
        Assert.Contains("shouldHandleShortcutInJavaScript", keyboardScript);
        Assert.Contains("event.repeat", keyboardScript);
        Assert.Contains("ToggleCommandPalette", keyboardScript);
        Assert.Contains("SelectNextTab", keyboardScript);
        Assert.Contains("SelectPreviousTab", keyboardScript);
        Assert.Contains("\"F4\"", keyboardScript);
        Assert.Contains("\"F10\"", keyboardScript);
        Assert.Contains("\"ContextMenu\"", keyboardScript);
        string normalizedKeyboardScript = keyboardScript.Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Contains("\"End\",\n    \"PageUp\",\n    \"PageDown\"", normalizedKeyboardScript);

        ZipArchiveEntry scopedCssEntry = package.Entries.Single(entry =>
            entry.FullName.StartsWith("staticwebassets/BlazorTUI.", StringComparison.Ordinal) &&
            entry.FullName.EndsWith(".bundle.scp.css", StringComparison.Ordinal));
        string scopedCss = ReadEntry(scopedCssEntry);
        Assert.Contains("grid-template-columns: repeat(var(--tui-columns)", scopedCss);
        Assert.Contains(".gridfs:focus-visible", scopedCss);
        Assert.DoesNotContain("sizefs-", scopedCss);

        string nuspec = ReadEntry(package, "BlazorTUI.nuspec");
        XDocument manifest = XDocument.Parse(nuspec);
        XElement metadata = manifest.Descendants().Single(element => element.Name.LocalName == "metadata");
        Assert.Equal(version, metadata.Elements().Single(element => element.Name.LocalName == "version").Value);
        XElement repository = metadata.Elements().Single(element => element.Name.LocalName == "repository");
        Assert.Equal("git", repository.Attribute("type")?.Value);
        Assert.Equal("https://github.com/alexandrelozano/BlazorTUI", repository.Attribute("url")?.Value);
        Assert.DoesNotContain("System.Drawing.Common", nuspec, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.SourceLink.GitHub", nuspec, StringComparison.OrdinalIgnoreCase);

        using ZipArchive symbolsPackage = ZipFile.OpenRead(symbolsPackagePath);
        Assert.Contains(symbolsPackage.Entries, entry => entry.FullName == "lib/net10.0/BlazorTUI.pdb");

        string packedReadme = ReadEntry(package, "README.md");
        Assert.Equal(File.ReadAllText(Path.Combine(root, "README.md")), packedReadme);
    }

    [Fact]
    public void GeneratedAnalyzerPackageContainsExpectedMetadataAndAssets()
    {
        string root = FindRepositoryRoot();
        string projectPath = Path.Combine(root, "BlazorTUI.Analyzers", "BlazorTUI.Analyzers.csproj");
        XDocument project = XDocument.Load(projectPath);
        string version = project.Descendants("Version").Single().Value;
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        string packagePath = Path.Combine(root, "BlazorTUI.Analyzers", "bin", configuration, $"BlazorTUI.Analyzers.{version}.nupkg");
        Assert.True(File.Exists(packagePath), $"Analyzer package was not found at {packagePath}.");

        Assert.Equal("true", ReadProjectProperty(project, "GeneratePackageOnBuild"));
        Assert.Equal("false", ReadProjectProperty(project, "IncludeBuildOutput"));
        Assert.Equal("true", ReadProjectProperty(project, "SuppressDependenciesWhenPacking"));
        Assert.Equal("true", ReadProjectProperty(project, "DevelopmentDependency"));
        Assert.Equal("true", ReadProjectProperty(project, "EnforceExtendedAnalyzerRules"));

        using ZipArchive package = ZipFile.OpenRead(packagePath);
        Assert.Contains(package.Entries, entry => entry.FullName == "README.md");
        Assert.Contains(package.Entries, entry => entry.FullName == "analyzers/dotnet/cs/BlazorTUI.Analyzers.dll");
        Assert.DoesNotContain(package.Entries, entry => entry.FullName.StartsWith("lib/", StringComparison.Ordinal));

        string nuspec = ReadEntry(package, "BlazorTUI.Analyzers.nuspec");
        XDocument manifest = XDocument.Parse(nuspec);
        XElement metadata = manifest.Descendants().Single(element => element.Name.LocalName == "metadata");
        Assert.Equal(version, metadata.Elements().Single(element => element.Name.LocalName == "version").Value);
        Assert.Equal("true", metadata.Elements().Single(element => element.Name.LocalName == "developmentDependency").Value);
        XElement repository = metadata.Elements().Single(element => element.Name.LocalName == "repository");
        Assert.Equal("git", repository.Attribute("type")?.Value);
        Assert.Equal("https://github.com/alexandrelozano/BlazorTUI", repository.Attribute("url")?.Value);
        Assert.DoesNotContain("Microsoft.CodeAnalysis.CSharp", nuspec, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseWorkflowVerifiesVersionGeneratesNotesAndPublishesArtifacts()
    {
        string root = FindRepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));

        Assert.Contains("tags:", workflow);
        Assert.Contains("\"v*.*.*\"", workflow);
        Assert.Contains("BlazorTUI/BlazorTUI.csproj", workflow);
        Assert.Contains("BlazorTUI.Analyzers/BlazorTUI.Analyzers.csproj", workflow);
        Assert.Contains("Analyzer package version", workflow);
        Assert.Contains("GITHUB_REF_NAME", workflow);
        Assert.Contains("README.md", workflow);
        Assert.Contains("release-notes.md", workflow);
        Assert.Contains("BlazorTUI.Analyzers/bin/Release/*.nupkg", workflow);
        Assert.Contains("*.nupkg", workflow);
        Assert.Contains("*.snupkg", workflow);
        Assert.Contains("gh release create", workflow);
        Assert.Contains("dotnet nuget push", workflow);
        Assert.Contains("Publish analyzer to NuGet", workflow);
        Assert.Contains("Publish symbols to NuGet", workflow);
        Assert.Contains("NUGET_API_KEY", workflow);
    }

    [Fact]
    public void WorkflowsUseNode24CompatibleGitHubActions()
    {
        string root = FindRepositoryRoot();
        string workflowsPath = Path.Combine(root, ".github", "workflows");
        string[] workflowFiles = Directory.GetFiles(workflowsPath, "*.yml");

        Assert.NotEmpty(workflowFiles);
        foreach (string workflowFile in workflowFiles)
        {
            string workflow = File.ReadAllText(workflowFile);

            Assert.DoesNotContain("actions/checkout@v4", workflow);
            Assert.DoesNotContain("actions/setup-dotnet@v4", workflow);
            Assert.DoesNotContain("actions/upload-artifact@v4", workflow);
        }

        string combinedWorkflows = string.Join(Environment.NewLine, workflowFiles.Select(File.ReadAllText));
        Assert.Contains("actions/checkout@v5", combinedWorkflows);
        Assert.Contains("actions/setup-dotnet@v5", combinedWorkflows);
        Assert.Contains("actions/upload-artifact@v6", combinedWorkflows);
    }

    private static string ReadEntry(ZipArchive package, string name)
    {
        ZipArchiveEntry entry = package.GetEntry(name) ?? throw new InvalidOperationException($"Missing package entry {name}.");
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    private static string ReadEntry(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    private static string ReadProjectProperty(XDocument project, string propertyName)
        => project
            .Descendants(propertyName)
            .Select(element => element.Value)
            .Single();

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "BlazorTUI.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
