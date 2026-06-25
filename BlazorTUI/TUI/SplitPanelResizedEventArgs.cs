namespace BlazorTUI.TUI
{
    public sealed class SplitPanelResizedEventArgs : EventArgs
    {
        public SplitPanelResizedEventArgs(
            short previousSplitterPosition,
            short splitterPosition,
            short firstPaneSize,
            short secondPaneSize)
        {
            PreviousSplitterPosition = previousSplitterPosition;
            SplitterPosition = splitterPosition;
            FirstPaneSize = firstPaneSize;
            SecondPaneSize = secondPaneSize;
        }

        public short PreviousSplitterPosition { get; }

        public short SplitterPosition { get; }

        public short FirstPaneSize { get; }

        public short SecondPaneSize { get; }
    }
}
