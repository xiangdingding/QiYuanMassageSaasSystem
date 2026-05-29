using System.Windows;
using System.Windows.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.ViewModels;

namespace MassageSaas.Cs.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        // 反映当前持久化偏好；用户后续勾选/取消立即切主题，App.xaml.cs 订阅 Changed 事件热应用
        var prefs = App.Resolve<PreferencesService>();
        A11yToggle.IsChecked = prefs.IsAccessible;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
        {
            vm.LoginCommand.Execute(null);
        }
    }

    private void A11yToggle_Changed(object sender, RoutedEventArgs e)
    {
        var prefs = App.Resolve<PreferencesService>();
        prefs.A11yMode = A11yToggle.IsChecked == true
            ? PreferencesService.AppMode.Accessible
            : PreferencesService.AppMode.Normal;
    }
}
