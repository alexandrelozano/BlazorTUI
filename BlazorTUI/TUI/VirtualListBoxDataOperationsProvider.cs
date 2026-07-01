using System.Globalization;

namespace BlazorTUI.TUI
{
    public sealed class VirtualListBoxDataOperationsProvider : IVirtualListBoxDataOperationsProvider
    {
        private readonly Func<int> getCount;
        private readonly Func<int, string> getItem;
        private readonly Func<int, string>? getKey;
        private readonly Func<VirtualListBoxQuery, int> getViewCount;
        private readonly Func<VirtualListBoxQuery, int, string> getViewItem;
        private readonly Func<VirtualListBoxQuery, int, string>? getViewKey;
        private readonly Func<VirtualListBoxQuery, string, int>? findViewIndexByKey;
        private readonly Action<VirtualListBoxQuery>? applyQuery;
        private readonly Func<VirtualListBoxQuery, CancellationToken, Task>? applyQueryAsync;

        public VirtualListBoxDataOperationsProvider(
            int count,
            Func<int, string> getItem,
            Func<VirtualListBoxQuery, int> getViewCount,
            Func<VirtualListBoxQuery, int, string> getViewItem,
            Func<int, string>? getKey = null,
            Func<VirtualListBoxQuery, int, string>? getViewKey = null,
            Func<VirtualListBoxQuery, string, int>? findViewIndexByKey = null,
            Action<VirtualListBoxQuery>? applyQuery = null,
            Func<VirtualListBoxQuery, CancellationToken, Task>? applyQueryAsync = null)
            : this(
                () => count,
                getItem,
                getViewCount,
                getViewItem,
                getKey,
                getViewKey,
                findViewIndexByKey,
                applyQuery,
                applyQueryAsync)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
        }

        public VirtualListBoxDataOperationsProvider(
            Func<int> getCount,
            Func<int, string> getItem,
            Func<VirtualListBoxQuery, int> getViewCount,
            Func<VirtualListBoxQuery, int, string> getViewItem,
            Func<int, string>? getKey = null,
            Func<VirtualListBoxQuery, int, string>? getViewKey = null,
            Func<VirtualListBoxQuery, string, int>? findViewIndexByKey = null,
            Action<VirtualListBoxQuery>? applyQuery = null,
            Func<VirtualListBoxQuery, CancellationToken, Task>? applyQueryAsync = null)
        {
            ArgumentNullException.ThrowIfNull(getCount);
            ArgumentNullException.ThrowIfNull(getItem);
            ArgumentNullException.ThrowIfNull(getViewCount);
            ArgumentNullException.ThrowIfNull(getViewItem);

            this.getCount = getCount;
            this.getItem = getItem;
            this.getKey = getKey;
            this.getViewCount = getViewCount;
            this.getViewItem = getViewItem;
            this.getViewKey = getViewKey;
            this.findViewIndexByKey = findViewIndexByKey;
            this.applyQuery = applyQuery;
            this.applyQueryAsync = applyQueryAsync;
        }

        public VirtualListBoxQuery CurrentQuery { get; private set; } = VirtualListBoxQuery.Empty;

        public int Count => Math.Max(0, getCount());

        public int ViewCount => Math.Max(0, getViewCount(CurrentQuery));

        public void ApplyQuery(VirtualListBoxQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);
            CurrentQuery = query;
            applyQuery?.Invoke(query);
        }

        public async Task ApplyQueryAsync(VirtualListBoxQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            cancellationToken.ThrowIfCancellationRequested();
            CurrentQuery = query;
            if (applyQueryAsync is not null)
                await applyQueryAsync(query, cancellationToken).ConfigureAwait(false);
            else
                applyQuery?.Invoke(query);
        }

        public string GetItem(int index)
        {
            ValidateSourceIndex(index);
            return getItem(index) ?? "";
        }

        public string GetKey(int index)
        {
            ValidateSourceIndex(index);
            return getKey?.Invoke(index) ?? index.ToString(CultureInfo.InvariantCulture);
        }

        public string GetViewItem(int viewIndex)
        {
            ValidateViewIndex(viewIndex);
            return getViewItem(CurrentQuery, viewIndex) ?? "";
        }

        public string GetViewKey(int viewIndex)
        {
            ValidateViewIndex(viewIndex);
            return getViewKey?.Invoke(CurrentQuery, viewIndex) ?? viewIndex.ToString(CultureInfo.InvariantCulture);
        }

        public int FindViewIndexByKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (findViewIndexByKey is not null)
                return findViewIndexByKey(CurrentQuery, key);

            for (int index = 0; index < ViewCount; index++)
            {
                if (GetViewKey(index) == key)
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
