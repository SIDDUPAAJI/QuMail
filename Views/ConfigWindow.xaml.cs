using System.Windows;

namespace QuMailClient
{
    public partial class ConfigWindow : Window
    {
        public ConfigWindow()
        {
            InitializeComponent();
            LoadStoredSettings();
        }

        /// <summary>
        /// Populates the UI fields with currently saved credentials from the user profile.
        /// </summary>
        private void LoadStoredSettings()
        {
            // Load the saved email address
            TxtEmail.Text = Properties.Settings.Default.SavedEmail;

            // PasswordBox does not support direct data binding for security; 
            // we manually set it from the saved settings
            TxtPassword.Password = Properties.Settings.Default.SavedPassword;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Simple validation to ensure fields aren't empty
            if (string.IsNullOrWhiteSpace(TxtEmail.Text) || string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                MessageBox.Show("Please enter both an Email and an App Password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. Update the local settings with the UI values
            Properties.Settings.Default.SavedEmail = TxtEmail.Text.Trim();
            Properties.Settings.Default.SavedPassword = TxtPassword.Password.Trim();

            // 2. Persist the changes to the Windows User Profile
            Properties.Settings.Default.Save();

            MessageBox.Show("Configuration Saved. Identity has been refreshed.", "System Update", MessageBoxButton.OK, MessageBoxImage.Information);

            // Signal to MainWindow that settings were changed
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}