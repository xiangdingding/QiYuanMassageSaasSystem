using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.ViewModels;
using MassageSaas.Shared.Stores;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace MassageSaas.Cs;

public partial class MainWindow : Window
{
    private readonly SessionService _session;
    private readonly Forms.NotifyIcon _tray;
    private bool _forceExit;
    private WindowState _restoreState = WindowState.Normal;

    public MainWindow()
    {
        InitializeComponent();
        _session = App.Resolve<SessionService>();

        // 反映持久化偏好（如果登录页改过、或者上次会话里切过）；用户头部勾选立即生效
        var prefs = App.Resolve<PreferencesService>();
        HeaderA11yToggle.IsChecked = prefs.IsAccessible;

        _tray = CreateTrayIcon();
    }

    // ---------- 系统托盘 ----------

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示主界面", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出系统", null, (_, _) => ExitApp());

        var tray = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "齐源按摩店收银系统",
            Visible = true,
            ContextMenuStrip = menu
        };
        // 左键单击 / 双击托盘图标都重新打开主界面
        tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) RestoreFromTray(); };
        tray.DoubleClick += (_, _) => RestoreFromTray();
        return tray;
    }

    private static Drawing.Icon LoadAppIcon()
    {
        // 优先用打包进资源的多尺寸 app.ico（齐源），NotifyIcon 会按需选合适尺寸
        try
        {
            var res = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
            if (res?.Stream is { } stream)
            {
                using (stream)
                    return new Drawing.Icon(stream);
            }
        }
        catch { /* 回退到 exe 关联图标 */ }

        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                var icon = Drawing.Icon.ExtractAssociatedIcon(exe);
                if (icon is not null) return icon;
            }
        }
        catch { /* 回退到系统图标 */ }
        return Drawing.SystemIcons.Application;
    }

    private void CloseToTray_Click(object sender, RoutedEventArgs e) => Close();

    private void HideToTray()
    {
        _restoreState = WindowState == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        Hide();
        ShowInTaskbar = false;
        _tray.ShowBalloonTip(1500, "齐源按摩店收银系统",
            "已最小化到托盘，双击图标可重新打开；右键可退出。", Forms.ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = _restoreState;
        Activate();
        // 置顶一下把窗口抢到最前，随即取消置顶
        Topmost = true;
        Topmost = false;
    }

    private void ExitApp()
    {
        _forceExit = true;
        Close();
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // 登出（会话已退出）或托盘“退出系统” → 真正关闭并清理托盘图标
        if (_forceExit || !_session.IsAuthenticated)
        {
            _tray.Visible = false;
            _tray.Dispose();
            // 托盘退出时主窗口可能处于隐藏态，需显式结束应用
            if (_forceExit) Application.Current.Shutdown();
            return;
        }

        // 普通关闭按钮 = 最小化到托盘，不退出
        e.Cancel = true;
        HideToTray();
    }

    // F10 是系统键（默认激活窗口菜单），KeyBinding 捕获不到，这里手动路由到「日结/交班」。
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.F10 && Keyboard.Modifiers == ModifierKeys.None
            && DataContext is MainViewModel vm)
        {
            vm.NavByKeyCommand.Execute("day-close");
            e.Handled = true;
        }
    }

    // ---------- 标题栏窗口按钮 ----------

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaxRestore_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Window_StateChanged(object sender, EventArgs e)
    {
        // 最大化尺寸由 WM_GETMINMAXINFO 精确约束到显示器工作区（不盖任务栏、不溢出边缘），
        // 故无需再加补偿边距，内容（含底部分页条）完整可见。
        RootContainer.Margin = new Thickness(0);
        var maximized = WindowState == WindowState.Maximized;
        MaxRestoreButton.Content = maximized ? "❐" : "□";
        MaxRestoreButton.ToolTip = maximized ? "还原" : "最大化";
    }

    // ---------- 最大化适配：把无边框窗口最大化尺寸限制到当前显示器工作区 ----------
    // WindowStyle=None + WindowChrome 最大化时默认会按整屏（含任务栏区域）铺开并向四周溢出约 8px，
    // 导致底部内容被裁切。处理 WM_GETMINMAXINFO，将最大尺寸/位置改为显示器工作区即可精确贴合。

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
    }

    private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;
        if (msg == WM_GETMINMAXINFO)
        {
            AdjustMaximizedBounds(hwnd, lParam);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static void AdjustMaximizedBounds(IntPtr hwnd, IntPtr lParam)
    {
        const int MONITOR_DEFAULTTONEAREST = 0x00000002;
        var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
        var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (monitor != IntPtr.Zero)
        {
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(monitor, ref info))
            {
                var work = info.rcWork;       // 工作区（已扣除任务栏）
                var screen = info.rcMonitor;  // 整个显示器
                // 位置相对显示器原点；尺寸取工作区宽高
                mmi.ptMaxPosition.X = work.Left - screen.Left;
                mmi.ptMaxPosition.Y = work.Top - screen.Top;
                mmi.ptMaxSize.X = work.Right - work.Left;
                mmi.ptMaxSize.Y = work.Bottom - work.Top;
                mmi.ptMaxTrackSize.X = work.Right - work.Left;
                mmi.ptMaxTrackSize.Y = work.Bottom - work.Top;
            }
        }
        Marshal.StructureToPtr(mmi, lParam, true);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private void StoreCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is ComboBox cb && cb.SelectedItem is StoreDto s)
        {
            vm.SwitchStoreCommand.Execute(s);
        }
    }

    // ---------- 顶栏用户菜单 ----------

    private void UserMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.ContextMenu is { } cm)
        {
            cm.PlacementTarget = b;
            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            cm.IsOpen = true;
        }
    }

    private void ProfileSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Views.ProfileWindow(App.Resolve<IApiClient>(), _session) { Owner = this };
        dlg.ShowDialog();
        var vm = DataContext as MainViewModel;
        if (dlg.PasswordChanged)
        {
            // 改密后需重新登录
            vm?.LogoutCommand.Execute(null);
            return;
        }
        vm?.RefreshUser();
    }

    private void LogoutMenu_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.LogoutCommand.Execute(null);
    }

    private void HeaderA11yToggle_Changed(object sender, RoutedEventArgs e)
    {
        var prefs = App.Resolve<PreferencesService>();
        prefs.A11yMode = HeaderA11yToggle.IsChecked == true
            ? PreferencesService.AppMode.Accessible
            : PreferencesService.AppMode.Normal;
    }
}
