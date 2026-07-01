using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class ContextMenu : Control, IPopupControl
    {
        private readonly List<ContextMenuItem> items = new();
        private readonly HashSet<string> targetControlNames = new(StringComparer.Ordinal);
        private int selectedIndex;

        public IReadOnlyList<ContextMenuItem> Items => items;

        public IReadOnlyCollection<string> TargetControlNames => targetControlNames;

        public bool IsOpen { get; private set; }

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();
                if (visibleItems.Count == 0)
                {
                    selectedIndex = -1;
                    return;
                }

                selectedIndex = Math.Clamp(value, 0, visibleItems.Count - 1);
                if (!IsSelectable(selectedIndex))
                    selectedIndex = FindNextSelectable(selectedIndex, 1);
            }
        }

        public event EventHandler<ContextMenuItemClickedEventArgs>? ItemClicked;

        bool IPopupControl.IsPopupOpen => IsOpen;

        public ContextMenu(
            string name,
            IEnumerable<ContextMenuItem> items,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            IEnumerable<string>? targetControlNames = null)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)4);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = 1;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = false;
            SetItems(items);

            if (targetControlNames is not null)
            {
                foreach (string targetControlName in targetControlNames)
                    AttachTo(targetControlName);
            }
        }

        public ContextMenu(
            string name,
            IEnumerable<TuiCommand> commands,
            short X,
            short Y,
            short width,
            Color foreColor,
            Color backgroundColor,
            IEnumerable<string>? targetControlNames = null)
            : this(
                name,
                commands?.Select(command => new ContextMenuItem(command)) ??
                    throw new ArgumentNullException(nameof(commands)),
                X,
                Y,
                width,
                foreColor,
                backgroundColor,
                targetControlNames)
        {
        }

        public void SetItems(IEnumerable<ContextMenuItem> nextItems)
        {
            ArgumentNullException.ThrowIfNull(nextItems);
            items.Clear();
            foreach (ContextMenuItem item in nextItems)
                AddItem(item);

            selectedIndex = FindNextSelectable(0, 1);
        }

        public void AddItem(ContextMenuItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (GetItem(item.Name) is not null)
                throw new InvalidOperationException($"A context menu item named '{item.Name}' already exists.");

            items.Add(item);
            selectedIndex = FindNextSelectable(Math.Max(0, selectedIndex), 1);
        }

        public ContextMenuItem AddCommand(TuiCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            var item = new ContextMenuItem(command);
            AddItem(item);
            return item;
        }

        public ContextMenuItem? GetItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return items.FirstOrDefault(item => item.Name == name);
        }

        public bool RemoveItem(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ContextMenuItem? item = GetItem(name);
            bool removed = item is not null && items.Remove(item);
            if (removed)
                selectedIndex = FindNextSelectable(Math.Min(selectedIndex, Math.Max(0, items.Count - 1)), 1);

            return removed;
        }

        public void ClearItems()
        {
            items.Clear();
            selectedIndex = -1;
            Close();
        }

        public void AttachTo(string controlName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(controlName);
            targetControlNames.Add(controlName);
        }

        public bool DetachFrom(string controlName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(controlName);
            return targetControlNames.Remove(controlName);
        }

        public void ClearTargets() => targetControlNames.Clear();

        public bool IsAttachedTo(string controlName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(controlName);
            return targetControlNames.Count == 0 || targetControlNames.Contains(controlName);
        }

        public void OpenAt(short x, short y)
        {
            IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();
            if (!Visible || visibleItems.Count == 0)
                return;

            X = x;
            Y = y;
            Height = (short)Math.Max(1, visibleItems.Count);
            selectedIndex = FindNextSelectable(selectedIndex < 0 ? 0 : selectedIndex, 1);
            IsOpen = true;
            container?.BringToFront(this);
        }

        public bool TryOpenFor(Control target)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (!IsAttachedTo(target.Name))
                return false;

            OpenAt(target.X, (short)(target.Y + target.Height));
            return IsOpen;
        }

        public void Close()
        {
            IsOpen = false;
            Height = 1;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            if (!Visible)
                return false;

            if (!IsOpen)
                return false;

            IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();

            switch (key)
            {
                case "Escape":
                    Close();
                    return true;
                case "ArrowDown":
                    MoveSelection(1);
                    return true;
                case "ArrowUp":
                    MoveSelection(-1);
                    return true;
                case "Home":
                    selectedIndex = FindNextSelectable(0, 1);
                    return true;
                case "End":
                    selectedIndex = FindNextSelectable(visibleItems.Count - 1, -1);
                    return true;
                case "Enter":
                case "Space":
                case " ":
                    InvokeSelectedItem();
                    return true;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();
            if (!Visible || !IsOpen || X < 0 || X >= Width || Y < 0 || Y >= visibleItems.Count)
                return false;

            selectedIndex = Y;
            InvokeSelectedItem();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible || !IsOpen || container is null)
                return;

            IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();
            for (int row = 0; row < visibleItems.Count; row++)
                RenderItem(rows, row, visibleItems[row], row == selectedIndex);
        }

        internal void ExportContextMenuState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            state.SetBoolean("IsOpen", IsOpen);
            state.SetInteger("SelectedIndex", selectedIndex);
            state.SetInteger("X", X);
            state.SetInteger("Y", Y);
        }

        internal void RestoreContextMenuState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TryGetInteger("SelectedIndex", out int restoredSelectedIndex))
                SelectedIndex = restoredSelectedIndex;

            if (state.TryGetInteger("X", out int restoredX) &&
                restoredX >= short.MinValue &&
                restoredX <= short.MaxValue)
            {
                X = (short)restoredX;
            }

            if (state.TryGetInteger("Y", out int restoredY) &&
                restoredY >= short.MinValue &&
                restoredY <= short.MaxValue)
            {
                Y = (short)restoredY;
            }

            IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();
            IsOpen = state.TryGetBoolean("IsOpen", out bool isOpen) && isOpen && visibleItems.Count > 0;
            Height = (short)(IsOpen ? Math.Max(1, visibleItems.Count) : 1);
        }

        bool IPopupControl.ContainsPopupPoint(short X, short Y)
            => Visible && IsOpen && X >= this.X && X < this.X + Width &&
                Y >= this.Y && Y < this.Y + GetVisibleItems().Count;

        void IPopupControl.ClosePopup()
            => Close();

        private void RenderItem(IList<Row> rows, int row, ContextMenuItem item, bool selected)
        {
            bool selectable = item.Type != ContextMenuItemType.Separator && item.Enabled;
            Color foreground = item.Enabled ? ForeColor : Color.Gray;
            Color background = BackgroundColor;
            if (selected && selectable)
            {
                foreground = BackgroundColor;
                background = ForeColor;
            }

            string text = item.Type == ContextMenuItemType.Separator
                ? new string('─', Width)
                : TuiText.PadRightToVisualWidth(item.Text, Width);

            for (int x = 0; x < Width; x++)
                WriteCell(rows, x, row, TuiText.CellAt(text, x), foreground, background);
        }

        private void MoveSelection(int delta)
        {
            if (GetVisibleItems().Count == 0)
                return;

            selectedIndex = FindNextSelectable(selectedIndex + delta, delta);
        }

        private int FindNextSelectable(int start, int delta)
        {
            IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();
            if (visibleItems.Count == 0)
                return -1;

            int direction = delta < 0 ? -1 : 1;
            int index = ((start % visibleItems.Count) + visibleItems.Count) % visibleItems.Count;
            for (int attempts = 0; attempts < visibleItems.Count; attempts++)
            {
                if (IsSelectable(index))
                    return index;

                index = (index + direction + visibleItems.Count) % visibleItems.Count;
            }

            return 0;
        }

        private bool IsSelectable(int index)
        {
            IReadOnlyList<ContextMenuItem> visibleItems = GetVisibleItems();
            return index >= 0 &&
                index < visibleItems.Count &&
                visibleItems[index].Enabled &&
                visibleItems[index].Type != ContextMenuItemType.Separator;
        }

        private void InvokeSelectedItem()
        {
            if (!IsSelectable(selectedIndex))
                return;

            ContextMenuItem item = GetVisibleItems()[selectedIndex];
            bool invoked = item.Invoke();
            if (invoked)
                ItemClicked?.Invoke(this, new ContextMenuItemClickedEventArgs(item));
            Close();
            NotifyClicked();
        }

        private IReadOnlyList<ContextMenuItem> GetVisibleItems()
            => items.Where(item => item.Visible).ToList();

        private void WriteCell(IList<Row> rows, int localX, int localY, string character, Color foreColor, Color backgroundColor)
        {
            int absoluteX = container.XOffset() + X + localX;
            int absoluteY = container.YOffset() + Y + localY;
            if (absoluteX < container.XOffset() ||
                absoluteX >= container.XOffset() + container.Width ||
                absoluteY < container.YOffset() ||
                absoluteY >= container.YOffset() + container.Height ||
                absoluteY < 0 ||
                absoluteY >= rows.Count ||
                absoluteX < 0 ||
                absoluteX >= rows[absoluteY].Cells.Count)
            {
                return;
            }

            Cell cell = rows[absoluteY].Cells[absoluteX];
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = character;
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }
    }
}
