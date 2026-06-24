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
            if (controls != null && (from c in controls where c.TabStop==true select c).Count() > 0)
            {
                Control? currentFocusControl = GetCurrentFocusControl();
                short currentTabIndex = 0;
                string currentName = "";

                if (currentFocusControl != null)
                {
                    currentTabIndex = currentFocusControl.TabIndex;
                    currentName = currentFocusControl.name;
                }

                Control? nextControl = (from c in controls where c.TabStop == true && c.TabIndex >= currentTabIndex && c.name != currentName orderby c.TabIndex, c.name select c).FirstOrDefault();

                if (nextControl == null)
                {
                    nextControl = (from c in controls where c.TabStop == true orderby c.TabIndex, c.name select c).First();
                }

                SetFocus(nextControl.name);
            }
        }

        public void FocusPreviousControl()
        {
            if (controls != null && (from c in controls where c.TabStop == true select c).Count() > 0)
            {
                Control? currentFocusControl = GetCurrentFocusControl();
                short currentTabIndex = 0;
                string currentName = "";

                if (currentFocusControl != null)
                {
                    currentTabIndex = currentFocusControl.TabIndex;
                    currentName = currentFocusControl.name;
                }

                Control? nextControl = (from c in controls where c.TabStop == true && c.TabIndex <= currentTabIndex && c.name != currentName orderby c.TabIndex descending, c.name descending select c).FirstOrDefault();

                if (nextControl == null)
                {
                    nextControl = (from c in controls where c.TabStop == true orderby c.TabIndex, c.name select c).Last();
                }

                SetFocus(nextControl.name);
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
        }
    }
}
