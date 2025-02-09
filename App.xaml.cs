using System;
using System.IO;
using System.Windows;
using DeepSeekAIFoundryUI.Views;

namespace DeepSeekAIFoundryUI
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var settingsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "appsettings.json"
            );

            if (!File.Exists(settingsFilePath))
            {

                var settingsWindow = new SettingsWindow();
                bool? dialogResult = settingsWindow.ShowDialog();


                if (dialogResult == true)
                {

                    OpenMainWindow();
                }
                else
                {

                    Shutdown();
                }
            }
            else
            {

                OpenMainWindow();
            }

        }

        private void OpenMainWindow()
        {
            var mainWindow = new MainWindow();


            this.MainWindow = mainWindow;


            mainWindow.Show();


            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }
}
