using System.Linq;

namespace BlazorTUI.TUI
{
    public class Screen
    {
        public short width { get; set; }

        public short height { get; set; }

        public IList<Row> rows;

        public Container topContainer { get; set; }

        public IList<Dialog> dialogs;

        public Screen(short width, short height)
        {
            this.width = width;
            this.height = height;

            dialogs = new List<Dialog>();   

            rows = new List<Row>();

            for (short y = 0; y < height; y++)
            {
                Row row = new Row();
                row.Cells = new List<Cell>();
                for (short x = 0; x < width; x++)
                {
                    Cell cell = new Cell();
                    cell.x = x;
                    cell.y = y;
                    cell.character = " ";
                    cell.foreColor = System.Drawing.Color.Yellow;
                    cell.backgroundColor = System.Drawing.Color.Blue;
                    row.Cells.Add(cell);
                }
                rows.Add(row);
            }

            topContainer = new Frame("TopContainer", "", 0, 0, width, height, Frame.BorderStyle.none, System.Drawing.Color.Yellow, System.Drawing.Color.Blue);

            this.topContainer = topContainer;
        }

        public void SetFocus(string name)
        {
            topContainer.SetFocus(name);
        }

        public void KeyDown(string key, bool shiftKey)
        {
            if (dialogs.Count == 0)
                topContainer.KeyDown(key, shiftKey);
            else
                dialogs.ElementAt(dialogs.Count-1).KeyDown(key, shiftKey);    
        }

        public void Render()
        {
            topContainer.Render(rows);

            foreach(Dialog dialog in dialogs)
            {
                dialog.Render(rows);
            }
        }
    }
}