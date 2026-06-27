using System.Drawing;

namespace BlazorTUI.TUI
{
    public sealed class TuiDialogService
    {
        private readonly Screen screen;

        internal TuiDialogService(Screen screen)
        {
            ArgumentNullException.ThrowIfNull(screen);
            this.screen = screen;
        }

        public Task<MessageBox.Result> ShowMessageAsync(
            string message,
            string title = "Message",
            MessageBox.Buttons buttons = MessageBox.Buttons.OKOnly,
            BorderStyle borderStyle = BorderStyle.Line,
            Color? foreColor = null,
            Color? backgroundColor = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(title);
            if (!Enum.IsDefined(buttons))
                throw new ArgumentOutOfRangeException(nameof(buttons));
            if (!Enum.IsDefined(borderStyle))
                throw new ArgumentOutOfRangeException(nameof(borderStyle));

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<MessageBox.Result>(cancellationToken);

            TuiColorPair dialogColors = screen.Theme.Resolve(TuiThemeRole.Dialog);
            var messageBox = new MessageBox(
                message,
                title,
                buttons,
                borderStyle,
                foreColor ?? dialogColors.ForeColor,
                backgroundColor ?? dialogColors.BackgroundColor,
                screen);

            var completion = new TaskCompletionSource<MessageBox.Result>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration cancellationRegistration = default;
            EventHandler<MessageBoxClosedEventArgs>? handler = null;

            handler = (_, args) =>
            {
                messageBox.Closed -= handler;
                cancellationRegistration.Dispose();
                completion.TrySetResult(args.Result);
            };

            messageBox.Closed += handler;
            cancellationRegistration = cancellationToken.Register(() =>
            {
                messageBox.Closed -= handler;
                messageBox.Close(MessageBox.Result.Cancel);
                completion.TrySetCanceled(cancellationToken);
            });

            messageBox.Show();
            return completion.Task;
        }

        public async Task<bool> ConfirmAsync(
            string message,
            string title = "Confirm",
            BorderStyle borderStyle = BorderStyle.Line,
            Color? foreColor = null,
            Color? backgroundColor = null,
            CancellationToken cancellationToken = default)
        {
            MessageBox.Result result = await ShowMessageAsync(
                message,
                title,
                MessageBox.Buttons.YesNo,
                borderStyle,
                foreColor,
                backgroundColor,
                cancellationToken).ConfigureAwait(false);

            return result == MessageBox.Result.Yes || result == MessageBox.Result.OK;
        }
    }
}
