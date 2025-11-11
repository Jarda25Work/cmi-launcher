using System.Windows;

namespace CMILauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Nastavení exception handling
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Neočekávaná chyba: {args.Exception.Message}", 
                    "Chyba aplikace", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}