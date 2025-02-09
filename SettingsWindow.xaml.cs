using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using DeepSeekAIFoundryUI.Models;

namespace DeepSeekAIFoundryUI.Views
{
    /// <summary>
    /// Represents a window for modifying configuration values such as model key, endpoint, and name.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private string _settingsFilePath;
        private AppSettingsModel _appSettings;

        /// <summary>
        /// Initializes the settings window and loads the current configuration.
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
            _settingsFilePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "appsettings.json"
            );

            LoadSettings();
        }

        /// <summary>
        /// Loads settings from the configuration file, or creates default values if none exist.
        /// </summary>
        private void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                _appSettings = JsonSerializer.Deserialize<AppSettingsModel>(json) ?? new AppSettingsModel();
            }
            else
            {
                _appSettings = new AppSettingsModel
                {
                    ModelKey = "",
                    ModelEndpoint = "",
                    ModelName = ""
                };
            }

            ModelKeyTextBox.Text = _appSettings.ModelKey;
            ModelEndpointTextBox.Text = _appSettings.ModelEndpoint;
            ModelNameTextBox.Text = _appSettings.ModelName;
        }

        /// <summary>
        /// Saves the updated configuration values to disk.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _appSettings.ModelKey = ModelKeyTextBox.Text;
            _appSettings.ModelEndpoint = ModelEndpointTextBox.Text;
            _appSettings.ModelName = ModelNameTextBox.Text;

            var json = JsonSerializer.Serialize(_appSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);

            this.DialogResult = true;
            Close();
        }

        /// <summary>
        /// Cancels any changes and closes the settings window.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    }
}
