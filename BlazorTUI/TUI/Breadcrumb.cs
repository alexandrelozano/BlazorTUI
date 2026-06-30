using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Breadcrumb : Control
    {
        private readonly List<BreadcrumbItem> items;
        private string separator = " / ";
        private string overflowText = "…";
        private int selectedIndex;

        public IReadOnlyList<BreadcrumbItem> Items => items;

        public int SelectedIndex
        {
            get => selectedIndex;
            set => SelectIndex(value);
        }

        public BreadcrumbItem? SelectedItem
            => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;

        public string? SelectedValue => SelectedItem?.Value;

        public string Separator
        {
            get => separator;
            set => separator = ValidateText(value);
        }

        public string OverflowText
        {
            get => overflowText;
            set => overflowText = ValidateText(value);
        }

        public event EventHandler? SelectedIndexChanged;

        public event EventHandler<BreadcrumbSelectionChangedEventArgs>? SelectionChanged;

        public event EventHandler<BreadcrumbItemActivatedEventArgs>? ItemActivated;

        public override string GetAccessibilitySummary()
        {
            string selected = SelectedItem is null
                ? "no item selected"
                : $"selected item {SelectedItem.Text}";
            return FormatAccessibilitySummary($"Breadcrumb {Name}: {selected}, {items.Count} levels.");
        }

        public Breadcrumb(
            string name,
            IEnumerable<string> items,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            int selectedIndex = -1)
            : this(
                name,
                items.Select((text, index) => new BreadcrumbItem($"item{index}", text)),
                X,
                Y,
                width,
                foreColor,
                backgroundColor,
                selectedIndex)
        {
        }

        public Breadcrumb(
            string name,
            IEnumerable<BreadcrumbItem> items,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            int selectedIndex = -1)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)1);

            this.items = items.Select(CloneItem).ToList();
            ValidateUniqueItemNames(this.items);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
            this.selectedIndex = NormalizeInitialSelectedIndex(selectedIndex, this.items.Count);
        }

        public BreadcrumbItem AddItem(string name, string text, string? value = null)
        {
            var item = new BreadcrumbItem(name, text, value);
            AddItem(item);
            return item;
        }

        public void AddItem(BreadcrumbItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (items.Any(existing => existing.Name == item.Name))
                throw new InvalidOperationException($"A breadcrumb item named '{item.Name}' already exists.");

            items.Add(CloneItem(item));
            if (selectedIndex < 0)
                SetSelectedIndex(items.Count - 1);
        }

        public bool RemoveItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            int index = items.FindIndex(item => item.Name == name);
            return index >= 0 && RemoveItemAt(index);
        }

        public bool RemoveItem(BreadcrumbItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return RemoveItem(item.Name);
        }

        public void ClearItems()
        {
            if (items.Count == 0)
                return;

            int previousSelectedIndex = selectedIndex;
            BreadcrumbItem? previousSelectedItem = SelectedItem;
            items.Clear();
            selectedIndex = -1;
            RaiseSelectionChanged(previousSelectedIndex, previousSelectedItem);
        }

        public BreadcrumbItem? GetItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return items.FirstOrDefault(item => item.Name == name);
        }

        public void SelectIndex(int index)
        {
            ValidateSelectedIndex(index);
            SetSelectedIndex(index);
        }

        public void SelectItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            int index = items.FindIndex(item => item.Name == name);
            if (index < 0)
                throw new ArgumentException("The item does not belong to this Breadcrumb.", nameof(name));

            SetSelectedIndex(index);
        }

        public void SelectItem(BreadcrumbItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            SelectItem(item.Name);
        }

        public void SelectValue(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            int index = items.FindIndex(item => item.Value == value);
            if (index < 0)
                throw new ArgumentException("The value does not belong to this Breadcrumb.", nameof(value));

            SetSelectedIndex(index);
        }

        public bool ActivateSelectedItem()
        {
            if (SelectedItem is null)
                return false;

            ItemActivated?.Invoke(this, new BreadcrumbItemActivatedEventArgs(selectedIndex, SelectedItem));
            return true;
        }

        public bool ActivateItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            int index = items.FindIndex(item => item.Name == name);
            if (index < 0)
                return false;

            SetSelectedIndex(index);
            ItemActivated?.Invoke(this, new BreadcrumbItemActivatedEventArgs(index, items[index]));
            return true;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            if (!Visible)
                return false;

            switch (key)
            {
                case "ArrowLeft":
                    MoveSelection(-1);
                    return items.Count > 0;
                case "ArrowRight":
                    MoveSelection(1);
                    return items.Count > 0;
                case "Home":
                    MoveSelectionTo(0);
                    return items.Count > 0;
                case "End":
                    MoveSelectionTo(items.Count - 1);
                    return items.Count > 0;
                case "Enter":
                case "Space":
                case " ":
                    bool activated = ActivateSelectedItem();
                    if (activated)
                        NotifyClicked();
                    return activated;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width || Y != 0)
                return false;

            int itemIndex = GetItemIndexAt(X);
            if (itemIndex < 0)
                return false;

            container.TopContainer().SetFocus(Name);
            SetSelectedIndex(itemIndex);
            ItemActivated?.Invoke(this, new BreadcrumbItemActivatedEventArgs(itemIndex, items[itemIndex]));
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            Clear(rows);

            IReadOnlyList<RenderedBreadcrumbCell> renderedCells = GetRenderedCells();
            for (int x = 0; x < renderedCells.Count && x < Width; x++)
            {
                RenderedBreadcrumbCell renderedCell = renderedCells[x];
                if (!TryGetCell(rows, x, 0, out Cell cell))
                    continue;

                bool selected = renderedCell.ItemIndex == selectedIndex;
                PrepareCell(
                    cell,
                    selected && Focus ? BackgroundColor : ForeColor,
                    selected && Focus ? ForeColor : BackgroundColor);
                cell.Decoration = selected && !Focus
                    ? Cell.TextDecoration.UnderLine
                    : Cell.TextDecoration.None;
                cell.Character = renderedCell.Character;
            }
        }

        private bool RemoveItemAt(int index)
        {
            int previousSelectedIndex = selectedIndex;
            BreadcrumbItem? previousSelectedItem = SelectedItem;

            items.RemoveAt(index);
            if (items.Count == 0)
                selectedIndex = -1;
            else if (index < selectedIndex)
                selectedIndex--;
            else if (selectedIndex >= items.Count)
                selectedIndex = items.Count - 1;

            if (previousSelectedIndex != selectedIndex || !ReferenceEquals(previousSelectedItem, SelectedItem))
                RaiseSelectionChanged(previousSelectedIndex, previousSelectedItem);

            return true;
        }

        private void MoveSelection(int direction)
        {
            if (items.Count == 0)
                return;

            int currentIndex = selectedIndex < 0 ? 0 : selectedIndex;
            SetSelectedIndex(Math.Clamp(currentIndex + direction, 0, items.Count - 1));
        }

        private void MoveSelectionTo(int index)
        {
            if (items.Count == 0)
                return;

            SetSelectedIndex(index);
        }

        private void SetSelectedIndex(int index)
        {
            if (selectedIndex == index)
                return;

            int previousSelectedIndex = selectedIndex;
            BreadcrumbItem? previousSelectedItem = SelectedItem;
            selectedIndex = index;
            RaiseSelectionChanged(previousSelectedIndex, previousSelectedItem);
        }

        private void RaiseSelectionChanged(int previousSelectedIndex, BreadcrumbItem? previousSelectedItem)
        {
            if (TuiEventScope.EventsSuppressed)
                return;

            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            SelectionChanged?.Invoke(
                this,
                new BreadcrumbSelectionChangedEventArgs(
                    previousSelectedIndex,
                    selectedIndex,
                    previousSelectedItem,
                    SelectedItem));
        }

        private int GetItemIndexAt(int localX)
        {
            IReadOnlyList<RenderedBreadcrumbCell> renderedCells = GetRenderedCells();
            return localX >= 0 && localX < renderedCells.Count
                ? renderedCells[localX].ItemIndex ?? -1
                : -1;
        }

        private IReadOnlyList<RenderedBreadcrumbCell> GetRenderedCells()
        {
            List<RenderedBreadcrumbCell> allCells = BuildAllCells();
            if (allCells.Count <= Width)
                return allCells;

            string effectiveOverflowText = TuiText.VisualWidth(overflowText) >= Width
                ? TuiText.TruncateByVisualWidth(overflowText, Width)
                : overflowText;
            int tailLength = Width - TuiText.VisualWidth(effectiveOverflowText);
            var renderedCells = new List<RenderedBreadcrumbCell>(Width);
            AddText(renderedCells, effectiveOverflowText, null);

            if (tailLength > 0)
                renderedCells.AddRange(allCells.Skip(allCells.Count - tailLength));

            return renderedCells;
        }

        private List<RenderedBreadcrumbCell> BuildAllCells()
        {
            var cells = new List<RenderedBreadcrumbCell>();
            for (int index = 0; index < items.Count; index++)
            {
                if (index > 0)
                    AddText(cells, separator, null);

                AddText(cells, items[index].Text, index);
            }

            return cells;
        }

        private void Clear(IList<Row> rows)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!TryGetCell(rows, x, 0, out Cell cell))
                    continue;

                PrepareCell(cell, ForeColor, BackgroundColor);
            }
        }

        private bool TryGetCell(IList<Row> rows, int localX, int localY, out Cell cell)
        {
            if (container is null)
            {
                cell = null!;
                return false;
            }

            int originX = container.XOffset() + X;
            int originY = container.YOffset() + Y;
            int x = originX + localX;
            int y = originY + localY;
            int minimumX = container.XOffset();
            int minimumY = container.YOffset();
            int maximumX = minimumX + container.Width;
            int maximumY = minimumY + container.Height;

            if (x < minimumX || x >= maximumX || y < minimumY || y >= maximumY ||
                y < 0 || y >= rows.Count || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private void ValidateSelectedIndex(int index)
        {
            if (index < -1 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
        }

        private static void AddText(List<RenderedBreadcrumbCell> cells, string text, int? itemIndex)
        {
            int width = TuiText.VisualWidth(text);
            for (int index = 0; index < width; index++)
                cells.Add(new RenderedBreadcrumbCell(TuiText.CellAt(text, index), itemIndex));
        }

        private static BreadcrumbItem CloneItem(BreadcrumbItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return new BreadcrumbItem(item.Name, item.Text, item.Value);
        }

        private static void PrepareCell(Cell cell, Color foreColor, Color backgroundColor)
        {
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = " ";
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }

        private static void ValidateUniqueItemNames(IEnumerable<BreadcrumbItem> items)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (BreadcrumbItem item in items)
            {
                if (!names.Add(item.Name))
                    throw new InvalidOperationException($"A breadcrumb item named '{item.Name}' already exists.");
            }
        }

        private static int NormalizeInitialSelectedIndex(int selectedIndex, int itemCount)
        {
            if (itemCount == 0)
            {
                if (selectedIndex != -1 && selectedIndex != 0)
                    throw new ArgumentOutOfRangeException(nameof(selectedIndex));

                return -1;
            }

            if (selectedIndex == -1)
                return itemCount - 1;

            if (selectedIndex < 0 || selectedIndex >= itemCount)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));

            return selectedIndex;
        }

        private static string ValidateText(string? value)
        {
            string text = value ?? "";
            if (text.Contains('\r') || text.Contains('\n'))
                throw new ArgumentException("Breadcrumb text cannot contain line breaks.", nameof(value));

            return text;
        }

        private readonly record struct RenderedBreadcrumbCell(string Character, int? ItemIndex);
    }
}
