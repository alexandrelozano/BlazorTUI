namespace BlazorTUI.TUI
{
    internal sealed class TuiEventScope : IDisposable
    {
        [ThreadStatic]
        private static int suppressionDepth;

        private readonly bool suppressEvents;
        private bool disposed;

        private TuiEventScope(bool suppressEvents)
        {
            this.suppressEvents = suppressEvents;
            if (suppressEvents)
                suppressionDepth++;
        }

        public static bool EventsSuppressed => suppressionDepth > 0;

        public static TuiEventScope Suppress(bool suppressEvents)
            => new(suppressEvents);

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (suppressEvents && suppressionDepth > 0)
                suppressionDepth--;
        }
    }
}
