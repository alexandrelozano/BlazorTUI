using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Benchmarks;

internal static class BenchmarkDataFactory
{
    public static Screen CreateShowcaseScreen(int gridRowCount = 1_000)
    {
        var screen = new Screen(120, 60);
        var frame = new Frame(
            "rootFrame",
            "BENCHMARK",
            1, 1, 116, 56,
            Frame.BorderStyle.Line,
            Color.Yellow,
            Color.DarkBlue);
        screen.TopContainer.AddContainer(frame);

        frame.AddControl(new Label(
            "headline",
            "Rendering benchmark with controls, layout, grid and tree",
            2, 2, 62,
            Color.White,
            Color.DarkBlue));
        frame.AddControl(new TextBox(
            "name",
            "Alexandre",
            2, 4, 18,
            Color.White,
            Color.Black));
        frame.AddControl(new TextArea(
            "notes",
            "First line\nSecond line with emoji 🙂 and wide 漢 text",
            2, 6, 42, 5, 120, 12,
            Color.White,
            Color.Black));
        frame.AddControl(new Slider(
            "volume",
            0, 100, 50, 5,
            2, 12, 30,
            Color.White,
            Color.Black));

        var status = new StatusBar(
            "status",
            "Ready",
            2, 52, 104,
            Color.Black,
            Color.Cyan);
        status.AddItem("help", "F1 Help");
        frame.AddControl(status);

        frame.AddControl(CreateMaterializedGridView(
            "orders",
            48, 4, 58, 18,
            gridRowCount,
            pageSize: 14));

        TreeView tree = CreateMaterializedTreeView(
            "navigation",
            2, 20, 40, 16,
            rootCount: 24,
            childCount: 8);
        tree.ExpandAll();
        frame.AddControl(tree);

        return screen;
    }

    public static GridView CreateMaterializedGridView(
        string name,
        short x,
        short y,
        short width,
        short height,
        int rowCount,
        int pageSize)
        => new(
            name,
            CreateGridColumns(),
            Enumerable.Range(0, rowCount).Select(CreateGridRow).ToArray(),
            x, y, width, height,
            Color.Yellow,
            Color.Black,
            pageSize);

    public static GridView CreateVirtualGridView(
        string name,
        short x,
        short y,
        short width,
        short height,
        int rowCount,
        int pageSize)
        => new(
            name,
            CreateGridColumns(),
            new VirtualGridViewDataProvider(
                rowCount,
                CreateGridRow,
                index => $"order-{index:000000}"),
            x, y, width, height,
            Color.Yellow,
            Color.Black,
            pageSize);

    public static TreeView CreateMaterializedTreeView(
        string name,
        short x,
        short y,
        short width,
        short height,
        int rootCount,
        int childCount)
    {
        var tree = new TreeView(name, x, y, width, height, Color.Yellow, Color.Black);
        for (int rootIndex = 0; rootIndex < rootCount; rootIndex++)
        {
            TreeNode root = tree.AddNode($"root-{rootIndex}", $"Project {rootIndex:000}", isExpanded: true);
            for (int childIndex = 0; childIndex < childCount; childIndex++)
                root.AddNode($"root-{rootIndex}-child-{childIndex}", $"Order {rootIndex:000}.{childIndex:00}");
        }

        return tree;
    }

    public static TreeView CreateVirtualTreeView(
        string name,
        short x,
        short y,
        short width,
        short height,
        int visibleCount)
    {
        var provider = new VirtualTreeViewDataProvider(
            () => visibleCount,
            index => new VirtualTreeViewNode(
                $"node-{index:000000}",
                $"Virtual node {index:000000}",
                depth: index % 5,
                hasChildren: false),
            key => key.StartsWith("node-", StringComparison.Ordinal) &&
                int.TryParse(key.AsSpan(5), out int parsed)
                    ? parsed
                    : -1);

        return new TreeView(name, provider, x, y, width, height, Color.Yellow, Color.Black);
    }

    public static Screen CreateStateScreen(int textControlCount = 160, int gridRowCount = 2_000)
    {
        var screen = new Screen(140, 70);
        var frame = new Frame(
            "stateFrame",
            "STATE",
            1, 1, 136, 66,
            Frame.BorderStyle.Line,
            Color.Yellow,
            Color.DarkBlue);
        screen.TopContainer.AddContainer(frame);

        for (int index = 0; index < textControlCount; index++)
        {
            short x = (short)(2 + (index % 5) * 26);
            short y = (short)(2 + (index / 5) % 42);
            frame.AddControl(new TextBox(
                $"text-{index:000}",
                $"Value {index:000} 🙂 漢",
                x, y, 20,
                Color.White,
                Color.Black));
        }

        frame.AddControl(CreateMaterializedGridView(
            "stateGrid",
            2, 46, 78, 12,
            gridRowCount,
            pageSize: 9));

        TreeView tree = CreateMaterializedTreeView(
            "stateTree",
            84, 46, 40, 12,
            rootCount: 40,
            childCount: 6);
        tree.ExpandAll();
        frame.AddControl(tree);

        screen.SetFocus("text-000");
        return screen;
    }

    public static GridView.GridColumn[] CreateGridColumns()
        =>
        [
            new() { Title = "Order", Width = 8 },
            new() { Title = "Customer", Width = 16 },
            new() { Title = "Status", Width = 12 },
            new() { Title = "Total", Width = 8 }
        ];

    public static GridView.GridRow CreateGridRow(int index)
        => new()
        {
            Cells =
            [
                index.ToString("000000"),
                $"Customer {index % 500:000}",
                index % 3 == 0 ? "Cooking" : index % 3 == 1 ? "Delivering" : "Queued",
                (10 + index % 90).ToString("000")
            ]
        };
}
