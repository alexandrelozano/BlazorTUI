namespace BlazorTUI.TUI
{
    internal interface IPopupControl
    {
        bool IsPopupOpen { get; }

        bool ContainsPopupPoint(short X, short Y);

        void ClosePopup();
    }
}
