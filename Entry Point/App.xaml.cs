using System.Windows;

namespace QuMailClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // This runs ONCE when the app starts.
            // It wipes the history from the PREVIOUS session.
            OutboxManager.ClearHistory();

            base.OnStartup(e);
        }
    }
}
