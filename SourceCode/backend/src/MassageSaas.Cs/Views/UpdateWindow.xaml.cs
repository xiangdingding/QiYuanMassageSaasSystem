using System.ComponentModel;
using System.Windows;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.AppVersions;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 升级提示弹窗：展示新版本与更新日志，点击"立即更新"下载安装包（带进度）并运行安装。
/// 强制更新（<see cref="AppVersionCheckResult.ForceUpdate"/>）时隐藏"稍后"、改为"退出程序"，
/// 关闭窗口即退出应用，必须更新才能继续使用。
/// </summary>
public partial class UpdateWindow : Window
{
    private readonly UpdateService _svc;
    private readonly AppVersionCheckResult _result;
    private readonly bool _force;
    private bool _downloading;
    private bool _installing;   // 已启动安装/即将退出，关闭时不再二次 Shutdown

    public UpdateWindow(UpdateService svc, AppVersionCheckResult result)
    {
        InitializeComponent();
        _svc = svc;
        _result = result;
        _force = result.ForceUpdate;

        VersionLine.Text = $"当前版本 {UpdateService.CurrentVersion}　→　最新版本 {result.LatestVersion}";
        ChangelogText.Text = string.IsNullOrWhiteSpace(result.Changelog) ? "优化体验、修复若干问题。" : result.Changelog;

        if (_force)
        {
            Title = "请更新到最新版本";
            LaterButton.Visibility = Visibility.Collapsed;
            ExitButton.Visibility = Visibility.Visible;
        }
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_downloading) return;
        _downloading = true;
        UpdateButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        ProgressPanel.Visibility = Visibility.Visible;
        ProgressText.Text = "正在下载安装包…";

        var progress = new Progress<double>(p =>
        {
            DownloadProgress.Value = p * 100;
            ProgressText.Text = $"正在下载安装包… {p * 100:0}%";
        });

        try
        {
            var path = await _svc.DownloadAsync(_result, progress, System.Threading.CancellationToken.None);
            ProgressText.Text = "下载完成，正在启动安装程序…";
            _installing = true;
            UpdateService.RunInstallerAndExit(path);
        }
        catch (Exception ex)
        {
            _downloading = false;
            UpdateButton.IsEnabled = true;
            LaterButton.IsEnabled = true;
            ProgressPanel.Visibility = Visibility.Collapsed;
            MessageBox.Show(this, $"更新失败：{ex.Message}", "更新", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Later_Click(object sender, RoutedEventArgs e) => Close();

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    protected override void OnClosing(CancelEventArgs e)
    {
        // 下载中不允许关闭，避免中断
        if (_downloading && !_installing)
        {
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // 强制更新且用户未走安装流程而关闭 → 退出应用
        if (_force && !_installing)
            Application.Current.Shutdown();
    }
}
