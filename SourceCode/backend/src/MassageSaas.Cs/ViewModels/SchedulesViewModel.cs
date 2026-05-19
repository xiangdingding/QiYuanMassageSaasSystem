using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Schedules;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels;

/// <summary>排班与请假：技师班次安排 + 请假单审批。</summary>
public partial class SchedulesViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;
    private List<StaffDto> _staff = new();

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
    private DateTime toDate = DateTime.Today.AddDays(7);

    [ObservableProperty]
    private bool onlyPendingLeaves = true;

    [ObservableProperty]
    private bool isBusy;

    partial void OnFromDateChanged(DateTime value) => _ = ReloadAsync();
    partial void OnToDateChanged(DateTime value) => _ = ReloadAsync();
    partial void OnOnlyPendingLeavesChanged(bool value) => _ = ReloadAsync();

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            Schedules = new ObservableCollection<StaffScheduleDto>(
                await _api.GetSchedulesAsync(sid, FromDate.Date, ToDate.Date.AddDays(1)));
            Leaves = new ObservableCollection<LeaveRequestDto>(
                await _api.GetLeavesAsync(status: OnlyPendingLeaves ? "Pending" : null));

            var staff = await _api.GetStaffAsync(pageSize: 100, role: "Technician", storeId: sid);
            _staff = staff.Items.Where(s => s.IsActive).ToList();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddScheduleAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("未选择门店"); return; }
        if (_staff.Count == 0) { MessageBox.Show("当前门店暂无在职技师"); return; }
        var dlg = new Views.ScheduleFormWindow(sid, _staff);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateScheduleAsync(dlg.BuildRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
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
