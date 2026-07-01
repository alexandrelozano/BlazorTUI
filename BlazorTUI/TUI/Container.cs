using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class Container
    {
        internal string name { get; set; } = "";

        public string Name
        {
            get => name;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                name = value;
            }
        }

        public short X { get; set; }

        public short Y { get; set; }

        internal short width { get; set; }

        public short Width { get => width; set => width = value; }

        internal short height { get; set; }

        public short Height { get => height; set => height = value; }

        public bool Visible { get; set; } = true;

        public short ZOrder { get; set; }

        public string ScreenReaderSummary { get; set; } = "";

        public string ScreenReaderDescription { get; set; } = "";

        public bool IsFocusScope { get; set; }

        internal List<Control> controls { get; set; } = new List<Control>();

        public IReadOnlyList<Control> Controls => controls;

        internal List<Container> containers { get; set; } = new List<Container>();

        public IReadOnlyList<Container> Containers => containers;

        internal Container? parent { get; set; } = null;

        public Container? Parent => parent;

        public Container(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            this.Name = name;
        }

        public Container TopContainer()
        {
            if (parent == null)
            {
                return this;
            }
            else
            {
                return parent.TopContainer();
            }
        }

        public virtual void SetFocus(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            foreach (Control control in controls)
            {
                if (control.name == name && control.TabStop)
                {
                    control.Focus = true;
                }
                else
                {
                    control.Focus = false;
                }
            }

            foreach (Container container in containers)
            {
                container.SetFocus(name);
            }
        }

        public virtual string GetAccessibilitySummary()
        {
            if (string.IsNullOrWhiteSpace(ScreenReaderSummary) &&
                string.IsNullOrWhiteSpace(ScreenReaderDescription))
            {
                return "";
            }

            return FormatAccessibilitySummary($"{GetType().Name} {Name}");
        }

        public virtual void Click(short X, short Y)
        {
            if (TryClickOpenPopup(X, Y))
                return;

            foreach (Control control in controls)
            {
                if (control.Visible && control.X <= X && control.X + control.width > X && control.Y <= Y && control.Y + control.height > Y)
                {
                    control.Click((short)(X - control.X), (short)(Y - control.Y));
                }
            }

            foreach (Container container in containers)
            {
                if (container.Visible)
                    container.Click((short)(X - container.X), (short)(Y - container.Y));
            }
        }

        public virtual bool ContextClick(short X, short Y)
        {
            if (TryClickOpenPopup(X, Y))
                return true;

            foreach (Container container in containers
                .Where(container => container.Visible)
                .OrderByDescending(container => container.ZOrder))
            {
                if (X >= container.X && X < container.X + container.Width &&
                    Y >= container.Y && Y < container.Y + container.Height &&
                    container.ContextClick((short)(X - container.X), (short)(Y - container.Y)))
                {
                    return true;
                }
            }

            foreach (Control control in controls
                .Where(control => control.Visible)
                .OrderByDescending(control => control.ZOrder))
            {
                if (control is ContextMenu)
                    continue;

                if (control.X <= X && control.X + control.Width > X &&
                    control.Y <= Y && control.Y + control.Height > Y &&
                    TryOpenContextMenuFor(control, X, Y))
                {
                    return true;
                }
            }

            CloseOpenContextMenus();
            return false;
        }

        public virtual bool BeginDrag(short X, short Y) => false;

        public virtual bool Drag(short startX, short startY, short currentX, short currentY) => false;

        public virtual bool EndDrag(short startX, short startY, short currentX, short currentY) => false;

        internal virtual TuiDragSession? BeginMouseDrag(short X, short Y)
        {
            if (!Visible)
                return null;

            TuiDragSession? popupSession = TryBeginDragOpenPopup(X, Y);
            if (popupSession is not null)
                return popupSession;

            foreach (Container container in containers
                .Where(container => container.Visible)
                .OrderByDescending(container => container.ZOrder))
            {
                if (X < container.X || X >= container.X + container.Width ||
                    Y < container.Y || Y >= container.Y + container.Height)
                {
                    continue;
                }

                TuiDragSession? session = container.BeginMouseDrag(
                    (short)(X - container.X),
                    (short)(Y - container.Y));
                if (session is not null)
                    return session;
            }

            foreach (Control control in controls
                .Where(control => control.Visible)
                .OrderByDescending(control => control.ZOrder))
            {
                if (control.X > X || control.X + control.Width <= X ||
                    control.Y > Y || control.Y + control.Height <= Y)
                {
                    continue;
                }

                short localX = (short)(X - control.X);
                short localY = (short)(Y - control.Y);
                if (control.BeginDrag(localX, localY))
                    return new TuiDragSession(control, localX, localY);
            }

            return BeginDrag(X, Y)
                ? new TuiDragSession(this, X, Y)
                : null;
        }

        public virtual void KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

            if (TryRouteOpenContextMenuKey(key, shiftKey))
                return;

            if ((key == "ContextMenu" || (key == "F10" && shiftKey)) && TryOpenContextMenuForFocusedControl())
                return;

            if (key == "Tab")
            {
                Control? currentFocusControl = TopContainer().GetCurrentFocusControl();

                if (currentFocusControl != null)
                {
                    if (!shiftKey)
                    {
                        currentFocusControl.container.FocusNextControl();
                    }
                    else
                    {
                        currentFocusControl.container.FocusPreviousControl();
                    }
                }
            }
            else
            {
                foreach (Control control in controls)
                {
                    if (control.Focus)
                    {
                        control.KeyDown(key, shiftKey);
                    }
                }

                foreach (Container container in containers.Where(container => container.Visible))
                {
                    container.KeyDown(key, shiftKey);
                }
            }
        }

        public void AddControl(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (String.IsNullOrWhiteSpace(control.name))
            {
                throw new ArgumentException("Control must have a non-empty name.", nameof(control));
            }

            if (TopContainer().GetControl(control.name)!=null)
            {
                throw new InvalidOperationException($"A control named '{control.name}' already exists.");
            }

            if (control.TabStop && control.TabIndex == 0)
            {
                control.TabIndex = (short)(controls
                    .Where(existing => existing.TabStop)
                    .Select(existing => existing.TabIndex)
                    .DefaultIfEmpty()
                    .Max() + 1);
            }

            if (control.width < 1 || control.height < 1)
                throw new ArgumentOutOfRangeException(nameof(control), "Control dimensions must be positive.");

            controls.Add(control);
            control.container = this;
            control.ZOrder = (short)((from p in controls orderby p.ZOrder descending select p.ZOrder).FirstOrDefault() + 1);
        }

        public void AddContainer(Container container)
        {
            ArgumentNullException.ThrowIfNull(container);

            if (container.width < 1 || container.height < 1)
                throw new ArgumentOutOfRangeException(nameof(container), "Container dimensions must be positive.");

            containers.Add(container);
            container.parent = this;
            container.ZOrder = (short)((from p in containers orderby p.ZOrder descending select p.ZOrder).FirstOrDefault() + 1);
        }

        public Control? GetControl(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            Control? ret = null;

            if (controls != null)
            {
                ret = (from c in controls where c.name == name select c).FirstOrDefault();  
            }

            if (ret == null)
            {
                foreach (Container container in containers)
                {
                    ret = container.GetControl(name);

                    if (ret != null)
                    {
                        break;
                    }
                }
            }

            return ret;
        }

        public void BringToFront(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (control != null)
            {
                short z = 0;
                foreach(Control c in (from c in controls where c.name != control.name orderby c.ZOrder select c))
                {
                    c.ZOrder = ++z;
                }

                control.ZOrder = ++z;
            }
        }

        public virtual bool Validate(bool focusFirstInvalid = true)
        {
            List<Control> invalidControls = new();
            ValidateInto(invalidControls, validationGroup: null);

            Control? firstFocusableInvalidControl = invalidControls.FirstOrDefault(control => control.TabStop);
            if (focusFirstInvalid && firstFocusableInvalidControl is not null)
                TopContainer().SetFocus(firstFocusableInvalidControl.Name);

            return invalidControls.Count == 0;
        }

        public virtual bool Validate(string validationGroup, bool focusFirstInvalid = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(validationGroup);

            List<Control> invalidControls = new();
            ValidateInto(invalidControls, validationGroup.Trim());

            Control? firstFocusableInvalidControl = invalidControls.FirstOrDefault(control => control.TabStop);
            if (focusFirstInvalid && firstFocusableInvalidControl is not null)
                TopContainer().SetFocus(firstFocusableInvalidControl.Name);

            return invalidControls.Count == 0;
        }

        public IReadOnlyList<Control> GetInvalidControls()
        {
            List<Control> invalidControls = new();
            AddInvalidControls(invalidControls, validationGroup: null);
            return invalidControls;
        }

        public IReadOnlyList<Control> GetInvalidControls(string validationGroup)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(validationGroup);

            List<Control> invalidControls = new();
            AddInvalidControls(invalidControls, validationGroup.Trim());
            return invalidControls;
        }

        private bool TryClickOpenPopup(short X, short Y)
        {
            foreach (Control control in controls.OrderByDescending(control => control.ZOrder))
            {
                if (control is not IPopupControl { IsPopupOpen: true } popup)
                    continue;

                if (popup.ContainsPopupPoint(X, Y))
                {
                    control.Click((short)(X - control.X), (short)(Y - control.Y));
                    return true;
                }

                popup.ClosePopup();
            }

            foreach (Container container in containers
                .Where(container => container.Visible)
                .OrderByDescending(container => container.ZOrder))
            {
                if (container.TryClickOpenPopup((short)(X - container.X), (short)(Y - container.Y)))
                    return true;
            }

            return false;
        }

        private TuiDragSession? TryBeginDragOpenPopup(short X, short Y)
        {
            foreach (Control control in controls.OrderByDescending(control => control.ZOrder))
            {
                if (control is not IPopupControl { IsPopupOpen: true } popup)
                    continue;

                if (popup.ContainsPopupPoint(X, Y))
                {
                    short localX = (short)(X - control.X);
                    short localY = (short)(Y - control.Y);
                    return control.BeginDrag(localX, localY)
                        ? new TuiDragSession(control, localX, localY)
                        : null;
                }

                popup.ClosePopup();
            }

            foreach (Container container in containers
                .Where(container => container.Visible)
                .OrderByDescending(container => container.ZOrder))
            {
                TuiDragSession? session = container.TryBeginDragOpenPopup(
                    (short)(X - container.X),
                    (short)(Y - container.Y));
                if (session is not null)
                    return session;
            }

            return null;
        }

        private bool TryRouteOpenContextMenuKey(string key, bool shiftKey)
        {
            foreach (ContextMenu contextMenu in controls
                .OfType<ContextMenu>()
                .Where(menu => menu.IsOpen)
                .OrderByDescending(menu => menu.ZOrder))
            {
                if (contextMenu.KeyDown(key, shiftKey))
                    return true;
            }

            foreach (Container container in containers
                .Where(container => container.Visible)
                .OrderByDescending(container => container.ZOrder))
            {
                if (container.TryRouteOpenContextMenuKey(key, shiftKey))
                    return true;
            }

            return false;
        }

        private bool TryOpenContextMenuForFocusedControl()
        {
            Control? focusedControl = TopContainer().GetCurrentFocusControl();
            if (focusedControl?.ParentContainer is null)
                return false;

            return focusedControl.ParentContainer.TryOpenContextMenuFor(
                focusedControl,
                focusedControl.X,
                (short)(focusedControl.Y + focusedControl.Height));
        }

        private bool TryOpenContextMenuFor(Control targetControl, short X, short Y)
        {
            foreach (ContextMenu contextMenu in controls
                .OfType<ContextMenu>()
                .Where(menu => menu.Visible)
                .OrderByDescending(menu => menu.ZOrder))
            {
                if (!contextMenu.IsAttachedTo(targetControl.Name))
                    continue;

                contextMenu.OpenAt(X, Y);
                return true;
            }

            return false;
        }

        private void CloseOpenContextMenus()
        {
            foreach (ContextMenu contextMenu in controls.OfType<ContextMenu>())
                contextMenu.Close();

            foreach (Container container in containers.Where(container => container.Visible))
                container.CloseOpenContextMenus();
        }

        public Container GetTopContainer() => TopContainer();

        protected string FormatAccessibilitySummary(string fallback)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

            string summary = string.IsNullOrWhiteSpace(ScreenReaderSummary)
                ? fallback.Trim()
                : ScreenReaderSummary.Trim();

            if (!string.IsNullOrWhiteSpace(ScreenReaderDescription))
                summary = $"{summary}. {ScreenReaderDescription.Trim()}";

            return summary;
        }

        public void BringToBottom(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);

            if (control != null)
            {
                control.ZOrder = 1;
                short z = 1;
                foreach (Control c in (from c in controls where c.name != control.name orderby c.ZOrder select c))
                {
                    c.ZOrder = ++z;
                }
            }
        }

        public void BringToFront(Container container)
        {
            ArgumentNullException.ThrowIfNull(container);

            if (container != null)
            {
                short z = 0;
                foreach (Container c in (from c in containers where c.name != container.name orderby c.ZOrder select c))
                {
                    c.ZOrder = ++z;
                }

                container.ZOrder = ++z;
            }
        }

        public void BringToBottom(Container container)
        {
            ArgumentNullException.ThrowIfNull(container);

            if (container != null)
            {
                container.ZOrder = 1;
                short z = 1;
                foreach (Container c in (from c in containers where c.name != container.name orderby c.ZOrder select c))
                {
                    c.ZOrder = ++z;
                }
            }
        }

        public virtual Control? GetCurrentFocusControl()
        {
            Control? ret = null;

            if (controls != null)
            {
                ret = (from c in controls where c.Focus == true select c).FirstOrDefault();
            }

            if (ret == null)
            {
                foreach (Container container in containers)
                {
                    ret = container.GetCurrentFocusControl();
                    if (ret != null)
                    {
                        break;  
                    }
                }
            }

            return ret;
        }

        public void FocusNextControl()
            => TryFocusNextControl();

        public bool TryFocusNextControl()
        {
            List<Control> focusableControls = GetFocusNavigationControls();
            if (focusableControls.Count == 0)
                return false;

            Container focusScope = GetFocusNavigationContainer();
            Control? currentFocusControl = focusScope.GetCurrentFocusControl();
            int currentIndex = currentFocusControl is null
                ? -1
                : focusableControls.FindIndex(control => control.name == currentFocusControl.name);

            Control nextControl = focusableControls[(currentIndex + 1) % focusableControls.Count];
            focusScope.SetFocus(nextControl.name);
            return true;
        }

        public void FocusPreviousControl()
            => TryFocusPreviousControl();

        public bool TryFocusPreviousControl()
        {
            List<Control> focusableControls = GetFocusNavigationControls();
            if (focusableControls.Count == 0)
                return false;

            Container focusScope = GetFocusNavigationContainer();
            Control? currentFocusControl = focusScope.GetCurrentFocusControl();
            int currentIndex = currentFocusControl is null
                ? 0
                : focusableControls.FindIndex(control => control.name == currentFocusControl.name);

            if (currentIndex < 0)
                currentIndex = 0;

            Control previousControl = focusableControls[(currentIndex - 1 + focusableControls.Count) % focusableControls.Count];
            focusScope.SetFocus(previousControl.name);
            return true;
        }

        public bool FocusFirstControl()
        {
            IReadOnlyList<Control> focusableControls = GetFocusableControlsInTabOrder();
            if (focusableControls.Count == 0)
                return false;

            TopContainer().SetFocus(focusableControls[0].Name);
            return true;
        }

        public bool FocusLastControl()
        {
            IReadOnlyList<Control> focusableControls = GetFocusableControlsInTabOrder();
            if (focusableControls.Count == 0)
                return false;

            TopContainer().SetFocus(focusableControls[^1].Name);
            return true;
        }

        public virtual IReadOnlyList<Control> GetFocusableControlsInTabOrder()
            => EnumerateFocusableControlsInTabOrder(this).ToList();

        private Container GetFocusNavigationContainer()
        {
            for (Container? current = this; current is not null; current = current.parent)
            {
                if (current.IsFocusScope)
                    return current;
            }

            return parent is SplitPanel splitPanel ? splitPanel : this;
        }

        private List<Control> GetFocusNavigationControls()
        {
            Container focusScope = GetFocusNavigationContainer();
            return focusScope.IsFocusScope || focusScope is SplitPanel
                ? focusScope.GetFocusableControlsInTabOrder().ToList()
                : controls
                    .Where(control => control.Visible && control.TabStop)
                    .OrderBy(control => control.TabIndex)
                    .ThenBy(control => control.name)
                    .ToList();
        }

        private static IEnumerable<Control> EnumerateFocusableControlsInTabOrder(Container container)
        {
            foreach (Control control in container.Controls
                .Where(control => control.Visible && control.TabStop)
                .OrderBy(control => control.TabIndex)
                .ThenBy(control => control.Name, StringComparer.Ordinal))
            {
                yield return control;
            }

            foreach (Container child in container.Containers
                .Where(child => child.Visible)
                .OrderBy(child => child.ZOrder)
                .ThenBy(child => child.Name, StringComparer.Ordinal))
            {
                foreach (Control control in child.GetFocusableControlsInTabOrder())
                    yield return control;
            }
        }

        public short YOffset()
        {
            if (this.parent != null)
                return (short)(this.Y + this.parent.YOffset());
            else
                return this.Y;  
        }

        public short GetYOffset() => YOffset();

        public short XOffset()
        {
            if (this.parent != null)
                return (short)(this.X + this.parent.XOffset());
            else
                return this.X;
        }

        public short GetXOffset() => XOffset();

        public virtual void Render(IList<Row> rows) {
            ArgumentNullException.ThrowIfNull(rows);

            CellSnapshot[][]? snapshots = parent is null ? null : CaptureCellSnapshots(rows);
            ClipBounds clipBounds = GetEffectiveClipBounds(rows);
        
            foreach (Control control in (from c in controls orderby c.ZOrder select c))
            {
                control.Render(rows);
                RenderValidationMessage(rows, control, clipBounds);
            }

            foreach (Container container in (from c in containers orderby c.ZOrder select c))
            {
                if (container.Visible)
                {
                    container.Render(rows);
                }
            }

            if (snapshots is not null)
                RestoreCellsOutsideClip(rows, snapshots, clipBounds);
        }

        private void ValidateInto(List<Control> invalidControls, string? validationGroup)
        {
            foreach (Control control in controls)
            {
                if (control.Visible &&
                    ShouldValidateControl(control, validationGroup) &&
                    !control.Validate())
                {
                    invalidControls.Add(control);
                }
            }

            foreach (Container container in containers.Where(container => container.Visible))
                container.ValidateInto(invalidControls, validationGroup);
        }

        private void AddInvalidControls(List<Control> invalidControls, string? validationGroup)
        {
            foreach (Control control in controls)
            {
                if (control.Visible &&
                    ShouldValidateControl(control, validationGroup) &&
                    !control.IsValid)
                {
                    invalidControls.Add(control);
                }
            }

            foreach (Container container in containers.Where(container => container.Visible))
                container.AddInvalidControls(invalidControls, validationGroup);
        }

        private static bool ShouldValidateControl(Control control, string? validationGroup)
            => validationGroup is null ||
                string.Equals(control.ValidationGroup, validationGroup, StringComparison.Ordinal);

        private void RenderValidationMessage(IList<Row> rows, Control control, ClipBounds clipBounds)
        {
            if (!control.Visible ||
                !control.ShowValidationMessage ||
                control.IsValid ||
                string.IsNullOrEmpty(control.ValidationMessage))
            {
                return;
            }

            int localMessageX = control.X + control.Width + 1;
            int localMessageY = control.Y;
            int messageX = XOffset() + localMessageX;
            int messageY = YOffset() + localMessageY;

            if (messageX >= clipBounds.Right)
            {
                localMessageX = control.X;
                localMessageY = control.Y + control.Height;
                messageX = XOffset() + localMessageX;
                messageY = YOffset() + localMessageY;
            }

            int availableWidth = clipBounds.Right - messageX;
            if (availableWidth <= 0 || messageY < clipBounds.Top || messageY >= clipBounds.Bottom)
                return;

            string message = TuiText.TruncateByVisualWidth(control.ValidationMessage, availableWidth);
            int messageWidth = TuiText.VisualWidth(message);
            for (int x = 0; x < messageWidth; x++)
            {
                int cellX = messageX + x;
                if (!clipBounds.Contains(cellX, messageY) ||
                    messageY < 0 ||
                    messageY >= rows.Count ||
                    cellX < 0 ||
                    cellX >= rows[messageY].Cells.Count)
                {
                    continue;
                }

                Cell cell = rows[messageY].Cells[cellX];
                cell.ForeColor = control.ValidationMessageForeColor;
                cell.BackgroundColor = control.ValidationMessageBackgroundColor;
                cell.Decoration = Cell.TextDecoration.None;
                cell.Character = TuiText.CellAt(message, x);
                cell.IsVisible = true;
                cell.BackgroundImage = "";
                cell.ScaleX = 1;
                cell.ScaleY = 1;
            }
        }

        private ClipBounds GetEffectiveClipBounds(IList<Row> rows)
        {
            int left = XOffset();
            int top = YOffset();
            int right = left + width;
            int bottom = top + height;

            for (Container? current = parent; current is not null; current = current.parent)
            {
                int currentLeft = current.XOffset();
                int currentTop = current.YOffset();

                left = Math.Max(left, currentLeft);
                top = Math.Max(top, currentTop);
                right = Math.Min(right, currentLeft + current.width);
                bottom = Math.Min(bottom, currentTop + current.height);
            }

            int screenWidth = rows.Count == 0 ? 0 : rows.Max(row => row.Cells.Count);
            left = Math.Clamp(left, 0, screenWidth);
            right = Math.Clamp(right, 0, screenWidth);
            top = Math.Clamp(top, 0, rows.Count);
            bottom = Math.Clamp(bottom, 0, rows.Count);

            if (right < left)
                right = left;
            if (bottom < top)
                bottom = top;

            return new ClipBounds(left, top, right, bottom);
        }

        private static CellSnapshot[][] CaptureCellSnapshots(IList<Row> rows)
        {
            var snapshots = new CellSnapshot[rows.Count][];
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                IList<Cell> cells = rows[rowIndex].Cells;
                snapshots[rowIndex] = new CellSnapshot[cells.Count];
                for (int cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                    snapshots[rowIndex][cellIndex] = new CellSnapshot(cells[cellIndex]);
            }

            return snapshots;
        }

        private static void RestoreCellsOutsideClip(IList<Row> rows, CellSnapshot[][] snapshots, ClipBounds clipBounds)
        {
            for (int rowIndex = 0; rowIndex < rows.Count && rowIndex < snapshots.Length; rowIndex++)
            {
                IList<Cell> cells = rows[rowIndex].Cells;
                CellSnapshot[] rowSnapshots = snapshots[rowIndex];
                for (int cellIndex = 0; cellIndex < cells.Count && cellIndex < rowSnapshots.Length; cellIndex++)
                {
                    if (!clipBounds.Contains(cellIndex, rowIndex))
                        rowSnapshots[cellIndex].Restore(cells[cellIndex]);
                }
            }
        }

        private readonly struct ClipBounds
        {
            public ClipBounds(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public int Left { get; }

            public int Top { get; }

            public int Right { get; }

            public int Bottom { get; }

            public bool Contains(int x, int y)
                => x >= Left && x < Right && y >= Top && y < Bottom;
        }

        private readonly struct CellSnapshot
        {
            private readonly System.Drawing.Color foreColor;
            private readonly System.Drawing.Color backgroundColor;
            private readonly Cell.TextDecoration textDecoration;
            private readonly string character;
            private readonly bool visible;
            private readonly string backgroundImage;
            private readonly double scaleX;
            private readonly double scaleY;

            public CellSnapshot(Cell cell)
            {
                foreColor = cell.ForeColor;
                backgroundColor = cell.BackgroundColor;
                textDecoration = cell.Decoration;
                character = cell.Character;
                visible = cell.IsVisible;
                backgroundImage = cell.BackgroundImage;
                scaleX = cell.ScaleX;
                scaleY = cell.ScaleY;
            }

            public void Restore(Cell cell)
            {
                cell.ForeColor = foreColor;
                cell.BackgroundColor = backgroundColor;
                cell.Decoration = textDecoration;
                cell.Character = character;
                cell.IsVisible = visible;
                cell.BackgroundImage = backgroundImage;
                cell.ScaleX = scaleX;
                cell.ScaleY = scaleY;
            }
        }
    }
}
