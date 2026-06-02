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
        // DataContext 由 App.ShowLogin 在构造后用对象初始化器赋值，故同步“记住的密码 / 勾选态”要等 Loaded
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm) return;
        RememberCheck.IsChecked = vm.RememberMe;
        // PasswordBox.Password 不可绑定，VM 已从凭据加载明文，这里回填到框里
        if (!string.IsNullOrEmpty(vm.Password)) PasswordBox.Password = vm.Password;
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

    private void RememberCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.RememberMe = RememberCheck.IsChecked == true;
        }
    }

    private void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        var api = App.Resolve<IApiClient>();
        var dlg = new RegisterWindow(api) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.RegisteredPhone is { } phone && DataContext is LoginViewModel vm)
        {
            // 注册成功：把店主登录手机号填进账号框，清空密码让用户输入刚设置的密码
            vm.Username = phone;
            vm.Password = string.Empty;
            PasswordBox.Clear();
            PasswordBox.Focus();
            vm.ErrorMessage = "注册成功！已填入登录手机号，请输入刚才设置的密码登录。";
        }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    // 登录窗是首个窗口，关闭即退出程序
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void A11yToggle_Changed(object sender, RoutedEventArgs e)
    {
        var prefs = App.Resolve<PreferencesService>();
        prefs.A11yMode = A11yToggle.IsChecked == true
            ? PreferencesService.AppMode.Accessible
            : PreferencesService.AppMode.Normal;
    }
}
