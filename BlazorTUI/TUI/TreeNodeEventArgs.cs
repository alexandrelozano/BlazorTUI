namespace BlazorTUI.TUI
{
    public sealed class TreeNodeEventArgs : EventArgs
    {
        public TreeNode Node { get; }

        public TreeNodeEventArgs(TreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Node = node;
        }
    }
}
