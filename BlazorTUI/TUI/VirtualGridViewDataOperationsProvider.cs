using System.Globalization;

namespace BlazorTUI.TUI
{
    public sealed class VirtualGridViewDataOperationsProvider : IVirtualGridViewDataOperationsProvider
    {
        private readonly Func<int> getCount;
        private readonly Func<int, GridView.GridRow> getRow;
        private readonly Func<int, string>? getRowKey;
        private readonly Action<int, int, string>? setCellValue;
        private readonly Func<VirtualGridViewQuery, int> getViewCount;
        private readonly Func<VirtualGridViewQuery, int, GridView.GridRow> getViewRow;
        private readonly Func<VirtualGridViewQuery, int, string>? getViewRowKey;
        private readonly Func<VirtualGridViewQuery, int, int>? getSourceIndex;
        private readonly Func<VirtualGridViewQuery, int, int>? findViewIndexBySourceIndex;
        private readonly Func<VirtualGridViewQuery, string, int>? findViewIndexByKey;
        private readonly Action<VirtualGridViewQuery>? applyQuery;
        private readonly Func<VirtualGridViewQuery, CancellationToken, Task>? applyQueryAsync;

        public VirtualGridViewDataOperationsProvider(
            int count,
            Func<int, GridView.GridRow> getRow,
            Func<VirtualGridViewQuery, int> getViewCount,
            Func<VirtualGridViewQuery, int, GridView.GridRow> getViewRow,
            Func<int, string>? getRowKey = null,
            Action<int, int, string>? setCellValue = null,
            Func<VirtualGridViewQuery, int, string>? getViewRowKey = null,
            Func<VirtualGridViewQuery, int, int>? getSourceIndex = null,
            Func<VirtualGridViewQuery, int, int>? findViewIndexBySourceIndex = null,
            Func<VirtualGridViewQuery, string, int>? findViewIndexByKey = null,
            Action<VirtualGridViewQuery>? applyQuery = null,
            Func<VirtualGridViewQuery, CancellationToken, Task>? applyQueryAsync = null)
            : this(
                () => count,
                getRow,
                getViewCount,
                getViewRow,
                getRowKey,
                setCellValue,
                getViewRowKey,
                getSourceIndex,
                findViewIndexBySourceIndex,
                findViewIndexByKey,
                applyQuery,
                applyQueryAsync)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
        }

        public VirtualGridViewDataOperationsProvider(
            Func<int> getCount,
            Func<int, GridView.GridRow> getRow,
            Func<VirtualGridViewQuery, int> getViewCount,
            Func<VirtualGridViewQuery, int, GridView.GridRow> getViewRow,
            Func<int, string>? getRowKey = null,
            Action<int, int, string>? setCellValue = null,
            Func<VirtualGridViewQuery, int, string>? getViewRowKey = null,
            Func<VirtualGridViewQuery, int, int>? getSourceIndex = null,
            Func<VirtualGridViewQuery, int, int>? findViewIndexBySourceIndex = null,
            Func<VirtualGridViewQuery, string, int>? findViewIndexByKey = null,
            Action<VirtualGridViewQuery>? applyQuery = null,
            Func<VirtualGridViewQuery, CancellationToken, Task>? applyQueryAsync = null)
        {
            ArgumentNullException.ThrowIfNull(getCount);
            ArgumentNullException.ThrowIfNull(getRow);
            ArgumentNullException.ThrowIfNull(getViewCount);
            ArgumentNullException.ThrowIfNull(getViewRow);

            this.getCount = getCount;
            this.getRow = getRow;
            this.getRowKey = getRowKey;
            this.setCellValue = setCellValue;
            this.getViewCount = getViewCount;
            this.getViewRow = getViewRow;
            this.getViewRowKey = getViewRowKey;
            this.getSourceIndex = getSourceIndex;
            this.findViewIndexBySourceIndex = findViewIndexBySourceIndex;
            this.findViewIndexByKey = findViewIndexByKey;
            this.applyQuery = applyQuery;
            this.applyQueryAsync = applyQueryAsync;
        }

        public VirtualGridViewQuery CurrentQuery { get; private set; } = VirtualGridViewQuery.Empty;

        public int Count => Math.Max(0, getCount());

        public int ViewCount => Math.Max(0, getViewCount(CurrentQuery));

        public bool CanUpdate => setCellValue is not null;

        public void ApplyQuery(VirtualGridViewQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);
            CurrentQuery = query;
            applyQuery?.Invoke(query);
        }

        public async Task ApplyQueryAsync(VirtualGridViewQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            cancellationToken.ThrowIfCancellationRequested();
            CurrentQuery = query;
            if (applyQueryAsync is not null)
                await applyQueryAsync(query, cancellationToken).ConfigureAwait(false);
            else
                applyQuery?.Invoke(query);
        }

        public GridView.GridRow GetRow(int index)
        {
            ValidateSourceIndex(index);
            return getRow(index) ?? throw new InvalidOperationException("The virtual GridView row provider returned null.");
        }

        public string GetRowKey(int index)
        {
            ValidateSourceIndex(index);
            return getRowKey?.Invoke(index) ?? index.ToString(CultureInfo.InvariantCulture);
        }

        public void SetCellValue(int rowIndex, int columnIndex, string value)
        {
            if (setCellValue is null)
                throw new NotSupportedException("This virtual GridView data provider is read-only.");

            ValidateSourceIndex(rowIndex);
            ArgumentOutOfRangeException.ThrowIfNegative(columnIndex);
            setCellValue(rowIndex, columnIndex, value ?? "");
        }

        public GridView.GridRow GetViewRow(int viewIndex)
        {
            ValidateViewIndex(viewIndex);
            return getViewRow(CurrentQuery, viewIndex) ??
                throw new InvalidOperationException("The virtual GridView view provider returned null.");
        }

        public string GetViewRowKey(int viewIndex)
        {
            ValidateViewIndex(viewIndex);
            return getViewRowKey?.Invoke(CurrentQuery, viewIndex) ?? GetRowKey(GetSourceIndex(viewIndex));
        }

        public int GetSourceIndex(int viewIndex)
        {
            ValidateViewIndex(viewIndex);
            int sourceIndex = getSourceIndex?.Invoke(CurrentQuery, viewIndex) ?? viewIndex;
            ValidateSourceIndex(sourceIndex);
            return sourceIndex;
        }

        public int FindViewIndexBySourceIndex(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= Count)
                return -1;

            if (findViewIndexBySourceIndex is not null)
                return findViewIndexBySourceIndex(CurrentQuery, sourceIndex);

            for (int index = 0; index < ViewCount; index++)
            {
                if (GetSourceIndex(index) == sourceIndex)
                    return index;
            }

            return -1;
        }

        public int FindViewIndexByKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (findViewIndexByKey is not null)
                return findViewIndexByKey(CurrentQuery, key);

            for (int index = 0; index < ViewCount; index++)
            {
                if (GetViewRowKey(index) == key)
                    return index;
            }

            return -1;
        }

        private void ValidateSourceIndex(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
        }

        private void ValidateViewIndex(int index)
        {
            if (index < 0 || index >= ViewCount)
                throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
