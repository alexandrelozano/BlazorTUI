using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorTUI.TUI
{
    public abstract class Control
    {
        public string name { get; set; }

        public Container container { get; set; }

        public short X {  get; set; }

        public short Y { get; set; }

        public short width { get; set; }

        public short height { get; set; }

        public Color foreColor { get; set; }

        public Color backgroundColor { get; set; }

        private bool focus;

        public bool Focus {
            get
            {
                return this.focus;
            }
            set
            {
                if (value != this.focus)
                {
                    if (value == true)
                    {
                        this.focus = value;
                        if (OnFocus != null)
                            OnFocus.Invoke();
                    }
                    else
                    {
                        this.focus = value;
                        if (OnLostFocus != null)
                            OnLostFocus.Invoke();
                    }
                }
            }
        }
        
        public bool TabStop { get; set; }

        public short TabIndex { get; set; }

        public bool Visible { get; set; } = true;

        public short ZOrder { get; set; }

        abstract public void Render(IList<Row> rows);

        public virtual bool KeyDown(string key, bool shiftKey) { return false; }

        public virtual bool Click(short X, short Y) { return false; }

        public Action<Control> OnClick;

        public Action OnFocus;

        public Action OnLostFocus;

    }
}