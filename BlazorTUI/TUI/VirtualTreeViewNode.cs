namespace BlazorTUI.TUI
{
    public sealed class VirtualTreeViewNode
    {
        public VirtualTreeViewNode(
            string key,
            string text,
            int depth,
            bool hasChildren = false,
            bool isExpanded = false,
            string? parentKey = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(text);
            ArgumentOutOfRangeException.ThrowIfNegative(depth);

            Key = key;
            Text = text;
            Depth = depth;
            HasChildren = hasChildren;
            IsExpanded = isExpanded;
            ParentKey = parentKey;
        }

        public string Key { get; }

        public string Text { get; }

        public int Depth { get; }

        public bool HasChildren { get; }

        public bool IsExpanded { get; }

        public string? ParentKey { get; }
    }
}
