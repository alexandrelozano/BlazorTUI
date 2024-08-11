using System.Drawing;

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

        public Buttons buttons { get; set; }

        private Dialog dialog { get; set; }

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

        public Result result { get; set; }  

        private Button defaultButton { get; set; }  

        public MessageBox(string message, string title, Buttons buttons, BorderStyle borderStyle, Color foreColor, Color backgroundColor, Screen screen)
        {
            this.screen = screen;

            short width = (short)(message.Length + 4);
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
                short x = (short)((width / 2) - (m.Length / 2)); 
                Label lbl = new Label($"lbl{internalId}", m, x, y, (short)m.Length, foreColor, backgroundColor );
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
                    bttOk = new Button($"bttOk{internalId}", "Ok", (short)((width/2) - 2), (short)(heigth - 2), 4, foreColor, Color.Black , bttOk_OnClick);
                    dialog.AddControl(bttOk);
                    defaultButton = bttOk;
                    break;
                case Buttons.OKCancel:
                    bttOk = new Button($"bttOk{internalId}", "Ok", (short)((width / 4) - 2), (short)(heigth - 2), 4, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttOk);
                    defaultButton = bttOk;

                    bttCancel = new Button($"bttCancel{internalId}", "Cancel", (short)(((width / 4) * 3) - 4), (short)(heigth - 2), 8, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttCancel);
                    break;
                case Buttons.YesNo:
                    bttYes = new Button($"bttYes{internalId}", "Yes", (short)((width / 4) - 3), (short)(heigth - 2), 5, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttYes);
                    defaultButton = bttYes;

                    bttNo = new Button($"bttNo{internalId}", "No", (short)(((width / 4) * 3) - 2), (short)(heigth - 2), 4, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttNo);
                    break;
                case Buttons.YesNoCancel:
                    bttYes = new Button($"bttYes{internalId}", "Yes", 2, (short)(heigth - 2), 5, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttYes);
                    defaultButton = bttYes;

                    bttNo = new Button($"bttNo{internalId}", "No", (short)((width / 2) - 2), (short)(heigth - 2), 4, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttNo);
                    
                    bttCancel = new Button($"bttCancel{internalId}", "Cancel", (short)(width - 10), (short)(heigth - 2), 8, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttCancel);
                    break;
                case Buttons.RetryCancel:
                    bttRetry = new Button($"bttRetry{internalId}", "Retry", (short)((width / 4) - 5), (short)(heigth - 2), 7, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttRetry);
                    defaultButton = bttRetry;

                    bttCancel = new Button($"bttCancel{internalId}", "Cancel", (short)(((width / 4) * 3) - 4), (short)(heigth - 2), 8, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttCancel);
                    break;
                case Buttons.AbortRetryIgnore:
                    bttAbort = new Button($"bttAbort{internalId}", "Abort", 2, (short)(heigth - 2), 7, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttAbort);
                    defaultButton = bttAbort;

                    bttRetry = new Button($"bttRetry{internalId}", "Retry", (short)((width / 2) - 4), (short)(heigth - 2), 7, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttRetry);

                    bttIgnore = new Button($"bttIgnore{internalId}", "Ignore", (short)(width - 10), (short)(heigth - 2), 8, foreColor, Color.Black, bttOk_OnClick);
                    dialog.AddControl(bttIgnore);
                    break;
            }
        }

        public void Show()
        {
            dialog.Show();
            screen.SetFocus(defaultButton.name);
        }

        private void bttOk_OnClick()
        {
            dialog.Close();

            result = Result.OK;
        }

        private void bttCancel_OnClick()
        {
            dialog.Close();

            result = Result.Cancel;
        }
    }
}
