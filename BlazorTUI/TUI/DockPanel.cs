using System.Drawing;

namespace BlazorTUI.TUI
{
    public class DockPanel : LayoutPanel
    {
        private readonly Dictionary<object, DockPanelDock> docks = new(ReferenceEqualityComparer.Instance);

        public DockPanel(
            string name,
            short X,
            short Y,
            short width,
            short height,
            Color foreColor,
            Color backgroundColor)
            : base(name, X, Y, width, height, foreColor, backgroundColor)
        {
        }

        public void AddControl(Control control, DockPanelDock dock)
        {
            AddControl(control);
            SetDock(control, dock);
        }

        public void AddContainer(Container container, DockPanelDock dock)
        {
            AddContainer(container);
            SetDock(container, dock);
        }

        public void SetDock(Control control, DockPanelDock dock)
        {
            ArgumentNullException.ThrowIfNull(control);
            ValidateDock(dock);
            if (!controls.Any(existing => ReferenceEquals(existing, control)))
                throw new ArgumentException("The control does not belong to this DockPanel.", nameof(control));

            docks[control] = dock;
        }

        public void SetDock(Container container, DockPanelDock dock)
        {
            ArgumentNullException.ThrowIfNull(container);
            ValidateDock(dock);
            if (!containers.Any(existing => ReferenceEquals(existing, container)))
                throw new ArgumentException("The container does not belong to this DockPanel.", nameof(container));

            docks[container] = dock;
        }

        public DockPanelDock GetDock(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            return docks.TryGetValue(control, out DockPanelDock dock) ? dock : DockPanelDock.Fill;
        }

        public DockPanelDock GetDock(Container container)
        {
            ArgumentNullException.ThrowIfNull(container);
            return docks.TryGetValue(container, out DockPanelDock dock) ? dock : DockPanelDock.Fill;
        }

        protected override void ArrangeChildren()
        {
            int left = ContentLeft;
            int top = ContentTop;
            int right = ContentLeft + ContentWidth;
            int bottom = ContentTop + ContentHeight;

            foreach (LayoutChild child in GetLayoutChildren())
            {
                DockPanelDock dock = GetDock(child);
                int availableWidth = Math.Max(1, right - left);
                int availableHeight = Math.Max(1, bottom - top);
                int childWidth = Math.Min(GetChildWidth(child), availableWidth);
                int childHeight = Math.Min(GetChildHeight(child), availableHeight);

                switch (dock)
                {
                    case DockPanelDock.Left:
                        SetChildBounds(child, left, top, childWidth, availableHeight);
                        left += childWidth;
                        break;
                    case DockPanelDock.Right:
                        SetChildBounds(child, right - childWidth, top, childWidth, availableHeight);
                        right -= childWidth;
                        break;
                    case DockPanelDock.Top:
                        SetChildBounds(child, left, top, availableWidth, childHeight);
                        top += childHeight;
                        break;
                    case DockPanelDock.Bottom:
                        SetChildBounds(child, left, bottom - childHeight, availableWidth, childHeight);
                        bottom -= childHeight;
                        break;
                    case DockPanelDock.Fill:
                        SetChildBounds(child, left, top, availableWidth, availableHeight);
                        break;
                }
            }
        }

        private DockPanelDock GetDock(LayoutChild child)
            => child.Control is not null
                ? GetDock(child.Control)
                : GetDock(child.Container!);

        private static void ValidateDock(DockPanelDock dock)
        {
            if (!Enum.IsDefined(dock))
                throw new ArgumentOutOfRangeException(nameof(dock));
        }
    }
}
