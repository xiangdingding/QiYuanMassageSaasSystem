using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Auth;

namespace MassageSaas.Cs.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly SessionService _session;

    public LoginViewModel(IApiClient api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

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
