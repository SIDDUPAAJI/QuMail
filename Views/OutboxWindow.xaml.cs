using System.Windows;

namespace QuMailClient
{
    public partial class OutboxWindow : Window
    {
        public OutboxWindow()
        {
            InitializeComponent();

            // DO NOT call ClearHistory here anymore.
            // Just refresh to show whatever has been sent in THIS session.
            RefreshOutbox();
        }

        public void RefreshOutbox()
        {
            // This pulls the current session's history from outbox.dat
            var entries = OutboxManager.LoadEntries();

            // Bind to the DataGrid
            DgHistory.ItemsSource = entries;
        }
    }
}
