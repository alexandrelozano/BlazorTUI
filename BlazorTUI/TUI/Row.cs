using System.Collections.Generic;
using System.Drawing;

namespace BlazorTUI.TUI
{
    public class Row
    {
        private CellState[] _renderedCells = Array.Empty<CellState>();

        internal short y { get; set; }

        public short Y { get => y; set => y = value; }

        public IList<Cell> Cells { get; set; } = new List<Cell>();

        public IReadOnlyList<Cell> ReadOnlyCells => Cells as IReadOnlyList<Cell> ?? Cells.ToArray();

        internal long Revision { get; private set; }

        internal void CaptureChanges()
        {
            bool changed = _renderedCells.Length != Cells.Count;

            if (changed)
                _renderedCells = new CellState[Cells.Count];

            for (int index = 0; index < Cells.Count; index++)
            {
                CellState state = CellState.From(Cells[index]);
                if (_renderedCells[index] != state)
                    changed = true;

                _renderedCells[index] = state;
            }

            if (changed)
                Revision++;
        }

        private readonly record struct CellState(
            Cell Cell,
            short X,
            short Y,
            Color ForeColor,
            Color BackgroundColor,
            Cell.TextDecoration TextDecoration,
            string Character,
            bool Visible,
            string BackgroundImage,
            double ScaleX,
            double ScaleY)
        {
            internal static CellState From(Cell cell)
                => new(
                    cell,
                    cell.x,
                    cell.y,
                    cell.foreColor,
                    cell.backgroundColor,
                    cell.textDecoration,
                    cell.character,
                    cell.visible,
                    cell.backgroundImage,
                    cell.scaleX,
                    cell.scaleY);
        }
    }
}
