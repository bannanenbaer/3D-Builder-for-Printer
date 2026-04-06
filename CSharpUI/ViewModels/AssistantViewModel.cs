using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ThreeDBuilder.Services;

namespace ThreeDBuilder.ViewModels
{
    public class AssistantViewModel : INotifyPropertyChanged
    {
        private string _assistantInput = "";
        private ObservableCollection<string> _assistantMessages = new();
        private bool _isAssistantEnabled = true;
        private RelayCommand _sendAssistantMessageCommand;
        private RelayCommand _closeAssistantCommand;
        private PythonBridge? _pythonBridge;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AssistantViewModel(PythonBridge? pythonBridge)
        {
            _pythonBridge = pythonBridge;
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _sendAssistantMessageCommand = new RelayCommand(
                _ => SendAssistantMessage(),
                _ => !string.IsNullOrWhiteSpace(AssistantInput)
            );

            _closeAssistantCommand = new RelayCommand(_ => IsAssistantEnabled = false);
        }

        public string AssistantInput
        {
            get => _assistantInput;
            set
            {
                if (_assistantInput != value)
                {
                    _assistantInput = value;
                    OnPropertyChanged();
                    _sendAssistantMessageCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<string> AssistantMessages
        {
            get => _assistantMessages;
            set
            {
                if (_assistantMessages != value)
                {
                    _assistantMessages = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAssistantEnabled
        {
            get => _isAssistantEnabled;
            set
            {
                if (_isAssistantEnabled != value)
                {
                    _isAssistantEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SendAssistantMessageCommand => _sendAssistantMessageCommand;
        public ICommand CloseAssistantCommand => _closeAssistantCommand;

        private async void SendAssistantMessage()
        {
            if (string.IsNullOrWhiteSpace(AssistantInput))
                return;

            string userMessage = AssistantInput;
            AssistantMessages.Add($"👤 Du: {userMessage}");
            AssistantInput = "";

            try
            {
                // Parse user intent and generate response
                string response = await ProcessUserInput(userMessage);
                AssistantMessages.Add($"🤖 Assistent: {response}");
            }
            catch (Exception ex)
            {
                AssistantMessages.Add($"❌ Fehler: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task<string> ProcessUserInput(string userInput)
        {
            // Detect if user is asking for shape generation
            if (userInput.Contains("erstelle", StringComparison.OrdinalIgnoreCase) ||
                userInput.Contains("generiere", StringComparison.OrdinalIgnoreCase) ||
                userInput.Contains("mache", StringComparison.OrdinalIgnoreCase))
            {
                return await GenerateShapeFromDescription(userInput);
            }

            // Detect if user is asking for help
            if (userInput.Contains("hilf", StringComparison.OrdinalIgnoreCase) ||
                userInput.Contains("wie", StringComparison.OrdinalIgnoreCase) ||
                userInput.Contains("erklär", StringComparison.OrdinalIgnoreCase))
            {
                return GetHelpResponse(userInput);
            }

            // Default helpful response
            return "Ich kann dir helfen! Du kannst mich bitten, Formen zu erstellen (z.B. 'Erstelle einen roten Zylinder mit 50mm Höhe'), oder mich um Tipps fragen. Was möchtest du tun?";
        }

        private async System.Threading.Tasks.Task<string> GenerateShapeFromDescription(string description)
        {
            // This would integrate with LLM to parse the description
            // For now, return a helpful response
            return "Ich verstehe, dass du eine Form erstellen möchtest! " +
                   "Bitte beschreibe genauer: Welche Form (Würfel, Kugel, Zylinder, etc.), " +
                   "welche Größe (in mm) und welche Farbe? " +
                   "Beispiel: 'Erstelle einen Zylinder mit 50mm Höhe und 20mm Radius'";
        }

        private string GetHelpResponse(string query)
        {
            if (query.Contains("Kanten", StringComparison.OrdinalIgnoreCase) ||
                query.Contains("Fillet", StringComparison.OrdinalIgnoreCase))
            {
                return "Mit Fillet kannst du die Kanten deiner Formen abrunden! " +
                       "Wähle ein Objekt, gib den Radius ein und klick 'Fillet anwenden'. " +
                       "Das macht deine Modelle glatter und professioneller.";
            }

            if (query.Contains("Boolean", StringComparison.OrdinalIgnoreCase) ||
                query.Contains("Operation", StringComparison.OrdinalIgnoreCase))
            {
                return "Boolean-Operationen verbinden zwei Formen:\n" +
                       "• Vereinigung: Fügt zwei Formen zusammen\n" +
                       "• Subtraktion: Schneidet eine Form aus der anderen\n" +
                       "• Schnitt: Behält nur den überlappenden Bereich\n" +
                       "Wähle zwei Objekte und nutze die Buttons!";
            }

            if (query.Contains("Export", StringComparison.OrdinalIgnoreCase) ||
                query.Contains("STL", StringComparison.OrdinalIgnoreCase))
            {
                return "Du kannst dein Modell als STL exportieren! " +
                       "Gehe zu Datei → Exportieren oder drücke Ctrl+S. " +
                       "STL-Dateien kannst du direkt in Slicer-Software wie PrusaSlicer öffnen.";
            }

            return "Ich helfe gerne! Frag mich nach:\n" +
                   "• Formen erstellen\n" +
                   "• Kanten bearbeiten (Fillet/Chamfer)\n" +
                   "• Boolean-Operationen\n" +
                   "• STL-Export\n" +
                   "• OpenSCAD-Editor\n" +
                   "Was möchtest du wissen?";
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
