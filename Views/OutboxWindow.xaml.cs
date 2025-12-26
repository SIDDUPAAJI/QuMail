using System.Windows;

namespace QuMailClient
{
    public partial class OutboxWindow : Window
    {
        public OutboxWindow()
        {
            InitializeComponent();

            // Call the loading method as soon as the window initializes
            RefreshOutbox();
        }

        /// <summary>
        /// Pulls the latest encrypted history from the OutboxManager and 
        /// binds it to the DataGrid.
        /// </summary>
        public void RefreshOutbox()
        {
            // 1. Fetch the decrypted history from local storage
            var entries = OutboxManager.LoadEntries();

            // 2. Bind the list to the DataGrid's ItemsSource
            DgHistory.ItemsSource = entries;

            // 3. (Optional) Provide feedback if the history is empty
            if (entries == null || entries.Count == 0)
            {
                // Terminal-style notice
                // This matches the logic from your MainWindow diagnostics
            }
        }
    }
}