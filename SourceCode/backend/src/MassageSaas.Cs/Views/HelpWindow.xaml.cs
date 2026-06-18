using System;
using System.Threading.Tasks;
using System.Windows;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Settings;

namespace MassageSaas.Cs.Views;

/// <summary>
/// F1 使用帮助窗口。按当前显示模式默认展示 CS 端「正常 / 无障碍」版说明书，
/// 也可勾选「无障碍版本」随时切换查看。内容由平台端维护，未维护则用内置默认手册。
/// </summary>
public partial class HelpWindow : Window
{
    private readonly IApiClient _api;
    private PlatformManualDto? _manual;

    public HelpWindow(IApiClient api, PreferencesService prefs)
    {
        InitializeComponent();
        _api = api;
        A11yCheck.IsChecked = prefs.IsAccessible;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _manual = await _api.GetPlatformManualAsync();
        }
        catch (Exception ex)
        {
            ManualText.Text = "加载帮助失败：" + ex.Message;
            return;
        }
        Render();
    }

    private void Render()
    {
        if (_manual is null) return;
        var a11y = A11yCheck.IsChecked == true;
        ManualText.Text = a11y ? _manual.CsManualA11y : _manual.CsManualNormal;
        ManualText.FontSize = a11y ? 18 : 14;
        HeaderText.Text = a11y ? "使用帮助 · 无障碍版" : "使用帮助";
        ManualText.ScrollToHome();
    }

    private void A11yCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) Render();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
