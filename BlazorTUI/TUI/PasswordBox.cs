using System.Drawing;

namespace BlazorTUI.TUI
{
    public class PasswordBox : TextBox
    {
        private char maskCharacter;

        public char MaskCharacter
        {
            get => maskCharacter;
            set
            {
                if (char.IsControl(value))
                    throw new ArgumentException("The mask character cannot be a control character.", nameof(value));

                maskCharacter = value;
            }
        }

        public bool IsRevealed { get; set; }

        public PasswordBox(
            string name,
            string text,
            short X,
            short Y,
            short width,
            Color forecolor,
            Color backgroundcolor,
            char maskCharacter = '•')
            : base(name, text, X, Y, width, forecolor, backgroundcolor)
        {
            MaskCharacter = maskCharacter;
            AllowCopy = false;
            AllowPaste = true;
        }

        public void ToggleReveal()
        {
            IsRevealed = !IsRevealed;
        }

        protected override string GetDisplayCharacter(short position)
        {
            if (position >= Value.Length)
                return " ";

            return IsRevealed ? Value.Substring(position, 1) : MaskCharacter.ToString();
        }
    }
}
