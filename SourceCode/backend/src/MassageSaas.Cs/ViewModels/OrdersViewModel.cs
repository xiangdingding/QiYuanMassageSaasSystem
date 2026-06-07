using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Cs.Views;
using MassageSaas.Shared.Complaints;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;

    public OrdersViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<OrderListItemDto> rows = new();

    [ObservableProperty]
    private string? statusFilter;

    [ObservableProperty]
    private string? keyword;

    [ObservableProperty]
    private DateTime? fromDate;

    [ObservableProperty]
    private DateTime? toDate;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>重置时批量改筛选条件，用此标志压制每次属性变化都触发的自动查询，避免重复请求。</summary>
    private bool _suppressAutoReload;

    /// <summary>状态下拉框选值即查（回到第 1 页，对齐 BS onSearch）。</summary>
    partial void OnStatusFilterChanged(string? value)
    {
        if (_suppressAutoReload) return;
        Page = 1;
        _ = ReloadAsync();
    }

    // ---- 分页（对齐 BS） ----
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyPropertyChangedFor(nameof(CanPrev))]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private int page = 1;

    [ObservableProperty]
    private int pageSize = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageInfo))]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private int total;

    public int TotalPages => Total <= 0 ? 1 : (Total + PageSize - 1) / PageSize;
    public string PageInfo => $"第 {Page} / {TotalPages} 页 · 共 {Total} 条";
    public bool CanPrev => Page > 1;
    public bool CanNext => Page < TotalPages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMemberOrder))]
    [NotifyPropertyChangedFor(nameof(HasPunch))]
    [NotifyPropertyChangedFor(nameof(CanRefund))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(HasRoomCharges))]
    [NotifyPropertyChangedFor(nameof(IsDetailOpen))]
    private OrderDto? selectedDetail;

    /// <summary>详情抽屉是否打开：选中订单详情即打开（驱动右侧抽屉显隐与滑入动画）。</summary>
    public bool IsDetailOpen => SelectedDetail is not null;

    /// <summary>关闭详情抽屉（点遮罩 / 关闭按钮 / Esc）。</summary>
    [RelayCommand]
    private void CloseDetail() => SelectedDetail = null;

    /// <summary>详情项目明细行（含中文标签/提成/可转钟可投诉）。</summary>
    [ObservableProperty]
    private ObservableCollection<OrderDetailItemViewModel> detailItems = new();

    [ObservableProperty]
    private ObservableCollection<StaffDto> technicians = new();

    public bool IsMemberOrder => SelectedDetail is { } o &&
        (o.PayMethod == "MemberCard" || o.MemberId is not null || !string.IsNullOrEmpty(o.MemberCardNo));
    public bool HasPunch => (SelectedDetail?.PunchCardUsedCount ?? 0) > 0;
    public bool HasRoomCharges => (SelectedDetail?.RoomCharges?.Count ?? 0) > 0;
    public bool CanRefund => SelectedDetail?.Status == "Completed";
    public bool CanCancel => SelectedDetail?.Status == "Pending";

    partial void OnSelectedDetailChanged(OrderDto? value)
    {
        DetailItems = value is null
            ? new ObservableCollection<OrderDetailItemViewModel>()
            : new ObservableCollection<OrderDetailItemViewModel>(
                value.Items.Select(i => new OrderDetailItemViewModel(i, value.Status)));
    }

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            var data = await _api.GetOrdersAsync(
                page: Page, pageSize: PageSize,
                storeId: sid,
                status: string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter,
                from: FromDate,
                to: ToDate,
                keyword: string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim());
            Rows = new ObservableCollection<OrderListItemDto>(data.Items);
            Total = data.Total;
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
    }

    /// <summary>任何筛选条件改变后回到第 1 页再查（对齐 BS onSearch）。</summary>
    [RelayCommand]
    private async Task Search()
    {
        Page = 1;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task Reset()
    {
        _suppressAutoReload = true;
        StatusFilter = null;
        Keyword = null;
        FromDate = null;
        ToDate = null;
        Page = 1;
        _suppressAutoReload = false;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task PrevPage()
    {
        if (!CanPrev) return;
        Page--;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (!CanNext) return;
        Page++;
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ShowDetailAsync(OrderListItemDto? row)
    {
        if (row is null) return;
        try
        {
            SelectedDetail = await _api.GetOrderAsync(row.Id);
            if (Technicians.Count == 0)
            {
                var t = await _api.GetStaffAsync(role: "Technician", pageSize: 200, storeId: _context.ActiveStoreId);
                Technicians = new ObservableCollection<StaffDto>(t.Items);
            }
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task RefundAsync()
    {
        if (SelectedDetail is null) return;
        if (MessageBox.Show($"确认退款订单 {SelectedDetail.OrderNo}？实收 ¥{SelectedDetail.PaidAmount:F2} 将退还。",
                "确认退款", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.RefundAsync(SelectedDetail.Id, new RefundRequest("门店退款"));
            MessageBox.Show("已退款");
            SelectedDetail = null;
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (SelectedDetail is null) return;
        if (MessageBox.Show("确认取消订单？", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question)
            != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelOrderAsync(SelectedDetail.Id);
            MessageBox.Show("已取消");
            SelectedDetail = null;
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task ComplaintAsync(OrderDetailItemViewModel? row)
    {
        if (row is null) return;
        var item = row.Dto;
        var dlg = new ComplaintCreateDialog(item.ServiceName, item.TechnicianName ?? "—");
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.CreateComplaintAsync(new CreateComplaintRequest(
                OrderItemId: item.Id,
                Tags: string.IsNullOrWhiteSpace(dlg.Tags) ? null : dlg.Tags!.Trim(),
                Comment: string.IsNullOrWhiteSpace(dlg.Comment) ? null : dlg.Comment!.Trim()));
            MessageBox.Show("已登记投诉");
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    /// <summary>转钟（更换技师）：弹窗选新技师 + 原因，对齐 BS 转钟对话框。</summary>
    [RelayCommand]
    private async Task TransferAsync(OrderDetailItemViewModel? row)
    {
        if (row is null || SelectedDetail is null) return;
        var item = row.Dto;
        var dlg = new TransferTechnicianWindow(item, Technicians.ToList())
        {
            Owner = Application.Current?.MainWindow
        };
        if (dlg.ShowDialog() != true || dlg.ResultTechnicianId is not long newId) return;
        try
        {
            SelectedDetail = await _api.TransferTechnicianAsync(SelectedDetail.Id, item.Id,
                new TransferTechnicianRequest(newId, string.IsNullOrWhiteSpace(dlg.ResultReason) ? null : dlg.ResultReason));
            MessageBox.Show("转钟成功");
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}

/// <summary>订单详情里的一项服务明细（包一层 RowViewModel：中文标签、提成、可转钟/可投诉）。</summary>
public class OrderDetailItemViewModel
{
    private readonly OrderItemDto _dto;
    private readonly string _orderStatus;

    public OrderDetailItemViewModel(OrderItemDto dto, string orderStatus)
    {
        _dto = dto;
        _orderStatus = orderStatus;
    }

    public OrderItemDto Dto => _dto;
    public string ServiceName => _dto.ServiceName;
    public string TechnicianName => _dto.TechnicianName ?? "—";
    public string RoomNo => _dto.RoomNo ?? "—";
    public string QuantityText => $"{_dto.Quantity} 次";

    /// <summary>面值小计：ListAmount 优先，否则 ListUnitPrice×数量，再退回 ItemTotal。</summary>
    public decimal Amount =>
        _dto.ListAmount > 0 ? _dto.ListAmount :
        _dto.ListUnitPrice > 0 ? Math.Round(_dto.ListUnitPrice * _dto.Quantity, 2) : _dto.ItemTotal;
    public string AmountText => $"¥{Amount:F2}";
    public string CommissionText => $"¥{_dto.CommissionAmount:F2}";

    public bool IsPunch => _dto.MemberPackageId.HasValue;
    public string AssignTag => _dto.AssignmentSource switch
    {
        "Rotation" => "轮钟",
        "Designation" => "点钟",
        _ => string.Empty
    };
    public bool ShowAssignTag => AssignTag.Length > 0;
    public bool ShowTransferred => _dto.TransferredAt.HasValue;

    /// <summary>未结账/服务中可转钟；已完成可投诉（与 BS 一致）。</summary>
    public bool CanTransfer => _orderStatus is "Pending" or "InProgress";
    public bool CanComplain => _orderStatus == "Completed";
}
