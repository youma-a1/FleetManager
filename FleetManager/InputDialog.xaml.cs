using System.Windows;

namespace FleetManager
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; }

        public InputDialog(string prompt, string title = "Saisie", string defaultValue = "")
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = InputTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}