namespace BlazorTUI.TUI
{
    public sealed class TuiWizardStepChangedEventArgs : EventArgs
    {
        public TuiWizardStepChangedEventArgs(TuiWizardStep? previousStep, TuiWizardStep currentStep, int currentIndex)
        {
            ArgumentNullException.ThrowIfNull(currentStep);

            PreviousStep = previousStep;
            CurrentStep = currentStep;
            CurrentIndex = currentIndex;
        }

        public TuiWizardStep? PreviousStep { get; }

        public TuiWizardStep CurrentStep { get; }

        public int CurrentIndex { get; }
    }
}
