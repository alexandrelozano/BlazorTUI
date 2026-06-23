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
        Assert.True(File.Exists(packagePath), $"Package was not found at {packagePath}.");

        using ZipArchive package = ZipFile.OpenRead(packagePath);
        Assert.Contains(package.Entries, entry => entry.FullName == "README.md");
        Assert.Contains(package.Entries, entry => entry.FullName == "icon.png");
        Assert.Contains(package.Entries, entry => entry.FullName == "lib/net10.0/BlazorTUI.dll");

        string nuspec = ReadEntry(package, "BlazorTUI.nuspec");
        XDocument manifest = XDocument.Parse(nuspec);
        XElement metadata = manifest.Descendants().Single(element => element.Name.LocalName == "metadata");
        Assert.Equal(version, metadata.Elements().Single(element => element.Name.LocalName == "version").Value);
        Assert.DoesNotContain("System.Drawing.Common", nuspec, StringComparison.OrdinalIgnoreCase);

        string packedReadme = ReadEntry(package, "README.md");
        Assert.Equal(File.ReadAllText(Path.Combine(root, "README.md")), packedReadme);
    }

    private static string ReadEntry(ZipArchive package, string name)
    {
        ZipArchiveEntry entry = package.GetEntry(name) ?? throw new InvalidOperationException($"Missing package entry {name}.");
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

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
