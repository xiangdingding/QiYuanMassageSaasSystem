using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Complaints;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels;

/// <summary>投诉处理：登记的客诉单按状态查看，支持改派 / 退款 / 道歉 / 不予处理与取消。</summary>
public partial class ComplaintsViewModel : ObservableObject
{
    private readonly IApiClient _api;
    private readonly AppContextService _context;
    private List<StaffDto> _technicians = new();

    public ComplaintsViewModel(IApiClient api, AppContextService context)
    {
        _api = api;
        _context = context;
        _ = ReloadAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ComplaintRowViewModel> rows = new();

    /// <summary>状态筛选（对齐 BS）：Pending / Resolved / Cancelled，空串=全部。默认待处理。</summary>
    [ObservableProperty]
    private string statusFilter = "Pending";

    /// <summary>开始日期（含）；null = 不限。</summary>
    [ObservableProperty]
    private DateTime? fromDate;

    /// <summary>结束日期（含当天）；null = 不限。</summary>
    [ObservableProperty]
    private DateTime? toDate;

    [ObservableProperty]
    private bool isBusy;

    // ---- 分页（对齐 BS / 订单页） ----
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

    /// <summary>状态/日期任一变化即查，回到第 1 页。</summary>
    partial void OnStatusFilterChanged(string value) { Page = 1; _ = ReloadAsync(); }
    partial void OnFromDateChanged(DateTime? value) { Page = 1; _ = ReloadAsync(); }
    partial void OnToDateChanged(DateTime? value) { Page = 1; _ = ReloadAsync(); }

    [RelayCommand]
    public async Task ReloadAsync()
    {
        if (_context.ActiveStoreId is not long sid) return;
        IsBusy = true;
        try
        {
            var status = string.IsNullOrEmpty(StatusFilter) ? null : StatusFilter;
            // 与 BS 端一致：结束日期含当天（+1 天作上界）
            DateTime? from = FromDate?.Date;
            DateTime? to = ToDate?.Date.AddDays(1);
            var paged = await _api.GetComplaintsAsync(storeId: sid, status: status, from: from, to: to, page: Page, pageSize: PageSize);
            Rows = new ObservableCollection<ComplaintRowViewModel>(paged.Items.Select(c => new ComplaintRowViewModel(c)));
            Total = paged.Total;

            var staff = await _api.GetStaffAsync(pageSize: 100, role: "Technician", storeId: sid);
            _technicians = staff.Items.Where(s => s.IsActive).ToList();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
        finally { IsBusy = false; }
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

    /// <summary>登记投诉：查询订单(按技师+日期)选服务项登记，或不指定项目做匿名投诉。</summary>
    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (_context.ActiveStoreId is not long sid) { MessageBox.Show("请先选择门店"); return; }
        var dlg = new Views.ComplaintCreateWindow(_api, sid, _technicians) { Owner = Application.Current?.MainWindow };
        if (dlg.ShowDialog() == true) await ReloadAsync();
    }

    [RelayCommand]
    private async Task ResolveAsync(ComplaintDto? c)
    {
        if (c is null) return;
        if (c.Status != "Pending") { MessageBox.Show("仅待处理的投诉可处理"); return; }
        var dlg = new Views.ComplaintResolveWindow(c, _technicians);
        if (dlg.ShowDialog() != true) return;
        try
        {
            await _api.ResolveComplaintAsync(c.Id, dlg.BuildRequest());
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    [RelayCommand]
    private async Task CancelAsync(ComplaintDto? c)
    {
        if (c is null) return;
        if (c.Status != "Pending") { MessageBox.Show("仅待处理的投诉可取消"); return; }
        if (MessageBox.Show($"确认取消订单 {c.OrderNo} 上的这条投诉记录？", "提示",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            await _api.CancelComplaintAsync(c.Id);
            await ReloadAsync();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }
}

/// <summary>投诉列表行：对齐 BS 列（标签拆分、状态/处理结果中文化、登记/处理人时间、操作可见性）。</summary>
public class ComplaintRowViewModel
{
    private static readonly Dictionary<string, string> LabelMap = new()
    {
        ["Pending"] = "待处理", ["Resolved"] = "已处理", ["Cancelled"] = "已取消",
        ["Reassigned"] = "改派", ["Refunded"] = "退款", ["Apologized"] = "道歉/补偿", ["NoAction"] = "不予处理"
    };

    public ComplaintDto Dto { get; }
    public ComplaintRowViewModel(ComplaintDto dto) => Dto = dto;

    public string? OrderNo => Dto.OrderNo;
    public string? ServiceName => Dto.ServiceName;
    public string? OriginalTechnicianName => Dto.OriginalTechnicianName;
    public string? MemberName => Dto.MemberName;
    public string? Comment => Dto.Comment;
    public bool HasComment => !string.IsNullOrWhiteSpace(Dto.Comment);

    /// <summary>标签按逗号拆成块（对齐 BS el-tag）。</summary>
    public IReadOnlyList<string> TagList =>
        string.IsNullOrWhiteSpace(Dto.Tags)
            ? Array.Empty<string>()
            : Dto.Tags.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();

    public string StatusLabel => LabelMap.TryGetValue(Dto.Status, out var l) ? l : Dto.Status;
    /// <summary>状态底色：待处理橙、已处理绿、其它灰（对齐 BS tag type）。</summary>
    public System.Windows.Media.Brush StatusBrush => Dto.Status switch
    {
        "Pending" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE6, 0xA2, 0x3C)),
        "Resolved" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x6A, 0x4F)),
        _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x90, 0x93, 0x99))
    };

    public bool HasResolution => !string.IsNullOrEmpty(Dto.Resolution);
    public string ResolutionLabel =>
        Dto.Resolution is { } r && LabelMap.TryGetValue(r, out var l) ? l : (Dto.Resolution ?? "—");
    public bool HasReassign => !string.IsNullOrWhiteSpace(Dto.ReassignedToTechnicianName);
    public string ReassignText => $"→ {Dto.ReassignedToTechnicianName}";

    // ---- 登记 / 处理：处理人 日期 标签 ----
    public string RecordedLine => Dto.RecordedByName ?? "—";
    public string CreatedAtText => Dto.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    /// <summary>已处理或已取消都有处理人 + 时间（取消也记录操作人）。</summary>
    public bool HasResolved => !string.IsNullOrWhiteSpace(Dto.ResolvedByName);
    public string ResolvedByName => Dto.ResolvedByName ?? "—";
    public string ResolvedAtText => Dto.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;

    public bool IsCancelled => Dto.Status == "Cancelled";
    /// <summary>处理结果小标签文案：已取消 → "已取消"；已处理 → 处理方式。</summary>
    public string ResultTagLabel => IsCancelled ? "已取消" : (HasResolution ? ResolutionLabel : string.Empty);
    public bool HasResultTag => !string.IsNullOrEmpty(ResultTagLabel);
    /// <summary>小标签底色：已取消红、改派蓝、退款橙、道歉/补偿绿、不予处理灰。</summary>
    public System.Windows.Media.Brush ResultTagBrush => IsCancelled
        ? Rgb(0xF5, 0x6C, 0x6C)
        : Dto.Resolution switch
        {
            "Reassigned" => Rgb(0x40, 0x96, 0xFF),
            "Refunded" => Rgb(0xE6, 0xA2, 0x3C),
            "Apologized" => Rgb(0x2D, 0x6A, 0x4F),
            _ => Rgb(0x90, 0x93, 0x99)
        };

    private static System.Windows.Media.Brush Rgb(byte r, byte g, byte b) =>
        new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));

    /// <summary>仅待处理可处理 / 取消。</summary>
    public bool IsPending => Dto.Status == "Pending";
}
