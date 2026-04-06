using System.Windows;
using System.Windows.Controls;
using ThreeDBuilder.Services;

namespace ThreeDBuilder.Views
{
    public partial class SettingsPanel : UserControl
    {
        public SettingsPanel()
        {
            InitializeComponent();
            // Pre-fill masked box with placeholder if key exists
            Loaded += (_, _) =>
            {
                if (AIAssistantService.LoadApiKey() is { Length: > 0 })
                    ApiKeyBox.Password = "sk-ant-••••••••••••";
            };
        }

        private void OnSaveApiKey(object sender, RoutedEventArgs e)
        {
            var key = ApiKeyBox.Password.Trim();
            if (string.IsNullOrEmpty(key) || key.StartsWith("sk-ant-••"))
            {
                MessageBox.Show("Bitte gib einen gültigen Anthropic API-Schlüssel ein.",
                    "Kein Schlüssel", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AIAssistantService.SaveApiKey(key);
            MessageBox.Show("✅ API-Schlüssel gespeichert! Brixl ist jetzt vollständig aktiviert.",
                "Gespeichert", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
