using System.Windows;
using ThreeDBuilder.ViewModels;

namespace ThreeDBuilder.Views
{
    public partial class UpdateDialog : Window
    {
        public UpdateDialog()
        {
            InitializeComponent();
        }

        public UpdateDialog(UpdateViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
