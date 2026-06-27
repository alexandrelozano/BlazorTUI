using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class TabControl : Container
    {
        private readonly List<TabPage> tabs = new();
        private int selectedIndex = -1;

        public IReadOnlyList<TabPage> Tabs => tabs;

        public int SelectedIndex
        {
            get => selectedIndex;
            set => SelectTab(value);
        }

        public TabPage? SelectedTab
            => selectedIndex >= 0 && selectedIndex < tabs.Count ? tabs[selectedIndex] : null;

        public Color ForeColor { get; set; }

        public Color BackgroundColor { get; set; }

        public bool IsKeyboardActive { get; private set; }

        public event EventHandler? SelectedTabChanged;

        public TabControl(
            string name,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
            : base(name)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)4);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)4);

            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
        }

        public TabPage AddTab(string name, string title)
        {
            var tab = new TabPage(name, title);
            AddTab(tab);
            return tab;
        }

        public void AddTab(TabPage tab)
        {
            ArgumentNullException.ThrowIfNull(tab);
            if (tab.Parent is not null)
                throw new InvalidOperationException("The tab already belongs to a container.");
            if (tabs.Any(existing => string.Equals(existing.Name, tab.Name, StringComparison.Ordinal)))
                throw new InvalidOperationException($"A tab named '{tab.Name}' already exists.");
            foreach (Control control in EnumerateControls(tab))
            {
                if (TopContainer().GetControl(control.Name) is not null)
                    throw new InvalidOperationException($"A control named '{control.Name}' already exists.");
            }

            tab.X = 1;
            tab.Y = 2;
            tab.Width = (short)(Width - 2);
            tab.Height = (short)(Height - 3);
            tab.Visible = tabs.Count == 0;
            tabs.Add(tab);
            base.AddContainer(tab);

            if (selectedIndex < 0)
                selectedIndex = 0;
        }

        public void SelectTab(int index)
            => ActivateTab(index, true);

        public void SelectNextTab()
        {
            if (tabs.Count == 0)
                return;

            ActivateTab((selectedIndex + 1) % tabs.Count, true);
        }

        public void SelectPreviousTab()
        {
            if (tabs.Count == 0)
                return;

            ActivateTab((selectedIndex - 1 + tabs.Count) % tabs.Count, true);
        }

        public override void SetFocus(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            int tabIndex = tabs.FindIndex(tab => tab.GetControl(name) is not null);
            IsKeyboardActive = tabIndex >= 0;
            if (tabIndex >= 0 && tabIndex != selectedIndex)
                ActivateTab(tabIndex, false);

            base.SetFocus(name);
        }

        public override Control? GetCurrentFocusControl()
            => SelectedTab?.GetCurrentFocusControl();

        public override void Click(short X, short Y)
        {
            if (!Visible)
                return;

            if (Y == 0)
            {
                int tabIndex = GetTabIndexAt(X);
                if (tabIndex >= 0)
                    ActivateTab(tabIndex, true);
                return;
            }

            TabPage? selectedTab = SelectedTab;
            if (selectedTab is not null &&
                X >= selectedTab.X && X < selectedTab.X + selectedTab.Width &&
                Y >= selectedTab.Y && Y < selectedTab.Y + selectedTab.Height)
            {
                selectedTab.Click((short)(X - selectedTab.X), (short)(Y - selectedTab.Y));
            }
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            UpdateTabLayout();
            int originX = XOffset();
            int originY = YOffset();
            ClearAndDrawBorder(rows, originX, originY);
            DrawHeaders(rows, originX, originY);
            base.Render(rows);
        }

        private void ActivateTab(int index, bool focusFirstControl)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, tabs.Count);
            ActivateKeyboardContext();
            if (index == selectedIndex)
                return;

            TabPage? previousTab = SelectedTab;
            previousTab?.SetFocus($"{Name}-no-focused-control");

            selectedIndex = index;
            UpdateTabVisibility();

            if (focusFirstControl && FindFirstFocusableControl(tabs[index]) is { } firstControl)
                TopContainer().SetFocus(firstControl.Name);

            SelectedTabChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ActivateKeyboardContext()
        {
            foreach (TabControl tabControl in EnumerateTabControls(TopContainer()))
                tabControl.IsKeyboardActive = false;

            IsKeyboardActive = true;
        }

        private void UpdateTabLayout()
        {
            foreach (TabPage tab in tabs)
            {
                tab.X = 1;
                tab.Y = 2;
                tab.Width = (short)(Width - 2);
                tab.Height = (short)(Height - 3);
            }

            UpdateTabVisibility();
        }

        private void UpdateTabVisibility()
        {
            for (int index = 0; index < tabs.Count; index++)
                tabs[index].Visible = index == selectedIndex;
        }

        private int GetTabIndexAt(short x)
        {
            int headerX = 1;
            for (int index = 0; index < tabs.Count; index++)
            {
                int headerWidth = TuiText.VisualWidth(tabs[index].Title) + 2;
                if (x >= headerX && x < headerX + headerWidth)
                    return index;

                headerX += headerWidth + 1;
            }

            return -1;
        }

        private void ClearAndDrawBorder(IList<Row> rows, int originX, int originY)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, originX + x, originY + y, out Cell cell))
                        continue;

                    cell.ForeColor = ForeColor;
                    cell.BackgroundColor = BackgroundColor;
                    cell.Character = " ";
                    cell.Decoration = Cell.TextDecoration.None;
                    cell.BackgroundImage = "";
                    cell.ScaleX = 1;
                    cell.ScaleY = 1;

                    if (y == 1)
                        cell.Character = x switch { 0 => "┌", _ when x == Width - 1 => "┐", _ => "─" };
                    else if (y == Height - 1)
                        cell.Character = x switch { 0 => "└", _ when x == Width - 1 => "┘", _ => "─" };
                    else if (y > 1 && (x == 0 || x == Width - 1))
                        cell.Character = "│";
                }
            }
        }

        private void DrawHeaders(IList<Row> rows, int originX, int originY)
        {
            int headerX = 1;
            for (int index = 0; index < tabs.Count && headerX < Width - 1; index++)
            {
                string header = $"[{tabs[index].Title}]";
                int headerWidth = TuiText.VisualWidth(header);
                for (int characterIndex = 0; characterIndex < headerWidth && headerX + characterIndex < Width - 1; characterIndex++)
                {
                    if (!TryGetCell(rows, originX + headerX + characterIndex, originY, out Cell cell))
                        continue;

                    bool selected = index == selectedIndex;
                    cell.ForeColor = selected ? BackgroundColor : ForeColor;
                    cell.BackgroundColor = selected ? ForeColor : BackgroundColor;
                    cell.Character = TuiText.CellAt(header, characterIndex);
                }

                headerX += headerWidth + 1;
            }
        }

        private bool TryGetCell(IList<Row> rows, int x, int y, out Cell cell)
        {
            int minimumX = parent?.XOffset() ?? 0;
            int minimumY = parent?.YOffset() ?? 0;
            int maximumX = parent is null ? int.MaxValue : minimumX + parent.Width;
            int maximumY = parent is null ? int.MaxValue : minimumY + parent.Height;

            if (y < minimumY || y >= maximumY || y < 0 || y >= rows.Count ||
                x < minimumX || x >= maximumX || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private static Control? FindFirstFocusableControl(Container container)
        {
            Control? directControl = container.Controls
                .Where(control => control.Visible && control.TabStop)
                .OrderBy(control => control.TabIndex)
                .ThenBy(control => control.Name)
                .FirstOrDefault();
            if (directControl is not null)
                return directControl;

            foreach (Container child in container.Containers.Where(child => child.Visible).OrderBy(child => child.ZOrder))
            {
                if (FindFirstFocusableControl(child) is { } nestedControl)
                    return nestedControl;
            }

            return null;
        }

        private static IEnumerable<Control> EnumerateControls(Container container)
        {
            foreach (Control control in container.Controls)
                yield return control;

            foreach (Container child in container.Containers)
            {
                foreach (Control control in EnumerateControls(child))
                    yield return control;
            }
        }

        private static IEnumerable<TabControl> EnumerateTabControls(Container container)
        {
            if (container is TabControl tabControl)
                yield return tabControl;

            foreach (Container child in container.Containers)
            {
                foreach (TabControl nestedTabControl in EnumerateTabControls(child))
                    yield return nestedTabControl;
            }
        }
    }
}
