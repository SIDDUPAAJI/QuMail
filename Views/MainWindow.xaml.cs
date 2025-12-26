using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading.Tasks;

namespace QuMailClient
{
    public partial class MainWindow : Window
    {
        private readonly KmClient _kmClient = new KmClient();
        private readonly EmailService _emailService = new EmailService();
        private byte[]? _currentSessionKey;

        // Using Properties.Settings directly is cleaner than static fields 
        // to avoid data desync, but we will keep them if your logic requires it.
        public static string UserEmail = "";
        public static string UserAppPassword = "";

        public MainWindow()
        {
            InitializeComponent();
            RefreshSession();
            AppendLog("QUMAIL CORE: System Online. Ready for secure transmission.", Brushes.Cyan);
        }

        public void RefreshSession()
        {
            UserEmail = Properties.Settings.Default.SavedEmail;
            UserAppPassword = Properties.Settings.Default.SavedPassword;

            if (!string.IsNullOrEmpty(UserEmail))
                AppendLog($"IDENTITY: Session active for {UserEmail}", Brushes.Lime);
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            RefreshSession();

            if (string.IsNullOrEmpty(UserEmail) || string.IsNullOrEmpty(UserAppPassword))
            {
                AppendLog("CRITICAL: Configuration required. Update Key Settings.", Brushes.Red);
                MessageBox.Show("CONFIGURATION_REQUIRED: Set Email/App Password in Key Configuration.", "System Error");
                return;
            }

            try
            {
                AppendLog("KMS: Requesting Quantum Key from Node...", Brushes.Gray);

                // 1. Quantum Key Acquisition
                var (keyId, keyBytes) = await _kmClient.GetQuantumKeyAsync();
                if (keyBytes == null)
                {
                    AppendLog("KMS_ERROR: Node rejected request or timed out.", Brushes.Red);
                    return;
                }

                _currentSessionKey = keyBytes;
                AppendLog($"KMS_SUCCESS: Key [{keyId}] synchronized.", Brushes.Lime);

                // 2. Encryption Phase
                int level = ComboSecurityLevel.SelectedIndex;
                AppendLog($"CORE: Encrypting payload (Security Level {level + 1})...", Brushes.Gray);

                string ciphertext = level switch
                {
                    0 => SecurityCore.EncryptWithOtp(TxtBody.Text, keyBytes),
                    1 => SecurityCore.EncryptWithAes(TxtBody.Text, keyBytes),
                    2 => SecurityCore.EncryptPqcHybrid(TxtBody.Text, keyBytes),
                    3 => SecurityCore.PlaintextMode(TxtBody.Text),
                    _ => TxtBody.Text
                };

                // 3. SMTP Transmission
                AppendLog("SMTP: Tunneling packet to Google Gateway...", Brushes.Gray);
                await _emailService.SendSecureEmailAsync(
                    TxtRecipient.Text,
                    TxtSubject.Text,
                    ciphertext,
                    keyId,
                    UserEmail,
                    UserAppPassword);

                // 4. Cleanup and Logging
                OutboxManager.LogMessage(TxtRecipient.Text, TxtSubject.Text, ciphertext, keyId);
                AppendLog("TRANSMISSION_SUCCESS: Secure packet delivered.", Brushes.Lime);

                TxtBody.Clear();
                MessageBox.Show("TRANSMISSION_SUCCESS: Packet delivered.");
            }
            catch (Exception ex)
            {
                AppendLog($"TRANSMISSION_FAILED: {ex.Message}", Brushes.Red);
                MessageBox.Show($"TRANSMISSION_FAILED: {ex.Message}");
            }
        }

        private void BtnInbox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TxtBody.Text)) return;

                AppendLog("CORE: Attempting decryption of local buffer...", Brushes.Gray);
                int level = ComboSecurityLevel.SelectedIndex;

                TxtBody.Text = level switch
                {
                    0 => SecurityCore.DecryptWithOtp(TxtBody.Text, _currentSessionKey ?? throw new Exception("No Key")),
                    1 => SecurityCore.DecryptWithAes(TxtBody.Text, _currentSessionKey ?? throw new Exception("No Key")),
                    2 => SecurityCore.DecryptPqcHybrid(TxtBody.Text, _currentSessionKey ?? throw new Exception("No Key")),
                    3 => SecurityCore.DecodePlaintext(TxtBody.Text),
                    _ => TxtBody.Text
                };

                AppendLog("DECRYPTION_SUCCESS: Original text restored.", Brushes.Lime);
            }
            catch (Exception ex)
            {
                AppendLog("DECRYPTION_FAILED: Protocol mismatch or missing key.", Brushes.Red);
                MessageBox.Show("DECRYPTION_FAILED: " + ex.Message);
            }
        }

        // --- NEW DIAGNOSTICS METHOD ---
        private async void BtnDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            paraLog.Inlines.Clear();
            AppendLog(">>> INITIATING DIAGNOSTIC SEQUENCE", Brushes.Cyan);

            // Testing KMS
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                await client.GetAsync("https://localhost:7001");
                AppendLog("[PASS] KMS NODE: Online.", Brushes.Lime);
            }
            catch { AppendLog("[FAIL] KMS NODE: Offline.", Brushes.Red); }

            // Testing Identity
            if (!string.IsNullOrEmpty(UserEmail)) AppendLog("[PASS] IDENTITY: Configured.", Brushes.Lime);
            else AppendLog("[FAIL] IDENTITY: Missing.", Brushes.Red);

            AppendLog("DIAGNOSTICS COMPLETE.", Brushes.Cyan);
        }

        private void AppendLog(string message, SolidColorBrush color)
        {
            // This ensures we can update the UI from background threads if necessary
            Dispatcher.Invoke(() => {
                var run = new Run($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}") { Foreground = color };
                paraLog.Inlines.Add(run);
                rtbTerminal.ScrollToEnd();
            });
        }

        private void BtnOutbox_Click(object sender, RoutedEventArgs e) => new OutboxWindow { Owner = this }.ShowDialog();

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            var configWin = new ConfigWindow { Owner = this };
            configWin.ShowDialog();
            RefreshSession();
        }
    }
}