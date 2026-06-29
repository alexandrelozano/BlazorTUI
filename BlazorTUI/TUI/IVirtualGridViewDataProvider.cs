namespace BlazorTUI.TUI
{
    public interface IVirtualGridViewDataProvider
    {
        int Count { get; }

        bool CanUpdate { get; }

        GridView.GridRow GetRow(int index);

        string GetRowKey(int index);

        void SetCellValue(int rowIndex, int columnIndex, string value);
    }
}
