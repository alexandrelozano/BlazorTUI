using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlazorTUI.TUI
{
    public class Container
    {
        public string name { get; set; } = "";

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

        public short width { get; set; }

        public short Width { get => width; set => width = value; }

        public short height { get; set; }

        public short Height { get => height; set => height = value; }

        public bool Visible { get; set; } = true;

        public short ZOrder { get; set; }

        public List<Control> controls { get; set; } = new List<Control>();

        public IReadOnlyList<Control> Controls => controls;

        public List<Container> containers { get; set; } = new List<Container>();

        public IReadOnlyList<Container> Containers => containers;

        public Container? parent { get; set; } = null;

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

        public virtual void KeyDown(string key, bool shiftKey)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);

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

        public Container GetTopContainer() => TopContainer();

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
        {
            List<Control> focusableControls = GetFocusScopeControls();
            if (focusableControls.Count > 0)
            {
                Container focusScope = GetFocusScopeContainer();
                Control? currentFocusControl = focusScope.GetCurrentFocusControl();
                int currentIndex = currentFocusControl is null
                    ? -1
                    : focusableControls.FindIndex(control => control.name == currentFocusControl.name);

                Control nextControl = focusableControls[(currentIndex + 1) % focusableControls.Count];

                focusScope.SetFocus(nextControl.name);
            }
        }

        public void FocusPreviousControl()
        {
            List<Control> focusableControls = GetFocusScopeControls();
            if (focusableControls.Count > 0)
            {
                Container focusScope = GetFocusScopeContainer();
                Control? currentFocusControl = focusScope.GetCurrentFocusControl();
                int currentIndex = currentFocusControl is null
                    ? 0
                    : focusableControls.FindIndex(control => control.name == currentFocusControl.name);

                if (currentIndex < 0)
                    currentIndex = 0;

                Control previousControl = focusableControls[(currentIndex - 1 + focusableControls.Count) % focusableControls.Count];

                focusScope.SetFocus(previousControl.name);
            }
        }

        private Container GetFocusScopeContainer()
            => parent is SplitPanel splitPanel ? splitPanel : this;

        private List<Control> GetFocusScopeControls()
            => parent is SplitPanel splitPanel
                ? splitPanel.GetFocusableControlsInTabOrder().ToList()
                : controls
                    .Where(control => control.TabStop)
                    .OrderBy(control => control.TabIndex)
                    .ThenBy(control => control.name)
                    .ToList();

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

            private int Left { get; }

            private int Top { get; }

            private int Right { get; }

            private int Bottom { get; }

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
