using System.Globalization;

namespace BlazorTUI.TUI
{
    public sealed class VirtualGridViewDataProvider : IVirtualGridViewDataProvider
    {
        private readonly Func<int, GridView.GridRow> getRow;
        private readonly Func<int, string>? getRowKey;
        private readonly Action<int, int, string>? setCellValue;

        public VirtualGridViewDataProvider(
            int count,
            Func<int, GridView.GridRow> getRow,
            Func<int, string>? getRowKey = null,
            Action<int, int, string>? setCellValue = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentNullException.ThrowIfNull(getRow);

            Count = count;
            this.getRow = getRow;
            this.getRowKey = getRowKey;
            this.setCellValue = setCellValue;
        }

        public int Count { get; }

        public bool CanUpdate => setCellValue is not null;

        public GridView.GridRow GetRow(int index)
        {
            ValidateIndex(index);
            return getRow(index) ?? throw new InvalidOperationException("The virtual GridView row provider returned null.");
        }

        public string GetRowKey(int index)
        {
            ValidateIndex(index);
            return getRowKey?.Invoke(index) ?? index.ToString(CultureInfo.InvariantCulture);
        }

        public void SetCellValue(int rowIndex, int columnIndex, string value)
        {
            if (setCellValue is null)
                throw new NotSupportedException("This virtual GridView data provider is read-only.");

            ValidateIndex(rowIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
            setCellValue(rowIndex, columnIndex, value ?? "");
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
