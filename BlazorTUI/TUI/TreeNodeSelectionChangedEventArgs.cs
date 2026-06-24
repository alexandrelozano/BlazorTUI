namespace BlazorTUI.TUI
{
    public sealed class TreeNodeSelectionChangedEventArgs : EventArgs
    {
        public TreeNode? PreviousNode { get; }

        public TreeNode? SelectedNode { get; }

        public TreeNodeSelectionChangedEventArgs(TreeNode? previousNode, TreeNode? selectedNode)
        {
            PreviousNode = previousNode;
            SelectedNode = selectedNode;
        }
    }
}
