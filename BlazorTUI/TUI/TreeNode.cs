namespace BlazorTUI.TUI
{
    public sealed class TreeNode
    {
        private readonly List<TreeNode> children = new();
        private TreeNode? parent;
        private TreeView? owner;
        private string text;
        private bool isExpanded;

        public string Name { get; }

        public string Text
        {
            get => text;
            set => text = ValidateText(value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (owner is null)
                    isExpanded = value;
                else
                    owner.SetNodeExpanded(this, value);
            }
        }

        public bool HasChildren => children.Count > 0;

        public TreeNode? Parent => parent;

        public IReadOnlyList<TreeNode> Children => children;

        public object? Tag { get; set; }

        internal TreeView? Owner => owner;

        public TreeNode(string name, string text, bool isExpanded = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
            this.text = ValidateText(text);
            this.isExpanded = isExpanded;
        }

        public TreeNode AddNode(string name, string text, bool isExpanded = false)
        {
            var node = new TreeNode(name, text, isExpanded);
            AddNode(node);
            return node;
        }

        public void AddNode(TreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (node.parent is not null || node.owner is not null)
                throw new InvalidOperationException("The node already belongs to a tree.");

            ValidateDetachedSubtree(node);
            owner?.RegisterSubtree(node);
            node.parent = this;
            children.Add(node);
            owner?.OnNodesChanged();
        }

        public bool RemoveNode(TreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (!children.Remove(node))
                return false;

            TreeView? currentOwner = owner;
            node.parent = null;
            currentOwner?.OnSubtreeRemoved(node, this);
            node.SetOwnerRecursive(null);
            return true;
        }

        public void ClearNodes()
        {
            foreach (TreeNode child in children.ToArray())
                RemoveNode(child);
        }

        internal IEnumerable<TreeNode> EnumerateSubtree()
        {
            yield return this;
            foreach (TreeNode child in children)
            {
                foreach (TreeNode descendant in child.EnumerateSubtree())
                    yield return descendant;
            }
        }

        internal void SetOwnerRecursive(TreeView? treeView)
        {
            owner = treeView;
            foreach (TreeNode child in children)
                child.SetOwnerRecursive(treeView);
        }

        internal void SetExpanded(bool value)
            => isExpanded = value;

        private void ValidateDetachedSubtree(TreeNode node)
        {
            TreeNode root = this;
            while (root.parent is not null)
                root = root.parent;

            var names = new HashSet<string>(
                root.EnumerateSubtree().Select(existing => existing.Name),
                StringComparer.Ordinal);
            foreach (TreeNode descendant in node.EnumerateSubtree())
            {
                if (!names.Add(descendant.Name))
                    throw new InvalidOperationException($"A node named '{descendant.Name}' already exists.");
            }
        }

        private static string ValidateText(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Contains('\r') || value.Contains('\n'))
                throw new ArgumentException("Tree node text cannot contain line breaks.", nameof(value));
            return value;
        }
    }
}
