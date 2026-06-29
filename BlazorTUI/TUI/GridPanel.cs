using System.Drawing;

namespace BlazorTUI.TUI
{
    public class GridPanel : LayoutPanel
    {
        private readonly GridPanelLength[] columns;
        private readonly GridPanelLength[] rows;
        private readonly Dictionary<object, GridPanelPlacement> placements = new(ReferenceEqualityComparer.Instance);

        public GridPanel(
            string name,
            IEnumerable<GridPanelLength> columns,
            IEnumerable<GridPanelLength> rows,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
            : base(name, X, Y, width, height, foreColor, backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(rows);

            this.columns = columns.ToArray();
            this.rows = rows.ToArray();
            if (this.columns.Length == 0)
                throw new ArgumentException("GridPanel requires at least one column.", nameof(columns));
            if (this.rows.Length == 0)
                throw new ArgumentException("GridPanel requires at least one row.", nameof(rows));
        }

        public IReadOnlyList<GridPanelLength> Columns => columns;

        public IReadOnlyList<GridPanelLength> Rows => rows;

        public void AddControl(Control control, int row, int column, int rowSpan = 1, int columnSpan = 1)
        {
            AddControl(control);
            SetCell(control, row, column, rowSpan, columnSpan);
        }

        public void AddContainer(Container container, int row, int column, int rowSpan = 1, int columnSpan = 1)
        {
            AddContainer(container);
            SetCell(container, row, column, rowSpan, columnSpan);
        }

        public void SetCell(Control control, int row, int column, int rowSpan = 1, int columnSpan = 1)
        {
            ArgumentNullException.ThrowIfNull(control);
            if (!controls.Any(existing => ReferenceEquals(existing, control)))
                throw new ArgumentException("The control does not belong to this GridPanel.", nameof(control));

            placements[control] = ValidatePlacement(row, column, rowSpan, columnSpan);
        }

        public void SetCell(Container container, int row, int column, int rowSpan = 1, int columnSpan = 1)
        {
            ArgumentNullException.ThrowIfNull(container);
            if (!containers.Any(existing => ReferenceEquals(existing, container)))
                throw new ArgumentException("The container does not belong to this GridPanel.", nameof(container));

            placements[container] = ValidatePlacement(row, column, rowSpan, columnSpan);
        }

        protected override void ArrangeChildren()
        {
            int[] columnSizes = ResolveLengths(columns, axisLength: ContentWidth, forColumns: true);
            int[] rowSizes = ResolveLengths(rows, axisLength: ContentHeight, forColumns: false);
            int[] columnOffsets = BuildOffsets(columnSizes, ContentLeft);
            int[] rowOffsets = BuildOffsets(rowSizes, ContentTop);

            int defaultIndex = 0;
            foreach (LayoutChild child in GetLayoutChildren())
            {
                GridPanelPlacement placement = GetPlacement(child, defaultIndex++);
                int x = columnOffsets[placement.Column];
                int y = rowOffsets[placement.Row];
                int width = Sum(columnSizes, placement.Column, placement.ColumnSpan);
                int height = Sum(rowSizes, placement.Row, placement.RowSpan);
                SetChildBounds(child, x, y, width, height);
            }
        }

        private GridPanelPlacement GetPlacement(LayoutChild child, int defaultIndex)
        {
            if (placements.TryGetValue(child.Instance, out GridPanelPlacement placement))
                return placement;

            int row = Math.Min(rows.Length - 1, defaultIndex / columns.Length);
            int column = defaultIndex % columns.Length;
            return new GridPanelPlacement(row, column, 1, 1);
        }

        private GridPanelPlacement ValidatePlacement(int row, int column, int rowSpan, int columnSpan)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            ArgumentOutOfRangeException.ThrowIfLessThan(rowSpan, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(columnSpan, 1);
            if (row >= rows.Length)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (column >= columns.Length)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (row + rowSpan > rows.Length)
                throw new ArgumentOutOfRangeException(nameof(rowSpan));
            if (column + columnSpan > columns.Length)
                throw new ArgumentOutOfRangeException(nameof(columnSpan));

            return new GridPanelPlacement(row, column, rowSpan, columnSpan);
        }

        private int[] ResolveLengths(IReadOnlyList<GridPanelLength> definitions, int axisLength, bool forColumns)
        {
            var sizes = new int[definitions.Count];
            int used = 0;
            int starWeight = 0;

            for (int index = 0; index < definitions.Count; index++)
            {
                GridPanelLength definition = definitions[index];
                switch (definition.UnitType)
                {
                    case GridPanelUnitType.Fixed:
                        sizes[index] = definition.Value;
                        used += sizes[index];
                        break;
                    case GridPanelUnitType.Auto:
                        sizes[index] = MeasureAuto(index, forColumns);
                        used += sizes[index];
                        break;
                    case GridPanelUnitType.Star:
                        starWeight += definition.Value;
                        break;
                }
            }

            int remaining = Math.Max(0, axisLength - used);
            int assignedStar = 0;
            for (int index = 0; index < definitions.Count; index++)
            {
                GridPanelLength definition = definitions[index];
                if (definition.UnitType != GridPanelUnitType.Star)
                    continue;

                int size = starWeight == 0
                    ? 1
                    : Math.Max(1, remaining * definition.Value / starWeight);
                sizes[index] = size;
                assignedStar += size;
            }

            if (assignedStar > 0 && used + assignedStar < axisLength)
            {
                for (int index = definitions.Count - 1; index >= 0; index--)
                {
                    if (definitions[index].UnitType == GridPanelUnitType.Star)
                    {
                        sizes[index] += axisLength - used - assignedStar;
                        break;
                    }
                }
            }

            return sizes;
        }

        private int MeasureAuto(int definitionIndex, bool forColumns)
        {
            int size = 1;
            int defaultIndex = 0;
            foreach (LayoutChild child in GetLayoutChildren())
            {
                GridPanelPlacement placement = GetPlacement(child, defaultIndex++);
                if (forColumns)
                {
                    if (placement.Column == definitionIndex && placement.ColumnSpan == 1)
                        size = Math.Max(size, GetChildWidth(child));
                }
                else if (placement.Row == definitionIndex && placement.RowSpan == 1)
                {
                    size = Math.Max(size, GetChildHeight(child));
                }
            }

            return size;
        }

        private static int[] BuildOffsets(IReadOnlyList<int> sizes, int start)
        {
            var offsets = new int[sizes.Count];
            int cursor = start;
            for (int index = 0; index < sizes.Count; index++)
            {
                offsets[index] = cursor;
                cursor += sizes[index];
            }

            return offsets;
        }

        private static int Sum(IReadOnlyList<int> sizes, int start, int count)
        {
            int total = 0;
            for (int index = start; index < start + count && index < sizes.Count; index++)
                total += sizes[index];
            return Math.Max(1, total);
        }

        private readonly record struct GridPanelPlacement(
            int Row,
            int Column,
            int RowSpan,
            int ColumnSpan);
    }
}
