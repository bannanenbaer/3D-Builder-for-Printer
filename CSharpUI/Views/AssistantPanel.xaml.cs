using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeDBuilder.Models;
using ThreeDBuilder.ViewModels;

namespace ThreeDBuilder.Views
{
    public partial class AssistantPanel : UserControl
    {
        public AssistantPanel()
        {
            InitializeComponent();

            // Auto-scroll to bottom whenever a message is added
            DataContextChanged += (_, _) => WireMessages();
        }

        private void WireMessages()
        {
            if (DataContext is AssistantViewModel vm)
            {
                vm.Messages.CollectionChanged += (_, _) =>
                    Dispatcher.InvokeAsync(() =>
                        ChatScroll.ScrollToBottom());
            }
        }

        // Send on Enter (Shift+Enter = new line, but AcceptsReturn=False so just Enter)
        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is AssistantViewModel vm)
            {
                vm.SendCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
