using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class AutoCompleteBox : TextBox, IPopupControl
    {
        private readonly List<string> items;
        private readonly List<string> filteredItems = new();
        private int highlightedIndex;
        private int scrollIndex;
        private short maxDropDownItems;

        public AutoCompleteBox(
            string name,
            string text,
            IEnumerable<string> items,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            short maxDropDownItems = 5)
            : base(name, text, X, Y, width, foreColor, backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)4);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDropDownItems, (short)1);

            this.items = items.Select(ValidateItem).ToList();
            this.maxDropDownItems = maxDropDownItems;
            RefreshSuggestions(openWhenAvailable: false);
        }

        public IReadOnlyList<string> Items => items;

        public IReadOnlyList<string> FilteredItems => filteredItems;

        public string? HighlightedItem
            => highlightedIndex >= 0 && highlightedIndex < filteredItems.Count ? filteredItems[highlightedIndex] : null;

        public string? SelectedItem { get; private set; }

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

        public event EventHandler<AutoCompleteBoxSuggestionSelectedEventArgs>? SuggestionSelected;

        bool IPopupControl.IsPopupOpen => IsDropDownOpen;

        public void SetItems(IEnumerable<string> nextItems)
        {
            ArgumentNullException.ThrowIfNull(nextItems);
            items.Clear();
            items.AddRange(nextItems.Select(ValidateItem));
            RefreshSuggestions(openWhenAvailable: IsDropDownOpen);
        }

        public void AddItem(string item)
        {
            items.Add(ValidateItem(item));
            RefreshSuggestions(openWhenAvailable: IsDropDownOpen);
        }

        public bool RemoveItem(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            bool removed = items.Remove(item);
            if (removed)
                RefreshSuggestions(openWhenAvailable: IsDropDownOpen);

            return removed;
        }

        public void ClearItems()
        {
            items.Clear();
            RefreshSuggestions(openWhenAvailable: false);
            CloseDropDown();
        }

        public void OpenDropDown()
        {
            RefreshSuggestions(openWhenAvailable: true);
            if (filteredItems.Count == 0)
                return;

            if (container is not null)
            {
                container.TopContainer().SetFocus(Name);
                container.BringToFront(this);
            }

            IsDropDownOpen = true;
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

        internal void ExportAutoCompleteBoxState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            ExportTextInputState(state);
            state.SetBoolean("IsDropDownOpen", IsDropDownOpen);
            state.SetInteger("HighlightedIndex", highlightedIndex);
            state.SetInteger("ScrollIndex", scrollIndex);
            if (SelectedItem is not null)
                state.SetString("SelectedItem", SelectedItem);
        }

        internal void RestoreAutoCompleteBoxState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            RestoreTextInputState(state);
            RefreshSuggestions(openWhenAvailable: false);
            highlightedIndex = state.TryGetInteger("HighlightedIndex", out int restoredHighlightedIndex)
                ? Math.Clamp(restoredHighlightedIndex, 0, Math.Max(0, filteredItems.Count - 1))
                : 0;
            scrollIndex = state.TryGetInteger("ScrollIndex", out int restoredScrollIndex)
                ? Math.Max(0, restoredScrollIndex)
                : 0;
            if (state.TryGetString("SelectedItem", out string restoredSelectedItem) &&
                items.Contains(restoredSelectedItem))
            {
                SelectedItem = restoredSelectedItem;
            }

            IsDropDownOpen = state.TryGetBoolean("IsDropDownOpen", out bool restoredOpen) && restoredOpen && filteredItems.Count > 0;
            EnsureHighlightedItemVisible();
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width)
                return false;

            if (Y == 0)
            {
                bool handled = base.Click(X, Y);
                RefreshSuggestions(openWhenAvailable: false);
                return handled;
            }

            int itemOffset = Y - 1;
            if (!IsDropDownOpen || itemOffset < 0 || itemOffset >= VisibleDropDownItemCount)
                return false;

            highlightedIndex = scrollIndex + itemOffset;
            CommitHighlightedItem();
            NotifyClicked();
            return true;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (!Visible)
                return false;

            switch (key)
            {
                case "Enter":
                    if (IsDropDownOpen && HighlightedItem is not null)
                    {
                        CommitHighlightedItem();
                        return true;
                    }
                    return false;
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
                case "Home" when IsDropDownOpen:
                    MoveHighlightTo(0);
                    return true;
                case "End" when IsDropDownOpen:
                    MoveHighlightTo(filteredItems.Count - 1);
                    return true;
                default:
                    bool handled = base.KeyDown(key, shiftKey);
                    if (handled)
                    {
                        SelectedItem = null;
                        RefreshSuggestions(openWhenAvailable: true);
                    }
                    return handled;
            }
        }

        public override void Render(IList<Row> rows)
        {
            base.Render(rows);
            if (IsDropDownOpen)
                DrawDropDown(rows);
        }

        bool IPopupControl.ContainsPopupPoint(short X, short Y)
            => Visible && X >= this.X && X < this.X + Width &&
                Y >= this.Y && Y < this.Y + 1 + VisibleDropDownItemCount;

        void IPopupControl.ClosePopup()
            => CloseDropDown();

        private void RefreshSuggestions(bool openWhenAvailable)
        {
            string query = Value;
            filteredItems.Clear();
            filteredItems.AddRange(string.IsNullOrEmpty(query)
                ? items
                : items.Where(item => item.Contains(query, StringComparison.OrdinalIgnoreCase)));

            highlightedIndex = filteredItems.Count == 0 ? -1 : Math.Clamp(highlightedIndex, 0, filteredItems.Count - 1);
            if (filteredItems.Count > 0 && highlightedIndex < 0)
                highlightedIndex = 0;

            EnsureHighlightedItemVisible();
            IsDropDownOpen = openWhenAvailable && filteredItems.Count > 0;
        }

        private void CommitHighlightedItem()
        {
            if (HighlightedItem is not { } item)
                return;

            Value = item;
            SelectedItem = item;
            CloseDropDown();
            SuggestionSelected?.Invoke(this, new AutoCompleteBoxSuggestionSelectedEventArgs(item));
            NotifyClicked();
        }

        private void MoveHighlight(int direction)
        {
            if (filteredItems.Count == 0)
                return;

            MoveHighlightTo(Math.Clamp(highlightedIndex + direction, 0, filteredItems.Count - 1));
        }

        private void MoveHighlightTo(int index)
        {
            if (filteredItems.Count == 0)
                return;

            highlightedIndex = Math.Clamp(index, 0, filteredItems.Count - 1);
            EnsureHighlightedItemVisible();
        }

        private void DrawDropDown(IList<Row> rows)
        {
            int visibleItemCount = VisibleDropDownItemCount;
            for (int row = 0; row < visibleItemCount; row++)
            {
                int itemIndex = scrollIndex + row;
                string item = filteredItems[itemIndex];
                bool highlighted = itemIndex == highlightedIndex;
                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, x, row + 1, out Cell cell))
                        continue;

                    PrepareCell(cell, highlighted ? BackgroundColor : ForeColor, highlighted ? ForeColor : BackgroundColor);
                    cell.Character = x == Width - 1
                        ? GetScrollCharacter(row, visibleItemCount)
                        : TuiText.CellAt(item, x);
                }
            }
        }

        private string GetScrollCharacter(int row, int visibleItemCount)
        {
            if (filteredItems.Count <= visibleItemCount)
                return " ";
            if (visibleItemCount == 1)
                return "↕";
            if (row == 0)
                return scrollIndex > 0 ? "↑" : "│";
            if (row == visibleItemCount - 1)
                return scrollIndex + visibleItemCount < filteredItems.Count ? "↓" : "│";
            return "│";
        }

        private int VisibleDropDownItemCount
        {
            get
            {
                int availableRows = container is null ? maxDropDownItems : Math.Max(0, container.Height - Y - 1);
                return Math.Min(filteredItems.Count, Math.Min(maxDropDownItems, availableRows));
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

            scrollIndex = Math.Clamp(scrollIndex, 0, Math.Max(0, filteredItems.Count - visibleItemCount));
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
                throw new ArgumentException("AutoCompleteBox items cannot contain line breaks.", nameof(item));
            return item;
        }
    }
}
