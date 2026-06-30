using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class MultiSelectComboBox : Control, IPopupControl
    {
        private readonly List<string> items;
        private readonly List<string> selectedItems = new();
        private int highlightedIndex;
        private int scrollIndex;
        private short maxDropDownItems;

        public MultiSelectComboBox(
            string name,
            IEnumerable<string> items,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            IEnumerable<string>? selectedItems = null,
            short maxDropDownItems = 5)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)6);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDropDownItems, (short)1);

            this.items = items.Select(ValidateItem).ToList();
            if (selectedItems is not null)
            {
                foreach (string item in selectedItems)
                    SelectItem(item);
            }

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
            this.maxDropDownItems = maxDropDownItems;
        }

        public IReadOnlyList<string> Items => items;

        public IReadOnlyList<string> SelectedItems => selectedItems;

        public bool IsDropDownOpen { get; private set; }

        public short MaxDropDownItems
        {
            get => maxDropDownItems;
            set
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value, (short)1);
                maxDropDownItems = value;
                EnsureHighlightedItemVisible();
            }
        }

        public event EventHandler<MultiSelectComboBoxSelectionChangedEventArgs>? SelectionChanged;

        bool IPopupControl.IsPopupOpen => IsDropDownOpen;

        public override string GetAccessibilitySummary()
        {
            string selected = selectedItems.Count == 0
                ? "no items selected"
                : $"{selectedItems.Count} selected";
            string popupState = IsDropDownOpen ? "drop-down open" : "drop-down closed";
            return FormatAccessibilitySummary($"MultiSelectComboBox {Name}: {selected}, {items.Count} options, {popupState}.");
        }

        public void OpenDropDown()
        {
            if (items.Count == 0)
                return;

            if (container is not null)
            {
                container.TopContainer().SetFocus(Name);
                container.BringToFront(this);
            }

            IsDropDownOpen = true;
            highlightedIndex = Math.Clamp(highlightedIndex, 0, items.Count - 1);
            EnsureHighlightedItemVisible();
        }

        public void CloseDropDown()
            => IsDropDownOpen = false;

        public void ToggleDropDown()
        {
            if (IsDropDownOpen)
                CloseDropDown();
            else
                OpenDropDown();
        }

        public void SelectItem(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            int index = items.IndexOf(item);
            if (index < 0)
                throw new ArgumentException("The item does not belong to this MultiSelectComboBox.", nameof(item));

            if (!selectedItems.Contains(item))
            {
                selectedItems.Add(item);
                SelectionChanged?.Invoke(this, new MultiSelectComboBoxSelectionChangedEventArgs(item, index, true));
            }
        }

        public void DeselectItem(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            int index = items.IndexOf(item);
            if (index < 0)
                throw new ArgumentException("The item does not belong to this MultiSelectComboBox.", nameof(item));

            if (selectedItems.Remove(item))
                SelectionChanged?.Invoke(this, new MultiSelectComboBoxSelectionChangedEventArgs(item, index, false));
        }

        public void ToggleItem(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (selectedItems.Contains(item))
                DeselectItem(item);
            else
                SelectItem(item);
        }

        public void ClearSelection()
        {
            foreach (string item in selectedItems.ToArray())
                DeselectItem(item);
        }

        internal void ExportMultiSelectComboBoxState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetStringList("SelectedItems", selectedItems);
            state.SetInteger("HighlightedIndex", highlightedIndex);
            state.SetInteger("ScrollIndex", scrollIndex);
            state.SetBoolean("IsDropDownOpen", IsDropDownOpen);
        }

        internal void RestoreMultiSelectComboBoxState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            selectedItems.Clear();
            if (state.TryGetStringList("SelectedItems", out IReadOnlyList<string> restoredSelectedItems))
            {
                foreach (string item in restoredSelectedItems)
                {
                    if (items.Contains(item) && !selectedItems.Contains(item))
                        selectedItems.Add(item);
                }
            }

            highlightedIndex = state.TryGetInteger("HighlightedIndex", out int restoredHighlightedIndex)
                ? Math.Clamp(restoredHighlightedIndex, 0, Math.Max(0, items.Count - 1))
                : 0;
            scrollIndex = state.TryGetInteger("ScrollIndex", out int restoredScrollIndex)
                ? Math.Max(0, restoredScrollIndex)
                : 0;
            IsDropDownOpen = state.TryGetBoolean("IsDropDownOpen", out bool restoredOpen) && restoredOpen && items.Count > 0;
            EnsureHighlightedItemVisible();
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            if (!Visible)
                return false;

            switch (key)
            {
                case "Enter":
                case "Space":
                case " ":
                    if (IsDropDownOpen)
                        ToggleHighlightedItem();
                    else
                        OpenDropDown();
                    NotifyClicked();
                    return true;
                case "Escape" when IsDropDownOpen:
                    CloseDropDown();
                    return true;
                case "F4":
                    ToggleDropDown();
                    return true;
                case "ArrowDown":
                    if (!IsDropDownOpen)
                        OpenDropDown();
                    else
                        MoveHighlight(1);
                    return true;
                case "ArrowUp":
                    if (IsDropDownOpen)
                        MoveHighlight(-1);
                    return true;
                case "Home":
                    MoveHighlightTo(0);
                    return items.Count > 0;
                case "End":
                    MoveHighlightTo(items.Count - 1);
                    return items.Count > 0;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width)
                return false;

            container.TopContainer().SetFocus(Name);
            if (Y == 0)
            {
                ToggleDropDown();
                NotifyClicked();
                return true;
            }

            int itemOffset = Y - 1;
            if (!IsDropDownOpen || itemOffset < 0 || itemOffset >= VisibleDropDownItemCount)
                return false;

            highlightedIndex = scrollIndex + itemOffset;
            ToggleHighlightedItem();
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            if (!Visible)
                return;

            DrawClosedControl(rows);
            if (IsDropDownOpen)
                DrawDropDown(rows);
        }

        bool IPopupControl.ContainsPopupPoint(short X, short Y)
            => Visible && X >= this.X && X < this.X + Width &&
                Y >= this.Y && Y < this.Y + 1 + VisibleDropDownItemCount;

        void IPopupControl.ClosePopup()
            => CloseDropDown();

        protected override object? GetValidationValue() => SelectedItems;

        private void DrawClosedControl(IList<Row> rows)
        {
            string text = selectedItems.Count == 0
                ? ""
                : selectedItems.Count == 1
                    ? selectedItems[0]
                    : $"{selectedItems.Count} selected";
            for (int x = 0; x < Width; x++)
            {
                if (!TryGetCell(rows, x, 0, out Cell cell))
                    continue;

                PrepareCell(cell, Focus ? BackgroundColor : ForeColor, Focus ? ForeColor : BackgroundColor);
                cell.Character = x switch
                {
                    0 => "[",
                    _ when x == Width - 2 => IsDropDownOpen ? "▲" : "▼",
                    _ when x == Width - 1 => "]",
                    _ => TuiText.CellAt(text, x - 1)
                };
            }
        }

        private void DrawDropDown(IList<Row> rows)
        {
            int visibleItemCount = VisibleDropDownItemCount;
            for (int row = 0; row < visibleItemCount; row++)
            {
                int itemIndex = scrollIndex + row;
                string item = items[itemIndex];
                bool highlighted = itemIndex == highlightedIndex;
                bool selected = selectedItems.Contains(item);
                string rendered = $"{(selected ? "☑" : "☐")} {item}";

                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, x, row + 1, out Cell cell))
                        continue;

                    PrepareCell(cell, highlighted ? BackgroundColor : ForeColor, highlighted ? ForeColor : BackgroundColor);
                    cell.Character = x == Width - 1
                        ? GetScrollCharacter(row, visibleItemCount)
                        : TuiText.CellAt(rendered, x);
                }
            }
        }

        private void ToggleHighlightedItem()
        {
            if (highlightedIndex < 0 || highlightedIndex >= items.Count)
                return;

            ToggleItem(items[highlightedIndex]);
        }

        private void MoveHighlight(int direction)
        {
            if (items.Count == 0)
                return;

            MoveHighlightTo(Math.Clamp(highlightedIndex + direction, 0, items.Count - 1));
        }

        private void MoveHighlightTo(int index)
        {
            if (items.Count == 0)
                return;

            highlightedIndex = Math.Clamp(index, 0, items.Count - 1);
            EnsureHighlightedItemVisible();
        }

        private string GetScrollCharacter(int row, int visibleItemCount)
        {
            if (items.Count <= visibleItemCount)
                return " ";
            if (visibleItemCount == 1)
                return "↕";
            if (row == 0)
                return scrollIndex > 0 ? "↑" : "│";
            if (row == visibleItemCount - 1)
                return scrollIndex + visibleItemCount < items.Count ? "↓" : "│";
            return "│";
        }

        private int VisibleDropDownItemCount
        {
            get
            {
                int availableRows = container is null ? maxDropDownItems : Math.Max(0, container.Height - Y - 1);
                return Math.Min(items.Count, Math.Min(maxDropDownItems, availableRows));
            }
        }

        private void EnsureHighlightedItemVisible()
        {
            int visibleItemCount = VisibleDropDownItemCount;
            if (highlightedIndex < 0 || visibleItemCount == 0)
            {
                scrollIndex = 0;
                return;
            }

            if (highlightedIndex < scrollIndex)
                scrollIndex = highlightedIndex;
            else if (highlightedIndex >= scrollIndex + visibleItemCount)
                scrollIndex = highlightedIndex - visibleItemCount + 1;

            scrollIndex = Math.Clamp(scrollIndex, 0, Math.Max(0, items.Count - visibleItemCount));
        }

        private bool TryGetCell(IList<Row> rows, int localX, int localY, out Cell cell)
        {
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

        private static string ValidateItem(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Contains('\r') || item.Contains('\n'))
                throw new ArgumentException("MultiSelectComboBox items cannot contain line breaks.", nameof(item));
            return item;
        }
    }
}
