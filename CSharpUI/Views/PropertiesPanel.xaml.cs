using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ThreeDBuilder.Views;

public partial class PropertiesPanel : UserControl
{
    public PropertiesPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Commit the currently focused TextBox binding on Enter, then execute ApplyParamsCommand.
    /// This lets users confirm any property change by pressing Enter without leaving the field.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            // Flush the focused TextBox binding so the ViewModel sees the latest value
            if (Keyboard.FocusedElement is TextBox tb)
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            // Execute the Apply command if available on the DataContext
            if (DataContext is ThreeDBuilder.ViewModels.MainViewModel vm &&
                vm.ApplyParamsCommand.CanExecute(null))
            {
                vm.ApplyParamsCommand.Execute(null);
                e.Handled = true;
            }
        }
        base.OnKeyDown(e);
    }
}
