using System.Drawing;

using BlazorTUI.Utils;

namespace BlazorTUI.TUI
{
    public class MessageBox
    {
        private Screen screen { get; set; }

        public enum Buttons
        {
            OKOnly,
            OKCancel,
            YesNoCancel,
            YesNo,
            RetryCancel,
            AbortRetryIgnore
        }

        internal Buttons buttons { get; set; }

        public Buttons ButtonSet { get => buttons; set => buttons = value; }

        private Dialog dialog { get; set; }

        private bool completed;

        public enum Result
        {
            OK,
            Yes,
            No,
            Cancel,
            Retry,
            Abort,
            Ignore
        }

        private string internalId {  get; set; }

        internal Result result { get; set; }

        public Result DialogResult => result;

        public event EventHandler<MessageBoxClosedEventArgs>? Closed;

        private Button defaultButton { get; set; }  

        public MessageBox(string message, string title, Buttons buttons, BorderStyle borderStyle, Color foreColor, Color backgroundColor, Screen screen)
        {
            this.screen = screen;

            short width = (short)(TuiText.VisualWidth(message) + 4);
            short heigth = 6;

            while (width > (screen.width - 2))
            {
                width = (short)(width / 2);
                heigth++;
                message = $"{message.Substring(0, width)}{Environment.NewLine}{message.Substring(width, message.Length-width)}";
            }

            if (width < 30)
                width = 30;

            internalId = Guid.NewGuid().ToString();

            dialog = new Dialog($"MessageBox{internalId}", title, width, heigth, borderStyle, foreColor, backgroundColor, screen);

            string[] messages = message.Split(Environment.NewLine);

            short y = 1;
            foreach (string m in messages)
            {
                y++;
                int messageWidth = TuiText.VisualWidth(m);
                short x = (short)((width / 2) - (messageWidth / 2));
                Label lbl = new Label($"lbl{internalId}", m, x, y, (short)messageWidth, foreColor, backgroundColor );
                dialog.AddControl(lbl);
            }

            Button bttOk;
            Button bttCancel;
            Button bttYes;
            Button bttNo;
            Button bttRetry;
            Button bttAbort;
            Button bttIgnore;

            switch (buttons)
            {
                case Buttons.OKOnly:
                    bttOk = new Button($"bttOk{internalId}", "Ok", (short)((width/2) - 2), (short)(heigth - 2), 4, foreColor, backgroundColor);
                    bttOk.Clicked += (_, _) => bttOk_OnClick();
                    dialog.AddControl(bttOk);
                    defaultButton = bttOk;
                    break;
                case Buttons.OKCancel:
                    bttOk = new Button($"bttOk{internalId}", "Ok", (short)((width / 4) - 2), (short)(heigth - 2), 4, foreColor, backgroundColor);
                    bttOk.Clicked += (_, _) => bttOk_OnClick();
                    dialog.AddControl(bttOk);
                    defaultButton = bttOk;

                    bttCancel = new Button($"bttCancel{internalId}", "Cancel", (short)(((width / 4) * 3) - 4), (short)(heigth - 2), 8, foreColor, backgroundColor);
                    bttCancel.Clicked += (_, _) => bttCancel_OnClick();
                    dialog.AddControl(bttCancel);
                    break;
                case Buttons.YesNo:
                    bttYes = new Button($"bttYes{internalId}", "Yes", (short)((width / 4) - 3), (short)(heigth - 2), 5, foreColor, backgroundColor);
                    bttYes.Clicked += (_, _) => bttYes_OnClick();
                    dialog.AddControl(bttYes);
                    defaultButton = bttYes;

                    bttNo = new Button($"bttNo{internalId}", "No", (short)(((width / 4) * 3) - 2), (short)(heigth - 2), 4, foreColor, backgroundColor);
                    bttNo.Clicked += (_, _) => bttNo_OnClick();
                    dialog.AddControl(bttNo);
                    break;
                case Buttons.YesNoCancel:
                    bttYes = new Button($"bttYes{internalId}", "Yes", 2, (short)(heigth - 2), 5, foreColor, backgroundColor);
                    bttYes.Clicked += (_, _) => bttYes_OnClick();
                    dialog.AddControl(bttYes);
                    defaultButton = bttYes;

                    bttNo = new Button($"bttNo{internalId}", "No", (short)((width / 2) - 2), (short)(heigth - 2), 4, foreColor, backgroundColor);
                    bttNo.Clicked += (_, _) => bttNo_OnClick();
                    dialog.AddControl(bttNo);
                    
                    bttCancel = new Button($"bttCancel{internalId}", "Cancel", (short)(width - 10), (short)(heigth - 2), 8, foreColor, backgroundColor);
                    bttCancel.Clicked += (_, _) => bttCancel_OnClick();
                    dialog.AddControl(bttCancel);
                    break;
                case Buttons.RetryCancel:
                    bttRetry = new Button($"bttRetry{internalId}", "Retry", (short)((width / 4) - 5), (short)(heigth - 2), 7, foreColor, backgroundColor);
                    bttRetry.Clicked += (_, _) => bttRetry_OnClick();
                    dialog.AddControl(bttRetry);
                    defaultButton = bttRetry;

                    bttCancel = new Button($"bttCancel{internalId}", "Cancel", (short)(((width / 4) * 3) - 4), (short)(heigth - 2), 8, foreColor, backgroundColor);
                    bttCancel.Clicked += (_, _) => bttCancel_OnClick();
                    dialog.AddControl(bttCancel);
                    break;
                case Buttons.AbortRetryIgnore:
                    bttAbort = new Button($"bttAbort{internalId}", "Abort", 2, (short)(heigth - 2), 7, foreColor, backgroundColor);
                    bttAbort.Clicked += (_, _) => bttAbort_OnClick();
                    dialog.AddControl(bttAbort);
                    defaultButton = bttAbort;

                    bttRetry = new Button($"bttRetry{internalId}", "Retry", (short)((width / 2) - 4), (short)(heigth - 2), 7, foreColor, backgroundColor);
                    bttRetry.Clicked += (_, _) => bttRetry_OnClick();
                    dialog.AddControl(bttRetry);

                    bttIgnore = new Button($"bttIgnore{internalId}", "Ignore", (short)(width - 10), (short)(heigth - 2), 8, foreColor, backgroundColor);
                    bttIgnore.Clicked += (_, _) => bttIgnore_OnClick();
                    dialog.AddControl(bttIgnore);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttons), buttons, "Unsupported message box button configuration.");
            }
        }

        public void Show()
        {
            dialog.Show();
            screen.SetFocus(defaultButton.name);
        }

        public void Close(Result result = Result.Cancel)
            => Complete(result);

        private void bttOk_OnClick()
            => Complete(Result.OK);

        private void bttCancel_OnClick()
            => Complete(Result.Cancel);

        private void bttYes_OnClick()
            => Complete(Result.Yes);

        private void bttNo_OnClick()
            => Complete(Result.No);

        private void bttRetry_OnClick()
            => Complete(Result.Retry);

        private void bttIgnore_OnClick()
            => Complete(Result.Ignore);

        private void bttAbort_OnClick()
            => Complete(Result.Abort);

        private void Complete(Result selectedResult)
        {
            if (completed)
                return;

            completed = true;
            result = selectedResult;
            dialog.Close();
            Closed?.Invoke(this, new MessageBoxClosedEventArgs(result));
        }
    }
}
