using System.Linq;

namespace BlazorTUI.TUI
{
    public class Screen
    {
        private Row[] _renderedRows = Array.Empty<Row>();
        private long[] _renderedRowRevisions = Array.Empty<long>();

        public short width { get; set; }

        public short height { get; set; }

        public IList<Row> rows;

        public Container topContainer { get; set; }

        public IList<Dialog> dialogs;

        public MenuBar? menuBar;

        public long Revision { get; private set; }

        public Screen(short width, short height)
        {
            this.width = width;
            this.height = height;

            dialogs = new List<Dialog>();   

            rows = new List<Row>();

            for (short y = 0; y < height; y++)
            {
                Row row = new Row();
                var cells = new List<Cell>(width);
                for (short x = 0; x < width; x++)
                {
                    Cell cell = new Cell();
                    cell.x = x;
                    cell.y = y;
                    cell.character = " ";
                    cell.foreColor = System.Drawing.Color.Yellow;
                    cell.backgroundColor = System.Drawing.Color.Blue;
                    cell.textDecoration = Cell.TextDecoration.None;
                    cell.visible = true;
                    cell.scaleX = 1;
                    cell.scaleY = 1;    
                    cells.Add(cell);
                }
                row.Cells = cells;
                rows.Add(row);
            }

            topContainer = new Frame("TopContainer", "", 0, 0, width, height, Frame.BorderStyle.none, System.Drawing.Color.Yellow, System.Drawing.Color.Blue);

            this.topContainer = topContainer;

            CaptureRenderedRows(incrementRevision: false);
        }

        public void SetFocus(string name)
        {
            if (dialogs.Count == 0)
                topContainer.SetFocus(name);
            else
                dialogs.ElementAt(dialogs.Count - 1).SetFocus(name);
        }

        public void KeyDown(string key, bool shiftKey)
        {
            MenuBar? activeMenuBar = menuBar;

            if (key == "Alt" && activeMenuBar != null)
                activeMenuBar.showShortCutkeys = !activeMenuBar.showShortCutkeys;
            if (activeMenuBar != null && (activeMenuBar.showShortCutkeys || activeMenuBar.OpenedMenu() != null))
                activeMenuBar.KeyDown(key, shiftKey);
            else if (dialogs.Count == 0)
                topContainer.KeyDown(key, shiftKey);
            else
                dialogs.ElementAt(dialogs.Count - 1).KeyDown(key, shiftKey);    
        }

        public void Render()
        {
            topContainer.Render(rows);

            foreach(Dialog dialog in dialogs)
            {
                dialog.Render(rows);
            }

            menuBar?.Render(rows);

            CaptureRenderedRows(incrementRevision: true);
        }

        private void CaptureRenderedRows(bool incrementRevision)
        {
            bool changed = _renderedRows.Length != rows.Count;

            if (changed)
            {
                _renderedRows = new Row[rows.Count];
                _renderedRowRevisions = new long[rows.Count];
            }

            for (int index = 0; index < rows.Count; index++)
            {
                Row row = rows[index];
                row.CaptureChanges();
                long rowRevision = row.Revision;

                if (!ReferenceEquals(_renderedRows[index], row) || _renderedRowRevisions[index] != rowRevision)
                    changed = true;

                _renderedRows[index] = row;
                _renderedRowRevisions[index] = rowRevision;
            }

            if (changed && incrementRevision)
                Revision++;
        }
    }
}
