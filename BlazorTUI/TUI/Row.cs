using System.Collections.Generic;

namespace BlazorTUI.TUI
{
    public class Row
    {
        public short y { get; set; }
        public IList<Cell> Cells { get; set; }
    }
}
