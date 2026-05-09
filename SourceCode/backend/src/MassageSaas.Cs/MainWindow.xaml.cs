using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.ViewModels;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void StoreCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ComboBox cb && cb.SelectedItem is StoreDto s)
        {
            vm.SwitchStoreCommand.Execute(s);
        }
    }
}
