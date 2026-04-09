using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ThreeDBuilder.ViewModels;

namespace ThreeDBuilder.Views
{
    public partial class AssistantPanel : UserControl
    {
        public AssistantPanel()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => WireMessages();
        }

        private AssistantViewModel? _wiredVm;

        private void WireMessages()
        {
            if (_wiredVm != null)
            {
                _wiredVm.Messages.CollectionChanged -= OnMessagesChanged;
                _wiredVm = null;
            }

            if (DataContext is AssistantViewModel vm)
            {
                _wiredVm = vm;
                vm.Messages.CollectionChanged += OnMessagesChanged;
                // Open chat when first message arrives (e.g. welcome message)
                vm.IsChatOpen = true;
            }
        }

        private void OnMessagesChanged(object? sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                ChatScroll.ScrollToBottom();
                // Auto-expand chat when a new bot message arrives
                if (DataContext is AssistantViewModel vm && !vm.IsChatOpen)
                    vm.IsChatOpen = true;
            });
        }

        /// <summary>Enter key sends the message; Escape collapses the chat.</summary>
        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is AssistantViewModel vm)
            {
                vm.SendCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && DataContext is AssistantViewModel vm2)
            {
                vm2.IsChatOpen = false;
                e.Handled = true;
            }
        }

        /// <summary>Toggle the history popup on the clock button.</summary>
        private void OnHistoryClick(object sender, RoutedEventArgs e)
        {
            HistoryPopup.IsOpen = !HistoryPopup.IsOpen;
        }
    }
}
