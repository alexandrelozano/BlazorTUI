namespace BlazorTUI.Utils
{
    public static class StringExtensions
    {
        public static string CenterString(this string stringToCenter, int totalLength)
            => TuiText.CenterToVisualWidth(stringToCenter, totalLength);
    }
}
