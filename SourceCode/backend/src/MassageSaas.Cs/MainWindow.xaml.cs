using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.ViewModels;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // 反映持久化偏好（如果登录页改过、或者上次会话里切过）；用户头部勾选立即生效
        var prefs = App.Resolve<PreferencesService>();
        HeaderA11yToggle.IsChecked = prefs.IsAccessible;
    }

    private void StoreCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ComboBox cb && cb.SelectedItem is StoreDto s)
        {
            vm.SwitchStoreCommand.Execute(s);
        }
    }

    private void HeaderA11yToggle_Changed(object sender, RoutedEventArgs e)
    {
        var prefs = App.Resolve<PreferencesService>();
        prefs.A11yMode = HeaderA11yToggle.IsChecked == true
            ? PreferencesService.AppMode.Accessible
            : PreferencesService.AppMode.Normal;
    }
}
