using System.Windows;

namespace VideoRecorderScreen.Views
{
    public enum ConflictChoice { Overwrite, SaveAsNumbered, Cancel }

    public partial class ConflictDialog : Window
    {
        private readonly TaskCompletionSource<ConflictChoice> _tcs = new();

        private ConflictDialog(string filename)
        {
            InitializeComponent();
            MessageText.Text = string.Format(Services.LocalizationService.Get("Conflict_Message"), filename);
        }

        public static Task<ConflictChoice> ShowAsync(string filename)
        {
            var dlg = new ConflictDialog(filename);
            dlg.Show();
            return dlg._tcs.Task;
        }

        private void OnOverwrite(object sender, RoutedEventArgs e)  { _tcs.TrySetResult(ConflictChoice.Overwrite);       Close(); }
        private void OnNumbered(object sender, RoutedEventArgs e)   { _tcs.TrySetResult(ConflictChoice.SaveAsNumbered);  Close(); }
        private void OnCancel(object sender, RoutedEventArgs e)     { _tcs.TrySetResult(ConflictChoice.Cancel);          Close(); }

        protected override void OnClosed(System.EventArgs e)
        {
            _tcs.TrySetResult(ConflictChoice.Cancel);
            base.OnClosed(e);
        }
    }
}
