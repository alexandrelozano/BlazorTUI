using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class ListBox : Control
    {
        public List<string> items = new List<string>();
        public IReadOnlyList<string> Items => items;
        public List<string> itemsSelected = new List<string>();
        public IReadOnlyList<string> SelectedItems => itemsSelected;
        private readonly IVirtualListBoxDataProvider? virtualItems;
        private readonly IVirtualListBoxDataOperationsProvider? virtualItemOperations;
        private readonly List<int> viewItemIndexes = new();
        private readonly List<string> selectedKeys = new();
        private string searchText = "";

        private bool multipleSelection;

        public bool AllowsMultipleSelection => multipleSelection;

        public bool IsVirtualized => virtualItems is not null;

        public int ItemCount
            => virtualItemOperations is not null
                ? virtualItemOperations.ViewCount
                : UsesSequentialVirtualItems ? SourceItemCount : viewItemIndexes.Count;

        public int SourceItemCount => virtualItems?.Count ?? items.Count;

        public string SearchText
        {
            get => searchText;
            set => SetSearchText(value);
        }

        public IReadOnlyList<string> SelectedKeys => selectedKeys;

        public int SelectedIndex => cursorY;

        public string? SelectedKey
            => cursorY >= 0 && cursorY < ItemCount ? GetItemKey(cursorY) : null;

        private int cursorY;
        private int scrollY;

        public override string GetAccessibilitySummary()
        {
            string selection = multipleSelection
                ? $"{Math.Max(itemsSelected.Count, selectedKeys.Count)} selected"
                : SelectedKey is null ? "no item selected" : $"selected item {GetItem(cursorY)}";
            string mode = IsVirtualized ? "virtualized" : "materialized";

            return FormatAccessibilitySummary($"ListBox {Name}: {mode}, {ItemCount} items, {selection}.");
        }

        public ListBox(string name, List<string> items, bool multipleSelection, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
        {
            ArgumentNullException.ThrowIfNull(items);

            this.Name = name;
            this.X = X;
            this.Y = Y;

            this.width = width;
            this.height = height;

            this.items = items;
            this.multipleSelection = multipleSelection;
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;

            scrollY = 0;
            cursorY = 0;

            this.Focus = false;
            this.TabStop = true;
            RefreshViewItems();
        }

        public ListBox(string name, IVirtualListBoxDataProvider items, bool multipleSelection, short X, short Y, short width, short height, Color forecolor, Color backgroundcolor)
        {
            ArgumentNullException.ThrowIfNull(items);

            Name = name;
            this.X = X;
            this.Y = Y;
            this.width = width;
            this.height = height;
            virtualItems = items;
            virtualItemOperations = items as IVirtualListBoxDataOperationsProvider;
            this.multipleSelection = multipleSelection;
            this.foreColor = forecolor;
            this.backgroundColor = backgroundcolor;

            scrollY = 0;
            cursorY = 0;

            Focus = false;
            TabStop = true;
            RefreshViewItems();
        }

        public async Task RefreshVirtualQueryAsync(CancellationToken cancellationToken = default)
        {
            if (virtualItemOperations is null)
                return;

            await virtualItemOperations
                .ApplyQueryAsync(CreateVirtualListBoxQuery(), cancellationToken)
                .ConfigureAwait(false);
            ClampViewState();
        }

        internal void ExportListBoxState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            state.SetStringList("SelectedItems", itemsSelected);
            state.SetStringList("SelectedKeys", selectedKeys);
            state.SetInteger("CursorY", cursorY);
            state.SetInteger("ScrollY", scrollY);
        }

        internal void RestoreListBoxState(TuiElementState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            if (state.TryGetStringList("SelectedItems", out IReadOnlyList<string> restoredSelectedItems))
            {
                itemsSelected.Clear();
                foreach (string item in restoredSelectedItems)
                {
                    if (items.Contains(item) && !itemsSelected.Contains(item))
                        itemsSelected.Add(item);
                }
            }

            if (state.TryGetStringList("SelectedKeys", out IReadOnlyList<string> restoredSelectedKeys))
            {
                selectedKeys.Clear();
                foreach (string key in restoredSelectedKeys)
                {
                    if (!selectedKeys.Contains(key))
                        selectedKeys.Add(key);
                }
            }

            int maximumIndex = Math.Max(0, ItemCount - 1);
            cursorY = state.TryGetInteger("CursorY", out int restoredCursorY)
                ? Math.Clamp(restoredCursorY, 0, maximumIndex)
                : 0;
            scrollY = state.TryGetInteger("ScrollY", out int restoredScrollY)
                ? Math.Clamp(restoredScrollY, 0, Math.Max(0, ItemCount - height))
                : 0;
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            bool handled = false;

            if (Visible)
            {
                switch (key)
                {
                    case "Tab":
                        break;
                    case "Enter":
                        container.TopContainer().SetFocus(name);
                        handled = true;
                        break;
                    case "Space":
                    case " ":
                        if (multipleSelection)
                            SelectItem(cursorY);
                        container.TopContainer().SetFocus(name);
                        handled = true;
                        break;
                    case "Backspace":
                        break;
                    case "ArrowUp":
                        if (cursorY > 0)
                        {
                            cursorY--;
                            EnsureCursorVisible();

                            if (!multipleSelection)
                                SelectItem(cursorY);
                            handled = true;
                        }
                        break;
                    case "ArrowDown":
                        if (cursorY < ItemCount - 1)
                        {
                            cursorY++;
                            EnsureCursorVisible();

                            if (!multipleSelection)
                                SelectItem(cursorY);
                            handled = true;
                        }
                        break;
                    default:
                        break;
                }
            }

            if (handled)
                NotifyClicked();

            return handled;
        }

        public void SelectKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (virtualItemOperations is not null)
            {
                int viewIndex = virtualItemOperations.FindViewIndexByKey(key);
                if (viewIndex < 0)
                    throw new ArgumentException("The key does not belong to this ListBox view.", nameof(key));

                cursorY = viewIndex;
                SelectItem(viewIndex);
                EnsureCursorVisible();
                return;
            }

            for (int index = 0; index < ItemCount; index++)
            {
                if (GetItemKey(index) == key)
                {
                    cursorY = index;
                    SelectItem(index);
                    EnsureCursorVisible();
                    return;
                }
            }

            throw new ArgumentException("The key does not belong to this ListBox.", nameof(key));
        }

        private void SelectItem(int index)
        {
            if (index < 0 || index >= ItemCount)
                return;

            string item = GetItem(index);
            string key = GetItemKey(index);

            if (multipleSelection)
            {
                if (itemsSelected.Contains(item))
                    itemsSelected.Remove(item);
                else
                    itemsSelected.Add(item);

                if (selectedKeys.Contains(key))
                    selectedKeys.Remove(key);
                else
                    selectedKeys.Add(key);
            }
            else
            {
                itemsSelected.Clear();
                itemsSelected.Add(item);
                selectedKeys.Clear();
                selectedKeys.Add(key);
            }
        }

        public override bool Click(short X, short Y)
        {
            bool handled = false;

            if (Visible)
            {
                if (X == width - 1)
                {
                    if (Y == 0)
                    {
                        if (scrollY > 0)
                            scrollY--;
                    }
                    else if (Y == height -1)
                    {
                    if (scrollY < ItemCount - height)
                            scrollY++;
                    }
                }
                else
                {
                    int itemIndex = Y + scrollY;
                    if (itemIndex < 0 || itemIndex >= ItemCount)
                        return false;

                    cursorY = itemIndex;
                    SelectItem(itemIndex);
                }       
                
                virtualItemOperations?.ApplyQuery(CreateVirtualListBoxQuery());
                container.TopContainer().SetFocus(name);
                handled = true;
            }

            if (handled)
                NotifyClicked();

            return handled;
        }

        public override void Render(IList<Row> rows)
        {
            if (Visible)
            {
                virtualItemOperations?.ApplyQuery(CreateVirtualListBoxQuery());
                for (short Yn = 0; Yn < height; Yn++)
                {
                    int itemIndex = Yn + scrollY;
                    string item = itemIndex < ItemCount ? GetItem(itemIndex) : "";
                    for (short n = 0; n < width; n++)
                    {
                        if (container.YOffset() + Y + Yn < container.YOffset() + container.height && container.YOffset() + Y + Yn < rows.Count)
                        {
                            int absoluteY = container.YOffset() + Y + Yn;
                            if (container.XOffset() + X + n < container.XOffset() + container.width && container.XOffset() + X + n < rows[absoluteY].Cells.Count)
                            {
                                string ch = " ";

                                Color foreColorTmp = foreColor;
                                Color backgroundColorTmp = backgroundColor;

                                if (n == 0)
                                {
                                    if (itemIndex < ItemCount && IsItemSelected(itemIndex, item))
                                    {
                                        ch = "●";
                                    }

                                    if (Focus && (Yn + scrollY) == cursorY)
                                    {
                                        foreColorTmp = backgroundColor;
                                        backgroundColorTmp = foreColor;
                                    }
                                }
                                else if (n == width - 1)
                                {
                                    if (Yn == 0)
                                        ch = "↑";
                                    else if (Yn == height - 1)
                                        ch = "↓";
                                    else
                                        ch = "│";
                                }
                                else
                                {
                                    ch = TuiText.CellAt(item, n - 1);

                                    if (Focus && (Yn + scrollY) == cursorY)
                                    {
                                        foreColorTmp = backgroundColor;
                                        backgroundColorTmp = foreColor;
                                    }
                                }

                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + n].foreColor = foreColorTmp;
                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + n].backgroundColor = backgroundColorTmp;
                                rows[container.YOffset() + Y + Yn].Cells[container.XOffset() + X + n].character = ch;
                            }
                        }
                    }
                }
            }
        }

        protected override object? GetValidationValue() => SelectedItems;

        private string GetItem(int index)
            => virtualItemOperations is not null
                ? virtualItemOperations.GetViewItem(index)
                : GetSourceItem(GetSourceItemIndex(index));

        private string GetItemKey(int index)
            => virtualItemOperations is not null
                ? virtualItemOperations.GetViewKey(index)
                : GetSourceItemKey(GetSourceItemIndex(index));

        private bool IsItemSelected(int index, string item)
            => virtualItems is null
                ? itemsSelected.Contains(item)
                : selectedKeys.Contains(GetItemKey(index));

        private int SourceItemIndexCount => SourceItemCount;

        private bool UsesSequentialVirtualItems
            => virtualItems is not null &&
                virtualItemOperations is null &&
                searchText.Length == 0;

        private int GetSourceItemIndex(int viewIndex)
        {
            if (viewIndex < 0 || viewIndex >= ItemCount)
                throw new ArgumentOutOfRangeException(nameof(viewIndex));

            return UsesSequentialVirtualItems ? viewIndex : viewItemIndexes[viewIndex];
        }

        private string GetSourceItem(int sourceIndex)
            => virtualItems is null ? items[sourceIndex] : virtualItems.GetItem(sourceIndex);

        private string GetSourceItemKey(int sourceIndex)
            => virtualItems is null ? GetSourceItem(sourceIndex) : virtualItems.GetKey(sourceIndex);

        private void SetSearchText(string? value)
        {
            string nextSearchText = value ?? "";
            if (nextSearchText.Contains('\r') || nextSearchText.Contains('\n'))
                throw new ArgumentException("ListBox search text cannot contain line breaks.", nameof(value));

            if (searchText == nextSearchText)
                return;

            searchText = nextSearchText;
            RefreshViewItems();
        }

        private void RefreshViewItems()
        {
            if (virtualItemOperations is not null)
            {
                viewItemIndexes.Clear();
                virtualItemOperations.ApplyQuery(CreateVirtualListBoxQuery());
                ClampViewState();
                return;
            }

            if (UsesSequentialVirtualItems)
            {
                viewItemIndexes.Clear();
                ClampViewState();
                return;
            }

            viewItemIndexes.Clear();
            for (int sourceIndex = 0; sourceIndex < SourceItemIndexCount; sourceIndex++)
            {
                if (ItemMatchesSearch(sourceIndex))
                    viewItemIndexes.Add(sourceIndex);
            }

            ClampViewState();
        }

        private VirtualListBoxQuery CreateVirtualListBoxQuery()
            => new(searchText, height <= 0 ? 0 : scrollY / Math.Max(1, (int)height), Math.Max(1, (int)height));

        private bool ItemMatchesSearch(int sourceIndex)
            => searchText.Length == 0 ||
                GetSourceItem(sourceIndex).Contains(searchText, StringComparison.OrdinalIgnoreCase);

        private void ClampViewState()
        {
            int maximumIndex = Math.Max(0, ItemCount - 1);
            cursorY = Math.Clamp(cursorY, 0, maximumIndex);
            scrollY = Math.Clamp(scrollY, 0, Math.Max(0, ItemCount - height));
        }

        private void EnsureCursorVisible()
        {
            if (cursorY < scrollY)
                scrollY = cursorY;
            else if (cursorY >= scrollY + height)
                scrollY = cursorY - height + 1;

            scrollY = Math.Clamp(scrollY, 0, Math.Max(0, ItemCount - height));
            virtualItemOperations?.ApplyQuery(CreateVirtualListBoxQuery());
        }
    }
}
