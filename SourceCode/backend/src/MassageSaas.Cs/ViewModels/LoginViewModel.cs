using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Auth;

namespace MassageSaas.Cs.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly SessionService _session;
    private readonly CredentialStore _credentials;

    public LoginViewModel(IApiClient api, SessionService session, CredentialStore credentials)
    {
        _api = api;
        _session = session;
        _credentials = credentials;

        // 启动时回填上次“记住”的账号密码；勾选状态随之置位，PasswordBox 由 View 在 Loaded 时同步
        if (_credentials.Load() is { } saved)
        {
            Username = saved.Username;
            Password = saved.Password;
            RememberMe = true;
        }
    }

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    public event Action? LoginSucceeded;

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "请输入账号和密码";
            return;
        }

        IsBusy = true;
        try
        {
            var resp = await _api.LoginAsync(new LoginRequest(Username.Trim(), Password, null));
            if (resp.User.Role == "PlatformAdmin")
            {
                ErrorMessage = "该账号是平台管理员，不能登录店铺端";
                return;
            }
            _session.SignIn(resp);
            if (RememberMe) _credentials.Save(Username.Trim(), Password);
            else _credentials.Clear();
            LoginSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            var (_, msg) = ErrorReporter.Parse(ex);
            ErrorMessage = msg;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
