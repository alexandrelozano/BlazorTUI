using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class TreeViewTests
{
    [Fact]
    public void RendersExpandedHierarchyAndHidesCollapsedDescendants()
    {
        var screen = new Screen(30, 10);
        var tree = new TreeView("projectTree", 1, 1, 22, 6, Color.Yellow, Color.Black);
        screen.TopContainer.AddControl(tree);
        TreeNode workspace = tree.AddNode("workspace", "Workspace", true);
        workspace.AddNode("program", "Program.cs");
        TreeNode dependencies = tree.AddNode("dependencies", "Dependencies");
        dependencies.AddNode("nuget", "NuGet");

        screen.Render();

        Assert.StartsWith("▼ Workspace", Read(screen, 1, 1, 21));
        Assert.StartsWith("  • Program.cs", Read(screen, 1, 2, 21));
        Assert.StartsWith("▶ Dependencies", Read(screen, 1, 3, 21));
        string visibleText = string.Join(
            Environment.NewLine,
            screen.Rows.Skip(1).Take(4).Select(row =>
                string.Concat(row.Cells.Select(cell => cell.Character))));
        Assert.DoesNotContain("NuGet", visibleText);

        dependencies.IsExpanded = true;
        screen.Render();

        Assert.StartsWith("  • NuGet", Read(screen, 1, 4, 21));
    }

    [Fact]
    public void KeyboardNavigationExpandsCollapsesAndActivatesNodes()
    {
        var container = new Container("root") { Width = 30, Height = 10 };
        var tree = new TreeView("projectTree", 0, 0, 22, 6, Color.White, Color.Black);
        container.AddControl(tree);
        TreeNode root = tree.AddNode("rootNode", "Root", true);
        TreeNode source = root.AddNode("source", "Source");
        TreeNode program = source.AddNode("program", "Program.cs");
        root.AddNode("readme", "README.md");
        int selectionChanges = 0;
        int expansions = 0;
        int collapses = 0;
        int activations = 0;
        tree.SelectedNodeChanged += (_, _) => selectionChanges++;
        tree.NodeExpanded += (_, _) => expansions++;
        tree.NodeCollapsed += (_, _) => collapses++;
        tree.NodeActivated += (_, _) => activations++;

        tree.KeyDown("ArrowDown", false);
        Assert.Same(source, tree.SelectedNode);

        tree.KeyDown("ArrowRight", false);
        Assert.True(source.IsExpanded);
        tree.KeyDown("ArrowRight", false);
        Assert.Same(program, tree.SelectedNode);

        tree.KeyDown("ArrowLeft", false);
        Assert.Same(source, tree.SelectedNode);
        tree.KeyDown("ArrowLeft", false);
        Assert.False(source.IsExpanded);

        tree.KeyDown("Enter", false);

        Assert.True(source.IsExpanded);
        Assert.Equal(2, expansions);
        Assert.Equal(1, collapses);
        Assert.Equal(1, activations);
        Assert.Equal(3, selectionChanges);

        tree.SelectNode(program);
        source.IsExpanded = false;
        Assert.Same(source, tree.SelectedNode);
    }

    [Fact]
    public void MouseClickTogglesMarkerAndSelectsVisibleNode()
    {
        var screen = new Screen(24, 8);
        var tree = new TreeView("projectTree", 1, 1, 18, 4, Color.White, Color.Black);
        screen.TopContainer.AddControl(tree);
        TreeNode root = tree.AddNode("rootNode", "Root");
        TreeNode child = root.AddNode("child", "Child");

        screen.TopContainer.Click(1, 1);
        screen.TopContainer.Click(4, 2);

        Assert.True(root.IsExpanded);
        Assert.Same(child, tree.SelectedNode);
        Assert.True(tree.Focus);
    }

    [Fact]
    public void SelectionScrollsIntoView()
    {
        var screen = new Screen(24, 6);
        var tree = new TreeView("projectTree", 1, 1, 18, 3, Color.White, Color.Black);
        screen.TopContainer.AddControl(tree);
        for (int index = 0; index < 6; index++)
            tree.AddNode($"node{index}", $"Node {index}");

        screen.TopContainer.Click(18, 3);
        screen.Render();
        Assert.StartsWith("• Node 1", Read(screen, 1, 1, 17));

        tree.SelectNode("node5");
        screen.SetFocus("projectTree");
        screen.Render();

        Assert.StartsWith("• Node 5", Read(screen, 1, 3, 17));
        Assert.Equal(Color.Black, screen.Rows[3].Cells[1].ForeColor);
        Assert.Equal(Color.White, screen.Rows[3].Cells[1].BackgroundColor);
    }

    [Fact]
    public void SupportsPrebuiltTreesMutationAndUniqueNames()
    {
        var tree = new TreeView("projectTree", 0, 0, 20, 5, Color.White, Color.Black);
        var root = new TreeNode("rootNode", "Root", true);
        TreeNode child = root.AddNode("child", "Child");
        tree.AddNode(root);
        tree.SelectNode(child);
        int changes = 0;
        tree.SelectedNodeChanged += (_, _) => changes++;

        Assert.Throws<InvalidOperationException>(() => tree.AddNode("child", "Duplicate"));
        Assert.Throws<InvalidOperationException>(() => root.AddNode("child", "Duplicate"));
        Assert.True(tree.RemoveNode(child));

        Assert.Same(root, tree.SelectedNode);
        Assert.Null(tree.GetNode("child"));
        Assert.Equal(1, changes);

        tree.ClearNodes();
        Assert.Empty(tree.Nodes);
        Assert.Null(tree.SelectedNode);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void ValidatesDimensionsTextAndNodeOwnership()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TreeView("tree", 0, 0, 3, 4, Color.White, Color.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TreeView("tree", 0, 0, 10, 0, Color.White, Color.Black));
        Assert.Throws<ArgumentException>(() => new TreeNode(" ", "Root"));
        Assert.Throws<ArgumentException>(() => new TreeNode("root", "Root\nChild"));

        var tree = new TreeView("tree", 0, 0, 10, 4, Color.White, Color.Black);
        var foreignNode = new TreeNode("foreign", "Foreign");
        Assert.Throws<ArgumentException>(() => tree.SelectNode(foreignNode));
        Assert.Throws<ArgumentException>(() => tree.SelectNode("missing"));
    }

    private static string Read(Screen screen, int x, int y, int length)
        => string.Concat(screen.Rows[y].Cells.Skip(x).Take(length).Select(cell => cell.Character));
}
