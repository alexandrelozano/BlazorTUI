using System.Drawing;
using BlazorTUI.TUI;

namespace BlazorTUI.Tests;

public class StatePersistenceTests
{
    [Fact]
    public void ExportAndRestoreStateRestoresControlValuesFocusAndSelections()
    {
        TestScreenFixture fixture = CreateFixture();

        fixture.Name.SelectAll();
        fixture.Notes.SelectAll();
        fixture.Password.IsRevealed = true;
        fixture.Check.Value = true;
        fixture.Priority.SelectItem("High");
        fixture.Contact.SelectValue("phone");
        fixture.Volume.Value = 70;
        fixture.Tree.SelectNode("childNode");
        fixture.Grid.IsReadOnly = false;
        fixture.Grid.SetExactFilter(2, "Cooking");
        fixture.Grid.SortByColumn(1);
        fixture.Grid.SelectSourceRow(2);
        fixture.Grid.SelectedColumnIndex = 1;
        fixture.Grid.Rows[2].cells[1] = "Veggie";
        fixture.Tabs.SelectedIndex = 1;
        fixture.Split.SplitterPosition = 14;
        fixture.Path.SelectValue("/docs/api");
        fixture.Status.Message = "Saved";
        fixture.Screen.SetFocus("priority");

        TuiScreenState state = fixture.Screen.ExportState();

        fixture.Name.Value = "Other";
        fixture.Notes.Value = "Changed";
        fixture.Password.Value = "changed";
        fixture.Password.IsRevealed = false;
        fixture.Check.Value = false;
        fixture.Priority.SelectItem("Low");
        fixture.Contact.SelectValue("email");
        fixture.Volume.Value = 10;
        fixture.Tree.SelectNode("rootNode");
        fixture.RootNode.IsExpanded = false;
        fixture.Grid.ClearFilters();
        fixture.Grid.ClearSort();
        fixture.Grid.Rows[2].cells[1] = "Changed";
        fixture.Grid.SelectRow(0);
        fixture.Grid.SelectedColumnIndex = 0;
        fixture.Tabs.SelectedIndex = 0;
        fixture.Split.SplitterPosition = 10;
        fixture.Path.SelectValue("/");
        fixture.Status.Message = "Dirty";
        fixture.Screen.SetFocus("name");

        fixture.Screen.RestoreState(state);

        Assert.Equal("Alex", fixture.Name.Value);
        Assert.Equal("Alex", fixture.Name.SelectedText);
        Assert.Equal("First line" + Environment.NewLine + "Second line", fixture.Notes.Value);
        Assert.Equal("secret", fixture.Password.Value);
        Assert.True(fixture.Password.IsRevealed);
        Assert.True(fixture.Check.Value);
        Assert.Equal("High", fixture.Priority.SelectedItem);
        Assert.Equal("phone", fixture.Contact.SelectedValue);
        Assert.Equal(70, fixture.Volume.Value);
        Assert.Equal("childNode", fixture.Tree.SelectedNode?.Name);
        Assert.True(fixture.RootNode.IsExpanded);
        Assert.True(fixture.Grid.HasActiveFilters);
        Assert.Equal(2, fixture.Grid.FilteredRowCount);
        Assert.Equal(GridSortDirection.Ascending, fixture.Grid.SortDirection);
        Assert.Equal(2, fixture.Grid.SelectedSourceRowIndex);
        Assert.Equal(1, fixture.Grid.SelectedColumnIndex);
        Assert.Equal("Veggie", fixture.Grid.Rows[2].Cells[1]);
        Assert.Equal(1, fixture.Tabs.SelectedIndex);
        Assert.Equal(14, fixture.Split.SplitterPosition);
        Assert.Equal("/docs/api", fixture.Path.SelectedValue);
        Assert.Equal("Saved", fixture.Status.Message);
        Assert.Equal("priority", fixture.Screen.TopContainer.GetCurrentFocusControl()?.Name);
    }

    [Fact]
    public void JsonStateRoundTripsThroughPublicApi()
    {
        TestScreenFixture fixture = CreateFixture();
        fixture.Name.Value = "Serialized";
        fixture.Priority.SelectItem("High");
        fixture.Screen.SetFocus("priority");

        string json = fixture.Screen.ExportStateJson(indented: true);

        fixture.Name.Value = "Changed";
        fixture.Priority.SelectItem("Low");
        fixture.Screen.SetFocus("name");
        fixture.Screen.RestoreStateJson(json);

        Assert.Contains("\"FocusedControlName\"", json, StringComparison.Ordinal);
        Assert.Equal("Serialized", fixture.Name.Value);
        Assert.Equal("High", fixture.Priority.SelectedItem);
        Assert.Equal("priority", fixture.Screen.TopContainer.GetCurrentFocusControl()?.Name);
    }

    [Fact]
    public void PredicateGridFiltersAreExportedAsMetadataButNotRestoredAsDelegates()
    {
        TestScreenFixture fixture = CreateFixture();
        fixture.Grid.SetColumnFilter(1, value => value.Length > 6, "Long pizzas");

        TuiScreenState state = fixture.Screen.ExportState();

        Assert.Equal("Long pizzas", state.Controls["orders"].Strings["Filter:1:Description"]);

        fixture.Grid.ClearFilters();
        fixture.Screen.RestoreState(state);

        Assert.False(fixture.Grid.HasActiveFilters);
    }

    [Fact]
    public void ScreenStateIncludesSchemaVersionAndCustomPayloadSlots()
    {
        TestScreenFixture fixture = CreateFixture();
        TuiScreenState state = fixture.Screen.ExportState();

        state.SetPayload("route", "/orders/42");
        state.SetProtectedPayload("token", "secret-token", Protect);

        string json = state.ToJson(indented: true);
        TuiScreenState restored = TuiScreenState.FromJson(json);

        Assert.Equal(TuiScreenState.CurrentSchemaVersion, state.SchemaVersion);
        Assert.Contains("\"SchemaVersion\"", json, StringComparison.Ordinal);
        Assert.True(restored.TryGetPayload("route", out string route));
        Assert.Equal("/orders/42", route);
        Assert.True(restored.TryGetProtectedPayload("token", Unprotect, out string token));
        Assert.Equal("secret-token", token);
    }

    [Fact]
    public void RestoreStateCanRestoreOnlySelectedControlsWithoutRestoringFocus()
    {
        TestScreenFixture fixture = CreateFixture();
        fixture.Name.Value = "Saved";
        fixture.Priority.SelectItem("High");
        fixture.Screen.SetFocus("priority");
        TuiScreenState state = fixture.Screen.ExportState();

        fixture.Name.Value = "Dirty";
        fixture.Priority.SelectItem("Low");
        fixture.Screen.SetFocus("name");

        fixture.Screen.RestoreState(
            state,
            new TuiStateRestoreOptions
            {
                RestoreFocus = false,
                ControlNames = new[] { "name" }
            });

        Assert.Equal("Saved", fixture.Name.Value);
        Assert.Equal("Low", fixture.Priority.SelectedItem);
        Assert.Equal("name", fixture.Screen.TopContainer.GetCurrentFocusControl()?.Name);
    }

    [Fact]
    public void RestoreStateAppliesVersionedMigrationHooksBeforeRestoring()
    {
        TestScreenFixture fixture = CreateFixture();
        fixture.Name.Value = "Migrated";
        TuiScreenState state = fixture.Screen.ExportState();
        state.SchemaVersion = 0;
        state.Controls["legacyName"] = state.Controls["name"];
        state.Controls.Remove("name");

        fixture.Name.Value = "Dirty";

        fixture.Screen.RestoreState(
            state,
            new TuiStateRestoreOptions
            {
                Migrations = new[]
                {
                    new TuiStateMigration(
                        fromVersion: 0,
                        toVersion: TuiScreenState.CurrentSchemaVersion,
                        migrate: migratedState =>
                        {
                            migratedState.Controls["name"] = migratedState.Controls["legacyName"];
                            migratedState.Controls.Remove("legacyName");
                            return migratedState;
                        })
                }
            });

        Assert.Equal("Migrated", fixture.Name.Value);
        Assert.Equal(TuiScreenState.CurrentSchemaVersion, state.SchemaVersion);
    }

    [Fact]
    public void RestoreStateCanSuppressControlEvents()
    {
        TestScreenFixture fixture = CreateFixture();
        fixture.Volume.Value = 70;
        fixture.Contact.SelectValue("phone");
        fixture.Path.SelectValue("/docs/api");
        fixture.Status.Message = "Saved";
        fixture.Screen.SetFocus("priority");
        TuiScreenState state = fixture.Screen.ExportState();

        int sliderEvents = 0;
        int radioEvents = 0;
        int breadcrumbEvents = 0;
        int statusEvents = 0;
        int focusEvents = 0;
        fixture.Volume.ValueChanged += (_, _) => sliderEvents++;
        fixture.Contact.SelectionChanged += (_, _) => radioEvents++;
        fixture.Path.SelectionChanged += (_, _) => breadcrumbEvents++;
        fixture.Status.MessageChanged += (_, _) => statusEvents++;
        fixture.Priority.GotFocus += (_, _) => focusEvents++;

        fixture.Volume.Value = 10;
        fixture.Contact.SelectValue("email");
        fixture.Path.SelectValue("/");
        fixture.Status.Message = "Dirty";
        fixture.Screen.SetFocus("name");

        sliderEvents = 0;
        radioEvents = 0;
        breadcrumbEvents = 0;
        statusEvents = 0;
        focusEvents = 0;

        fixture.Screen.RestoreState(
            state,
            new TuiStateRestoreOptions { SuppressEvents = true });

        Assert.Equal(70, fixture.Volume.Value);
        Assert.Equal("phone", fixture.Contact.SelectedValue);
        Assert.Equal("/docs/api", fixture.Path.SelectedValue);
        Assert.Equal("Saved", fixture.Status.Message);
        Assert.Equal("priority", fixture.Screen.TopContainer.GetCurrentFocusControl()?.Name);
        Assert.Equal(0, sliderEvents);
        Assert.Equal(0, radioEvents);
        Assert.Equal(0, breadcrumbEvents);
        Assert.Equal(0, statusEvents);
        Assert.Equal(0, focusEvents);
    }

    private static string Protect(string value)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    private static string Unprotect(string value)
        => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value));

    private static TestScreenFixture CreateFixture()
    {
        var screen = new Screen(80, 30);
        var frame = new Frame("frame", "STATE", 0, 0, 70, 24, Frame.BorderStyle.Line, Color.Yellow, Color.DarkBlue);
        screen.TopContainer.AddContainer(frame);

        var name = new TextBox("name", "Alex", 2, 2, 12, Color.White, Color.Black);
        var notes = new TextArea(
            "notes",
            "First line" + Environment.NewLine + "Second line",
            2,
            4,
            18,
            4,
            30,
            5,
            Color.White,
            Color.Black);
        var password = new PasswordBox("password", "secret", 2, 9, 12, Color.White, Color.Black);
        var check = new CheckBox("include", "Include", 2, 11, 12, Color.White, Color.Black);
        var priority = new ComboBox("priority", new[] { "Low", "High" }, 20, 2, 10, Color.White, Color.Black);
        var contact = new RadioGroup(
            "contact",
            new[]
            {
                new RadioGroupOption("email", "Email", "email"),
                new RadioGroupOption("phone", "Phone", "phone")
            },
            20,
            4,
            18,
            Color.White,
            Color.Black,
            selectedIndex: 0,
            RadioGroupOrientation.Horizontal);
        var volume = new Slider(
            "volume",
            0,
            100,
            20,
            10,
            20,
            6,
            12,
            Color.White,
            Color.Black);
        var tree = new TreeView("tree", 40, 2, 16, 5, Color.White, Color.Black);
        TreeNode rootNode = tree.AddNode("rootNode", "Root", true);
        rootNode.AddNode("childNode", "Child");
        var grid = new GridView(
            "orders",
            new[]
            {
                new GridView.GridColumn { Title = "Order", Width = 7 },
                new GridView.GridColumn { Title = "Pizza", Width = 10 },
                new GridView.GridColumn { Title = "Status", Width = 10 }
            },
            new[]
            {
                new GridView.GridRow { Cells = new[] { "1", "Pepperoni", "Cooking" } },
                new GridView.GridRow { Cells = new[] { "2", "Calzone", "Ready" } },
                new GridView.GridRow { Cells = new[] { "3", "Veggie", "Cooking" } }
            },
            2,
            14,
            28,
            5,
            Color.White,
            Color.Black,
            pageSize: 3);
        var tabs = new TabControl("tabs", 40, 8, 20, 7, Color.White, Color.Black);
        tabs.AddTab("general", "General").AddControl(new Label("generalLabel", "General", 1, 1, 10, Color.White, Color.Black));
        tabs.AddTab("details", "Details").AddControl(new Label("detailsLabel", "Details", 1, 1, 10, Color.White, Color.Black));
        var split = new SplitPanel("split", 2, 20, 30, 3, SplitPanelOrientation.Vertical, 10, Color.White, Color.Black);
        var path = new Breadcrumb(
            "path",
            new[]
            {
                new BreadcrumbItem("home", "Home", "/"),
                new BreadcrumbItem("docs", "Docs", "/docs"),
                new BreadcrumbItem("api", "API", "/docs/api")
            },
            34,
            20,
            24,
            Color.White,
            Color.Black);
        var status = new StatusBar("status", "Ready", 2, 22, 40, Color.Black, Color.Cyan);

        frame.AddControl(name);
        frame.AddControl(notes);
        frame.AddControl(password);
        frame.AddControl(check);
        frame.AddControl(priority);
        frame.AddControl(contact);
        frame.AddControl(volume);
        frame.AddControl(tree);
        frame.AddControl(grid);
        frame.AddContainer(tabs);
        frame.AddContainer(split);
        frame.AddControl(path);
        frame.AddControl(status);

        return new TestScreenFixture(
            screen,
            name,
            notes,
            password,
            check,
            priority,
            contact,
            volume,
            tree,
            rootNode,
            grid,
            tabs,
            split,
            path,
            status);
    }

    private sealed record TestScreenFixture(
        Screen Screen,
        TextBox Name,
        TextArea Notes,
        PasswordBox Password,
        CheckBox Check,
        ComboBox Priority,
        RadioGroup Contact,
        Slider Volume,
        TreeView Tree,
        TreeNode RootNode,
        GridView Grid,
        TabControl Tabs,
        SplitPanel Split,
        Breadcrumb Path,
        StatusBar Status);
}
