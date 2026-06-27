using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class ComboBox : Control, IPopupControl
    {
        private readonly List<string> items;
        private int selectedIndex;
        private int highlightedIndex;
        private int scrollIndex;
        private short maxDropDownItems;

        public IReadOnlyList<string> Items => items;

        public int SelectedIndex
        {
            get => selectedIndex;
            set => SelectIndex(value);
        }

        public string? SelectedItem
            => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;

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

        public event EventHandler? SelectedIndexChanged;

        bool IPopupControl.IsPopupOpen => IsDropDownOpen;

        public ComboBox(
            string name,
            IEnumerable<string> items,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            int selectedIndex = 0,
            short maxDropDownItems = 5)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)4);
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDropDownItems, (short)1);

            this.items = items.Select(ValidateItem).ToList();
            if (this.items.Count == 0)
            {
                if (selectedIndex != -1 && selectedIndex != 0)
                    throw new ArgumentOutOfRangeException(nameof(selectedIndex));

                selectedIndex = -1;
            }
            else if (selectedIndex < -1 || selectedIndex >= this.items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            }

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
            this.selectedIndex = selectedIndex;
            highlightedIndex = selectedIndex;
            this.maxDropDownItems = maxDropDownItems;

            LostFocus += (_, _) => CloseDropDown();
        }

        public void SelectIndex(int index)
        {
            ValidateSelectedIndex(index);
            SetSelectedIndex(index);
        }

        public void SelectItem(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            int index = items.IndexOf(item);
            if (index < 0)
                throw new ArgumentException("The item does not belong to this ComboBox.", nameof(item));

            SetSelectedIndex(index);
        }

        public void AddItem(string item)
        {
            items.Add(ValidateItem(item));
            if (selectedIndex < 0)
                SetSelectedIndex(0);
        }

        public bool RemoveItem(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            int index = items.IndexOf(item);
            if (index < 0)
                return false;

            items.RemoveAt(index);
            int nextSelectedIndex = selectedIndex;
            if (items.Count == 0)
                nextSelectedIndex = -1;
            else if (index < selectedIndex)
                nextSelectedIndex--;
            else if (selectedIndex >= items.Count)
                nextSelectedIndex = items.Count - 1;

            if (nextSelectedIndex != selectedIndex)
                SetSelectedIndex(nextSelectedIndex);
            else if (index == selectedIndex)
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);

            highlightedIndex = selectedIndex;
            EnsureHighlightedItemVisible();
            return true;
        }

        public void ClearItems()
        {
            if (items.Count == 0)
                return;

            items.Clear();
            SetSelectedIndex(-1);
            CloseDropDown();
        }

        public void OpenDropDown()
        {
            if (items.Count == 0 || IsDropDownOpen)
                return;

            if (container is not null)
            {
                container.TopContainer().SetFocus(Name);
                container.BringToFront(this);
            }
            IsDropDownOpen = true;
            highlightedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            EnsureHighlightedItemVisible();
        }

        public void CloseDropDown()
        {
            IsDropDownOpen = false;
            highlightedIndex = selectedIndex;
            EnsureHighlightedItemVisible();
        }

        public void ToggleDropDown()
        {
            if (IsDropDownOpen)
                CloseDropDown();
            else
                OpenDropDown();
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
                        CommitHighlightedItem();
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
                    MoveSelection(1);
                    return true;
                case "ArrowUp":
                    MoveSelection(-1);
                    return true;
                case "Home":
                    MoveSelectionTo(0);
                    return items.Count > 0;
                case "End":
                    MoveSelectionTo(items.Count - 1);
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
            CommitHighlightedItem();
            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
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

        private void DrawClosedControl(IList<Row> rows)
        {
            string text = SelectedItem ?? string.Empty;
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

                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, x, row + 1, out Cell cell))
                        continue;

                    PrepareCell(
                        cell,
                        highlighted ? BackgroundColor : ForeColor,
                        highlighted ? ForeColor : BackgroundColor);
                    cell.Character = x == Width - 1
                        ? GetScrollCharacter(row, visibleItemCount)
                        : TuiText.CellAt(item, x);
                }
            }
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

        private void MoveSelection(int direction)
        {
            if (items.Count == 0)
                return;

            int currentIndex = IsDropDownOpen ? highlightedIndex : selectedIndex;
            int nextIndex = Math.Clamp(currentIndex + direction, 0, items.Count - 1);
            MoveSelectionTo(nextIndex);
        }

        private void MoveSelectionTo(int index)
        {
            if (items.Count == 0)
                return;

            if (IsDropDownOpen)
            {
                highlightedIndex = index;
                EnsureHighlightedItemVisible();
            }
            else
            {
                SetSelectedIndex(index);
            }
        }

        private void CommitHighlightedItem()
        {
            if (highlightedIndex >= 0)
                SetSelectedIndex(highlightedIndex);
            CloseDropDown();
        }

        private void SetSelectedIndex(int index)
        {
            if (selectedIndex == index)
                return;

            selectedIndex = index;
            highlightedIndex = index;
            EnsureHighlightedItemVisible();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ValidateSelectedIndex(int index)
        {
            if (index < -1 || index >= items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
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

        private int VisibleDropDownItemCount
        {
            get
            {
                int availableRows = container is null ? maxDropDownItems : Math.Max(0, container.Height - Y - 1);
                return Math.Min(items.Count, Math.Min(maxDropDownItems, availableRows));
            }
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
                throw new ArgumentException("ComboBox items cannot contain line breaks.", nameof(item));
            return item;
        }
    }
}
