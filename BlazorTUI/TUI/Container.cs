using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlazorTUI.TUI
{
    public class Container
    {
        public string name { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        public short width { get; set; }

        public short height { get; set; }

        public bool Visible { get; set; } = true;

        public short ZOrder { get; set; }

        public List<Control> controls { get; set; } = new List<Control>();

        public List<Container> containers { get; set; } = new List<Container>();

        public Container? parent { get; set; } = null;

        public Container(string name)
        {
            this.name = name;
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

        public void SetFocus(string name)
        {
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

        public void Click(short X, short Y)
        {
            foreach (Control control in controls)
            {
                if (control.Visible && control.X <= X && control.X + control.width >= X && control.Y <= Y && control.Y + control.height >= Y)
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

        public void KeyDown(string key, bool shiftKey)
        {
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

                foreach (Container container in containers)
                {
                    container.KeyDown(key, shiftKey);
                }
            }
        }

        public void AddControl(Control control)
        {
            if (String.IsNullOrWhiteSpace(control.name))
            {
                throw new Exception($"Control must have a name");
            }

            if (TopContainer().GetControl(control.name)!=null)
            {
                throw new Exception($"Already exists a control with name {control.name}");
            }

            controls.Add(control);
            control.container = this;

            if (control.TabIndex == 0)
            {
                control.TabIndex = (short)((from p in controls where p.TabStop == true select p).Count() + 1);
            }

            control.ZOrder = (short)((from p in controls orderby p.ZOrder descending select p.ZOrder).FirstOrDefault() + 1);
        }

        public void AddContainer(Container container)
        {
            containers.Add(container);
            container.parent = this;
            container.ZOrder = (short)((from p in containers orderby p.ZOrder descending select p.ZOrder).FirstOrDefault() + 1);
        }

        public Control? GetControl(string name)
        {
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
            if (control != null)
            {
                control.ZOrder = 1;

                short z = 1;
                foreach(Control c in (from c in controls where c.name != control.name orderby c.ZOrder select c))
                {
                    z++;
                    c.ZOrder = z;
                }
            }
        }

        public void BringToBottom(Control control)
        {
            if (control != null)
            {
                control.ZOrder = (from c in controls orderby c.ZOrder descending select c.ZOrder).First();

                short z = 0;
                foreach (Control c in (from c in controls where c.name != control.name orderby c.ZOrder select c))
                {
                    z++;
                    c.ZOrder = z;
                }
            }
        }

        public void BringToFront(Container container)
        {
            if (container != null)
            {
                container.ZOrder = 1;

                short z = 1;
                foreach (Container c in (from c in containers where c.name != container.name orderby c.ZOrder select c))
                {
                    z++;
                    c.ZOrder = z;
                }
            }
        }

        public void BringToBottom(Container container)
        {
            if (container != null)
            {
                container.ZOrder = (from c in controls orderby c.ZOrder descending select c.ZOrder).First();

                short z = 0;
                foreach (Container c in (from c in containers where c.name != container.name orderby c.ZOrder select c))
                {
                    z++;
                    c.ZOrder = z;
                }
            }
        }

        public Control? GetCurrentFocusControl()
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
            if (name == "dlgConfirm")
            {
                string a = "a";
            }

            if (this.parent != null)
                return (short)(this.Y + this.parent.YOffset());
            else
                return this.Y;  
        }

        public short XOffset()
        {
            if (this.parent != null)
                return (short)(this.X + this.parent.XOffset());
            else
                return this.X;
        }

        public virtual void Render(IList<Row> rows) { 
        
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