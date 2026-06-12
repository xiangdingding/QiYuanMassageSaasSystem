using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Schedules;
using MassageSaas.Shared.Staff;
using Refit;

namespace MassageSaas.Cs.ViewModels;

/// <summary>排班与请假：技师班次安排 + 请假单审批。</summary>
public partial class SchedulesViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;
    private List<StaffDto> _staff = new();

    /// <summary>过滤下拉项：Id 为 null 表示"全部"。</summary>
    public record StaffOption(long? Id, string Label);
    /// <summary>枚举过滤下拉项：Value 为 null 表示"全部"。</summary>
    public record CodeOption(string? Value, string Label);

    public SchedulesViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<StaffScheduleDto> schedules = new();

    [ObservableProperty]
    private ObservableCollection<LeaveRequestDto> leaves = new();

    [ObservableProperty]
    private DateTime fromDate = DateTime.Today;

    [ObservableProperty]
    private DateTime toDate = DateTime.Today.AddDays(14);

    [ObservableProperty]
    private bool isBusy;

    partial void OnFromDateChanged(DateTime value) => _ = ReloadAsync();
    partial void OnToDateChanged(DateTime value) => _ = ReloadAsync();

    // ---- 请假审批过滤（员工 / 类型 / 状态 / 日期区间）----
    [ObservableProperty]
    private ObservableCollection<StaffOption> leaveStaffOptions = new() { new(null, "全部员工") };
    [ObservableProperty]
    private ObservableCollection<CodeOption> leaveTypeOptions = new()
    {
        new(null, "全部类型"), new("Sick", "病假"), new("Personal", "事假"),
        new("Annual", "年假"), new("Training", "培训")
    };
    [ObservableProperty]
    private ObservableCollection<CodeOption> leaveStatusOptions = new()
    {
        new(null, "全部状态"), new("Pending", "待审批"), new("Approved", "已通过"),
        new("Rejected", "已驳回"), new("Cancelled", "已撤销")
    };

    /// <summary>选中过滤员工 Id；null = 全部员工。</summary>
    [ObservableProperty]
    private long? leaveUserFilter;
    /// <summary>选中过滤请假类型；null = 全部类型。</summary>
    [ObservableProperty]
    private string? leaveTypeFilter;
    /// <summary>选中过滤审批状态；null = 全部状态。</summary>
    [ObservableProperty]
    private string? leaveStatusFilter;
    /// <summary>请假日期区间起；null = 不限。</summary>
    [ObservableProperty]
    private DateTime? leaveFromFilter;
    /// <summary>请假日期区间止；null = 不限。</summary>
    [ObservableProperty]
    private DateTime? leaveToFilter;

    partial void OnLeaveUserFilterChanged(long? value) => _ = ReloadLeavesAsync();
    partial void OnLeaveTypeFilterChanged(string? value) => _ = ReloadLeavesAsync();
    partial void OnLeaveStatusFilterChanged(string? value) => _ = ReloadLeavesAsync();
    partial void OnLeaveFromFilterChanged(DateTime? value) => _ = ReloadLeavesAsync();
    partial void OnLeaveToFilterChanged(DateTime? value) => _ = ReloadLeavesAsync();

    /// <summary>清空请假过滤条件（逐个置空，统一触发一次重载）。</summary>
    [RelayCommand]
    private async Task ResetLeaveFilterAsync()
    {
        leaveUserFilter = null; OnPropertyChanged(nameof(LeaveUserFilter));
        leaveTypeFilter = null; OnPropertyChanged(nameof(LeaveTypeFilter));
        leaveStatusFilter = null; OnPropertyChanged(nameof(LeaveStatusFilter));
        leaveFromFilter = null; OnPropertyChanged(nameof(LeaveFromFilter));
        leaveToFilter = null; OnPropertyChanged(nameof(LeaveToFilter));
        await ReloadLeavesAsync();
    }

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            // 日期取当天中午，避开服务端北京时间-8h 把 00:00 减成前一天，保证查询区间与存储日期一致
            var qFrom = NoonOf(FromDate);
            var qTo = NoonOf(ToDate);
            Schedules = new ObservableCollection<StaffScheduleDto>(
                await _api.GetSchedulesAsync(sid, qFrom, qTo));

            // 与 BS 一致：排班可选全部在职员工（不限技师）
            var staff = await _api.GetStaffAsync(pageSize: 100, storeId: sid);
            _staff = staff.Items.Where(s => s.IsActive).ToList();

            // 员工过滤下拉（保留"全部员工"占位 + 当前门店在职员工）
            var opts = new ObservableCollection<StaffOption> { new(null, "全部员工") };
            foreach (var s in _staff) opts.Add(new StaffOption(s.Id, s.RealName ?? s.Username));
            LeaveStaffOptions = opts;

            await ReloadLeavesAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>仅按当前过滤条件重载请假单（员工 / 类型 / 状态 / 日期区间）。</summary>
    private async Task ReloadLeavesAsync()
    {
        try
        {
            DateTime? qFrom = LeaveFromFilter.HasValue ? NoonOf(LeaveFromFilter.Value) : null;
            DateTime? qTo = LeaveToFilter.HasValue ? NoonOf(LeaveToFilter.Value) : null;
            Leaves = new ObservableCollection<LeaveRequestDto>(
                await _api.GetLeavesAsync(LeaveUserFilter, LeaveStatusFilter, LeaveTypeFilter, qFrom, qTo));
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task AddScheduleAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("未选择门店"); return; }
        if (_staff.Count == 0) { MessageBox.Show("当前门店暂无可排班员工"); return; }
        var dlg = new Views.ScheduleFormWindow(sid, _staff);
        if (dlg.ShowDialog() != true) return;
        var req = dlg.BuildRequest();
        var d = req.WorkDate.Date;
        try
        {
            await _api.CreateScheduleAsync(req);
            // 把筛选区间扩展到包含该日，确保刚添加的排班一定可见
            EnsureDateVisible(d);
            await ReloadAsync();
            // 与 BS 一致：给出明确的成功反馈，避免看不到结果而重复添加
            var who = _staff.FirstOrDefault(s => s.Id == req.UserId);
            var name = who?.RealName ?? who?.Username ?? "该员工";
            MessageBox.Show($"已为 {name} 保存 {req.WorkDate:yyyy-MM-dd} 的排班（{req.StartTime}-{req.EndTime}）。",
                "已保存", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (ApiException api) when (api.StatusCode == HttpStatusCode.Conflict)
        {
            // 该员工当天已有排班，但可能在当前日期区间之外没显示出来（CS 默认只看近两周）。
            // 把该日纳入区间并刷新，让那条已存在的排班显示出来，再按 BS 一致地提示后端消息。
            EnsureDateVisible(d);
            await ReloadAsync();
            ErrorReporter.Show(api);
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>取该日期当天中午（避开服务端北京时间-8h 把 00:00 减成前一天）。</summary>
    private static DateTime NoonOf(DateTime d) => new(d.Year, d.Month, d.Day, 12, 0, 0);

    /// <summary>把日期筛选区间扩展到包含指定日期（不触发自动重载，由调用方统一刷新）。</summary>
    private void EnsureDateVisible(DateTime date)
    {
        var d = date.Date;
        if (d < FromDate.Date) { fromDate = d; OnPropertyChanged(nameof(FromDate)); }
        if (d > ToDate.Date) { toDate = d; OnPropertyChanged(nameof(ToDate)); }
    }

    [RelayCommand]
    private async Task DeleteScheduleAsync(StaffScheduleDto? s)
    {
        if (s is null) return;
        if (MessageBox.Show($"删除 {s.UserName} 在 {s.WorkDate:MM-dd} 的班次？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.DeleteScheduleAsync(s.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>登记请假：为员工提交请假单，逻辑与 BS 端 submitLeave 一致。</summary>
    [RelayCommand]
    private async Task RegisterLeaveAsync()
    {
        if (_context.ActiveStoreId is null) { MessageBox.Show("未选择门店"); return; }
        if (_staff.Count == 0) { MessageBox.Show("当前门店暂无员工"); return; }
        var dlg = new Views.LeaveFormWindow(_staff) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.SubmitLeaveAsync(dlg.BuildRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>查看请假详情：只读弹窗展示该请假单的全部信息。</summary>
    [RelayCommand]
    private void ShowLeaveDetail(LeaveRequestDto? leave)
    {
        if (leave is null) return;
        new Views.LeaveDetailWindow(leave) { Owner = Application.Current?.MainWindow }.ShowDialog();
    }

    [RelayCommand]
    private async Task ApproveLeaveAsync(LeaveRequestDto? leave)
    {
        if (leave is null) return;
        await DecideLeaveAsync(leave, approve: true);
    }

    [RelayCommand]
    private async Task RejectLeaveAsync(LeaveRequestDto? leave)
    {
        if (leave is null) return;
        await DecideLeaveAsync(leave, approve: false);
    }

    private async Task DecideLeaveAsync(LeaveRequestDto leave, bool approve)
    {
        var verb = approve ? "批准" : "驳回";
        if (MessageBox.Show($"确认{verb} {leave.UserName} 的{leave.Type}请假？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        try
        {
            await _api.ApproveLeaveAsync(leave.Id, new ApproveLeaveRequest(approve, null));
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}
