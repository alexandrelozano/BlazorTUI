using System.Drawing;
using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class TreeView : Control
    {
        private readonly List<TreeNode> nodes = new();
        private readonly Dictionary<string, TreeNode> nodesByName = new(StringComparer.Ordinal);
        private TreeNode? selectedNode;
        private int scrollIndex;

        public IReadOnlyList<TreeNode> Nodes => nodes;

        public TreeNode? SelectedNode => selectedNode;

        public event EventHandler<TreeNodeSelectionChangedEventArgs>? SelectedNodeChanged;

        public event EventHandler<TreeNodeEventArgs>? NodeExpanded;

        public event EventHandler<TreeNodeEventArgs>? NodeCollapsed;

        public event EventHandler<TreeNodeEventArgs>? NodeActivated;

        public TreeView(
            string name,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(width, (short)4);
            ArgumentOutOfRangeException.ThrowIfLessThan(height, (short)1);

            Name = name;
            this.X = X;
            this.Y = Y;
            Width = width;
            Height = height;
            ForeColor = foreColor;
            BackgroundColor = backgroundColor;
            TabStop = true;
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
            if (node.Parent is not null || node.Owner is not null)
                throw new InvalidOperationException("The node already belongs to a tree.");

            RegisterSubtree(node);
            nodes.Add(node);
            OnNodesChanged();
        }

        public bool RemoveNode(TreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (node.Owner != this)
                return false;
            if (node.Parent is not null)
                return node.Parent.RemoveNode(node);
            if (!nodes.Remove(node))
                return false;

            OnSubtreeRemoved(node, null);
            node.SetOwnerRecursive(null);
            return true;
        }

        public void ClearNodes()
        {
            foreach (TreeNode node in nodes.ToArray())
                RemoveNode(node);
        }

        public TreeNode? GetNode(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return nodesByName.GetValueOrDefault(name);
        }

        public void SelectNode(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            SelectNode(GetNode(name) ?? throw new ArgumentException(
                $"A node named '{name}' does not exist.", nameof(name)));
        }

        public void SelectNode(TreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            ValidateOwnedNode(node);

            var ancestors = new Stack<TreeNode>();
            for (TreeNode? parent = node.Parent; parent is not null; parent = parent.Parent)
                ancestors.Push(parent);
            while (ancestors.TryPop(out TreeNode? ancestor))
                SetNodeExpanded(ancestor, true);

            SetSelectedNode(node, true);
        }

        public void ToggleNode(TreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            ValidateOwnedNode(node);
            if (node.HasChildren)
                SetNodeExpanded(node, !node.IsExpanded);
        }

        public void ExpandAll()
        {
            foreach (TreeNode node in nodes.SelectMany(node => node.EnumerateSubtree()))
            {
                if (node.HasChildren)
                    SetNodeExpanded(node, true);
            }
        }

        public void CollapseAll()
        {
            foreach (TreeNode node in nodes.SelectMany(node => node.EnumerateSubtree()).Reverse())
            {
                if (node.HasChildren)
                    SetNodeExpanded(node, false);
            }
        }

        public override bool KeyDown(string key, bool shiftKey)
        {
            if (!Visible || selectedNode is null)
                return false;

            IReadOnlyList<VisibleNode> visibleNodes = GetVisibleNodes();
            int selectedIndex = FindVisibleIndex(visibleNodes, selectedNode);
            switch (key)
            {
                case "ArrowUp":
                    MoveSelection(visibleNodes, Math.Max(0, selectedIndex - 1));
                    return true;
                case "ArrowDown":
                    MoveSelection(visibleNodes, Math.Min(visibleNodes.Count - 1, selectedIndex + 1));
                    return true;
                case "ArrowRight":
                    if (selectedNode.HasChildren && !selectedNode.IsExpanded)
                        SetNodeExpanded(selectedNode, true);
                    else if (selectedNode.IsExpanded && selectedNode.Children.Count > 0)
                        SetSelectedNode(selectedNode.Children[0], true);
                    return true;
                case "ArrowLeft":
                    if (selectedNode.HasChildren && selectedNode.IsExpanded)
                        SetNodeExpanded(selectedNode, false);
                    else if (selectedNode.Parent is not null)
                        SetSelectedNode(selectedNode.Parent, true);
                    return true;
                case "Home":
                    MoveSelection(visibleNodes, 0);
                    return true;
                case "End":
                    MoveSelection(visibleNodes, visibleNodes.Count - 1);
                    return true;
                case "Enter":
                case "Space":
                case " ":
                    TreeNode activatedNode = selectedNode;
                    ToggleNode(activatedNode);
                    NodeActivated?.Invoke(this, new TreeNodeEventArgs(activatedNode));
                    NotifyClicked();
                    return true;
                default:
                    return false;
            }
        }

        public override bool Click(short X, short Y)
        {
            if (!Visible || X < 0 || X >= Width || Y < 0 || Y >= Height)
                return false;

            container.TopContainer().SetFocus(Name);
            IReadOnlyList<VisibleNode> visibleNodes = GetVisibleNodes();
            if (X == Width - 1 && visibleNodes.Count > Height)
            {
                if (Y == 0)
                    scrollIndex = Math.Max(0, scrollIndex - 1);
                else if (Y == Height - 1)
                    scrollIndex = Math.Min(visibleNodes.Count - Height, scrollIndex + 1);

                NotifyClicked();
                return true;
            }

            int index = scrollIndex + Y;
            if (index < 0 || index >= visibleNodes.Count)
                return false;

            VisibleNode visibleNode = visibleNodes[index];
            SetSelectedNode(visibleNode.Node, true);
            if (X == visibleNode.Depth * 2 && visibleNode.Node.HasChildren)
                ToggleNode(visibleNode.Node);

            NotifyClicked();
            return true;
        }

        public override void Render(IList<Row> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);
            if (!Visible)
                return;

            IReadOnlyList<VisibleNode> visibleNodes = GetVisibleNodes();
            scrollIndex = Math.Clamp(scrollIndex, 0, Math.Max(0, visibleNodes.Count - Height));
            for (int row = 0; row < Height; row++)
            {
                int visibleIndex = scrollIndex + row;
                VisibleNode? visibleNode = visibleIndex < visibleNodes.Count ? visibleNodes[visibleIndex] : null;
                string content = visibleNode is null ? string.Empty : BuildNodeText(visibleNode.Value);
                bool highlighted = Focus && visibleNode is not null &&
                    ReferenceEquals(visibleNode.Value.Node, selectedNode);

                for (int x = 0; x < Width; x++)
                {
                    if (!TryGetCell(rows, x, row, out Cell cell))
                        continue;

                    PrepareCell(
                        cell,
                        highlighted ? BackgroundColor : ForeColor,
                        highlighted ? ForeColor : BackgroundColor);
                    cell.Character = x == Width - 1
                        ? GetScrollCharacter(row, visibleNodes.Count)
                        : TuiText.CellAt(content, x);
                }
            }
        }

        protected override object? GetValidationValue() => selectedNode;

        internal void RegisterSubtree(TreeNode node)
        {
            TreeNode[] subtree = node.EnumerateSubtree().ToArray();
            var newNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (TreeNode descendant in subtree)
            {
                if (descendant.Owner is not null ||
                    !newNames.Add(descendant.Name) ||
                    nodesByName.ContainsKey(descendant.Name))
                {
                    throw new InvalidOperationException($"A node named '{descendant.Name}' already exists.");
                }
            }

            foreach (TreeNode descendant in subtree)
                nodesByName.Add(descendant.Name, descendant);
            node.SetOwnerRecursive(this);
        }

        internal void OnNodesChanged()
        {
            if (selectedNode is null)
                SetSelectedNode(nodes.FirstOrDefault(), true);
            EnsureSelectionVisible(GetVisibleNodes());
        }

        internal void OnSubtreeRemoved(TreeNode node, TreeNode? fallbackNode)
        {
            if (selectedNode is not null && IsDescendantOrSelf(selectedNode, node))
                SetSelectedNode(fallbackNode ?? nodes.FirstOrDefault(), true);

            foreach (TreeNode descendant in node.EnumerateSubtree())
                nodesByName.Remove(descendant.Name);
            EnsureSelectionVisible(GetVisibleNodes());
        }

        internal void SetNodeExpanded(TreeNode node, bool expanded)
        {
            ValidateOwnedNode(node);
            if (node.IsExpanded == expanded || !node.HasChildren)
                return;

            if (!expanded && selectedNode is not null &&
                !ReferenceEquals(selectedNode, node) && IsDescendantOrSelf(selectedNode, node))
            {
                SetSelectedNode(node, true);
            }

            node.SetExpanded(expanded);
            var eventArgs = new TreeNodeEventArgs(node);
            if (expanded)
                NodeExpanded?.Invoke(this, eventArgs);
            else
                NodeCollapsed?.Invoke(this, eventArgs);
            EnsureSelectionVisible(GetVisibleNodes());
        }

        private void MoveSelection(IReadOnlyList<VisibleNode> visibleNodes, int index)
        {
            if (visibleNodes.Count == 0)
                return;
            SetSelectedNode(visibleNodes[index].Node, true);
        }

        private void SetSelectedNode(TreeNode? node, bool raiseEvent)
        {
            if (ReferenceEquals(selectedNode, node))
            {
                EnsureSelectionVisible(GetVisibleNodes());
                return;
            }

            TreeNode? previousNode = selectedNode;
            selectedNode = node;
            EnsureSelectionVisible(GetVisibleNodes());
            if (raiseEvent)
            {
                SelectedNodeChanged?.Invoke(
                    this,
                    new TreeNodeSelectionChangedEventArgs(previousNode, node));
            }
        }

        private IReadOnlyList<VisibleNode> GetVisibleNodes()
        {
            var result = new List<VisibleNode>();
            foreach (TreeNode node in nodes)
                AddVisibleNode(node, 0, result);
            return result;
        }

        private static void AddVisibleNode(TreeNode node, int depth, ICollection<VisibleNode> result)
        {
            result.Add(new VisibleNode(node, depth));
            if (!node.IsExpanded)
                return;

            foreach (TreeNode child in node.Children)
                AddVisibleNode(child, depth + 1, result);
        }

        private void EnsureSelectionVisible(IReadOnlyList<VisibleNode> visibleNodes)
        {
            int maximumScroll = Math.Max(0, visibleNodes.Count - Height);
            if (selectedNode is not null)
            {
                int selectedIndex = FindVisibleIndex(visibleNodes, selectedNode);
                if (selectedIndex >= 0)
                {
                    if (selectedIndex < scrollIndex)
                        scrollIndex = selectedIndex;
                    else if (selectedIndex >= scrollIndex + Height)
                        scrollIndex = selectedIndex - Height + 1;
                }
            }

            scrollIndex = Math.Clamp(scrollIndex, 0, maximumScroll);
        }

        private static int FindVisibleIndex(IReadOnlyList<VisibleNode> visibleNodes, TreeNode node)
        {
            for (int index = 0; index < visibleNodes.Count; index++)
            {
                if (ReferenceEquals(visibleNodes[index].Node, node))
                    return index;
            }
            return -1;
        }

        private static string BuildNodeText(VisibleNode visibleNode)
        {
            string marker = visibleNode.Node.HasChildren
                ? visibleNode.Node.IsExpanded ? "▼ " : "▶ "
                : "• ";
            return $"{new string(' ', visibleNode.Depth * 2)}{marker}{visibleNode.Node.Text}";
        }

        private string GetScrollCharacter(int row, int visibleNodeCount)
        {
            if (visibleNodeCount <= Height)
                return " ";
            if (Height == 1)
                return "↕";
            if (row == 0)
                return scrollIndex > 0 ? "↑" : "│";
            if (row == Height - 1)
                return scrollIndex + Height < visibleNodeCount ? "↓" : "│";
            return "│";
        }

        private bool TryGetCell(IList<Row> rows, int localX, int localY, out Cell cell)
        {
            int originX = container.XOffset() + X;
            int originY = container.YOffset() + Y;
            int x = originX + localX;
            int y = originY + localY;
            int minimumX = container.XOffset();
            int minimumY = container.YOffset();
            int maximumX = minimumX + container.Width;
            int maximumY = minimumY + container.Height;

            if (x < minimumX || x >= maximumX || y < minimumY || y >= maximumY ||
                y < 0 || y >= rows.Count || x < 0 || x >= rows[y].Cells.Count)
            {
                cell = null!;
                return false;
            }

            cell = rows[y].Cells[x];
            return true;
        }

        private static void PrepareCell(Cell cell, Color foreColor, Color backgroundColor)
        {
            cell.ForeColor = foreColor;
            cell.BackgroundColor = backgroundColor;
            cell.Character = " ";
            cell.Decoration = Cell.TextDecoration.None;
            cell.IsVisible = true;
            cell.BackgroundImage = "";
            cell.ScaleX = 1;
            cell.ScaleY = 1;
        }

        private void ValidateOwnedNode(TreeNode node)
        {
            if (node.Owner != this)
                throw new ArgumentException("The node does not belong to this TreeView.", nameof(node));
        }

        private static bool IsDescendantOrSelf(TreeNode node, TreeNode possibleAncestor)
        {
            for (TreeNode? current = node; current is not null; current = current.Parent)
            {
                if (ReferenceEquals(current, possibleAncestor))
                    return true;
            }
            return false;
        }

        private readonly record struct VisibleNode(TreeNode Node, int Depth);
    }
}
