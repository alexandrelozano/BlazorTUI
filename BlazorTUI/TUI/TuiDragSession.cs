namespace BlazorTUI.TUI
{
    internal sealed class TuiDragSession
    {
        public TuiDragSession(Control target, short startX, short startY)
        {
            ArgumentNullException.ThrowIfNull(target);

            Control = target;
            StartX = startX;
            StartY = startY;
        }

        public TuiDragSession(Container target, short startX, short startY)
        {
            ArgumentNullException.ThrowIfNull(target);

            Container = target;
            StartX = startX;
            StartY = startY;
        }

        public Control? Control { get; }

        public Container? Container { get; }

        public short StartX { get; }

        public short StartY { get; }

        public bool HasMoved { get; set; }

        public bool Drag(short screenX, short screenY)
        {
            (short currentX, short currentY) = ToLocalCoordinates(screenX, screenY);
            return Control is not null
                ? Control.Drag(StartX, StartY, currentX, currentY)
                : Container!.Drag(StartX, StartY, currentX, currentY);
        }

        public bool EndDrag(short screenX, short screenY)
        {
            (short currentX, short currentY) = ToLocalCoordinates(screenX, screenY);
            return Control is not null
                ? Control.EndDrag(StartX, StartY, currentX, currentY)
                : Container!.EndDrag(StartX, StartY, currentX, currentY);
        }

        private (short X, short Y) ToLocalCoordinates(short screenX, short screenY)
        {
            int originX = Control is not null
                ? Control.container.XOffset() + Control.X
                : Container!.XOffset();
            int originY = Control is not null
                ? Control.container.YOffset() + Control.Y
                : Container!.YOffset();

            return (
                ToShort(screenX - originX),
                ToShort(screenY - originY));
        }

        private static short ToShort(int value)
            => (short)Math.Clamp(value, short.MinValue, short.MaxValue);
    }
}
