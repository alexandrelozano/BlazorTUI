namespace BlazorTUI.TUI
{
    public sealed class VirtualTreeViewDataProvider : IVirtualTreeViewDataOperationsProvider
    {
        private readonly Func<int> getVisibleCount;
        private readonly Func<int, VirtualTreeViewNode> getVisibleNode;
        private readonly Func<string, int>? findVisibleIndexByKey;
        private readonly Action<string, bool>? setExpanded;
        private readonly Action<VirtualTreeViewQuery>? applyQuery;
        private readonly Func<VirtualTreeViewQuery, CancellationToken, Task>? applyQueryAsync;

        public VirtualTreeViewDataProvider(
            Func<int> getVisibleCount,
            Func<int, VirtualTreeViewNode> getVisibleNode,
            Func<string, int>? findVisibleIndexByKey = null,
            Action<string, bool>? setExpanded = null,
            Action<VirtualTreeViewQuery>? applyQuery = null,
            Func<VirtualTreeViewQuery, CancellationToken, Task>? applyQueryAsync = null)
        {
            ArgumentNullException.ThrowIfNull(getVisibleCount);
            ArgumentNullException.ThrowIfNull(getVisibleNode);

            this.getVisibleCount = getVisibleCount;
            this.getVisibleNode = getVisibleNode;
            this.findVisibleIndexByKey = findVisibleIndexByKey;
            this.setExpanded = setExpanded;
            this.applyQuery = applyQuery;
            this.applyQueryAsync = applyQueryAsync;
        }

        public VirtualTreeViewQuery CurrentQuery { get; private set; } = VirtualTreeViewQuery.Empty;

        public int VisibleCount => Math.Max(0, getVisibleCount());

        public void ApplyQuery(VirtualTreeViewQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);
            CurrentQuery = query;
            applyQuery?.Invoke(query);
        }

        public async Task ApplyQueryAsync(VirtualTreeViewQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            cancellationToken.ThrowIfCancellationRequested();
            CurrentQuery = query;
            if (applyQueryAsync is not null)
                await applyQueryAsync(query, cancellationToken).ConfigureAwait(false);
            else
                applyQuery?.Invoke(query);
        }

        public VirtualTreeViewNode GetVisibleNode(int visibleIndex)
        {
            if (visibleIndex < 0 || visibleIndex >= VisibleCount)
                throw new ArgumentOutOfRangeException(nameof(visibleIndex));

            return getVisibleNode(visibleIndex) ??
                throw new InvalidOperationException("The virtual TreeView node provider returned null.");
        }

        public int FindVisibleIndexByKey(string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (findVisibleIndexByKey is not null)
                return findVisibleIndexByKey(key);

            for (int index = 0; index < VisibleCount; index++)
            {
                if (GetVisibleNode(index).Key == key)
                    return index;
            }

            return -1;
        }

        public void SetExpanded(string key, bool expanded)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            setExpanded?.Invoke(key, expanded);
        }
    }
}
