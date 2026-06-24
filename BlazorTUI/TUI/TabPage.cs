namespace BlazorTUI.TUI
{
    public class TabPage : Container
    {
        private string title;

        public string Title
        {
            get => title;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                title = value;
            }
        }

        public TabPage(string name, string title)
            : base(name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            this.title = title;
            Width = 1;
            Height = 1;
        }
    }
}
