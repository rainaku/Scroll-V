using System.Windows;

namespace ScrollV
{
    public partial class AddAppDialog : Window
    {
        public string AppName { get; private set; } = string.Empty;

        public AddAppDialog()
        {
            InitializeComponent();
            AppNameInput.Focus();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AppName = AppNameInput.Text.Trim();
            if (!string.IsNullOrEmpty(AppName))
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
