using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Complaints;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 登记投诉：选被投诉技师 + 日期「查询订单」拉出当日已完成服务项 → 选一项登记；
/// 或打开「不指定项目」做匿名投诉（仅文字，可选技师）。逻辑与 BS 端 openCreate / submitCreate 一致。
/// </summary>
public partial class ComplaintCreateWindow : Window
{
    private readonly IApiClient _api;
    private readonly long _storeId;

    /// <summary>常用投诉标签：勾选多选，不够再手动补充。</summary>
    private static readonly string[] TagPresets =
        { "态度差", "力度不合适", "技术生疏", "迟到/超时", "卫生不佳", "环境嘈杂", "乱收费", "中途离岗" };
    private readonly List<TagOption> _tagOptions =
        TagPresets.Select(t => new TagOption { Label = t }).ToList();

    public ComplaintCreateWindow(IApiClient api, long storeId, List<StaffDto> technicians)
    {
        InitializeComponent();
        _api = api;
        _storeId = storeId;
        TechBox.ItemsSource = technicians;
        DateBox.SelectedDate = DateTime.Now.Date;
        TagOptionsControl.ItemsSource = _tagOptions;
    }

    private bool IsAnonymous => AnonymousBox.IsChecked == true;

    private void Anonymous_Changed(object sender, RoutedEventArgs e)
    {
        if (ItemsPanel is null) return;
        // 不指定项目：隐藏订单项区，技师与日期变为可选
        ItemsPanel.Visibility = IsAnonymous ? Visibility.Collapsed : Visibility.Visible;
        DateBox.Visibility = IsAnonymous ? Visibility.Collapsed : Visibility.Visible;
        LookupButton.Visibility = IsAnonymous ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Tech_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsAnonymous && TechBox.SelectedItem is StaffDto && DateBox.SelectedDate is not null)
            _ = LookupAsync();
    }

    private async void Lookup_Click(object sender, RoutedEventArgs e) => await LookupAsync();

    private async System.Threading.Tasks.Task LookupAsync()
    {
        if (TechBox.SelectedItem is not StaffDto tech) { Warn("请先选择被投诉技师"); return; }
        if (DateBox.SelectedDate is not DateTime date) { Warn("请选择日期"); return; }
        try
        {
            var items = await _api.GetServedItemsByTechnicianAsync(_storeId, tech.Id, date.Date);
            ItemsGrid.ItemsSource = items.Select(i => new ServedItemRow(i)).ToList();
            ItemsHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ItemsHint.Text = "该技师在该日没有已完成的服务项目。";
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        var tech = TechBox.SelectedItem as StaffDto;
        // 勾选标签 + 手动补充，合并为逗号分隔
        var picked = _tagOptions.Where(o => o.IsSelected).Select(o => o.Label);
        var manual = (TagsBox.Text ?? string.Empty)
            .Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => s.Length > 0);
        var allTags = picked.Concat(manual).ToList();
        var tags = allTags.Count > 0 ? string.Join(",", allTags) : null;
        var comment = string.IsNullOrWhiteSpace(CommentBox.Text) ? null : CommentBox.Text.Trim();

        CreateComplaintRequest req;
        if (IsAnonymous)
        {
            req = new CreateComplaintRequest(
                OrderItemId: null, Tags: tags, Comment: comment,
                StoreId: _storeId, TechnicianId: tech?.Id);
        }
        else
        {
            if (ItemsGrid.SelectedItem is not ServedItemRow row)
            {
                Warn("请先查询并选择被投诉的服务项");
                return;
            }
            if (row.HasPendingComplaint)
            {
                Warn("该服务项已有待处理投诉，不能重复登记");
                return;
            }
            req = new CreateComplaintRequest(OrderItemId: row.ItemId, Tags: tags, Comment: comment);
        }

        try
        {
            SubmitButton.IsEnabled = false;
            await _api.CreateComplaintAsync(req);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorReporter.Show(ex);
            SubmitButton.IsEnabled = true;
        }
    }

    private static void Warn(string msg) =>
        MessageBox.Show(msg, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    /// <summary>常用标签勾选项：ToggleButton 双向绑定 IsSelected，提交时读取。</summary>
    private sealed class TagOption
    {
        public string Label { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}

/// <summary>已完成服务项行：会员展示带散客兜底、是否已存在待处理投诉。</summary>
public class ServedItemRow
{
    public TechnicianServedItemDto Dto { get; }
    public ServedItemRow(TechnicianServedItemDto dto) => Dto = dto;

    public long ItemId => Dto.ItemId;
    public string OrderNo => Dto.OrderNo;
    public string ServiceName => Dto.ServiceName;
    public DateTime? CompletedAt => Dto.CompletedAt;
    public decimal Amount => Dto.Amount;
    public string MemberDisplay => Dto.MemberName ?? Dto.MemberCardNo ?? "散客";
    public bool HasPendingComplaint => Dto.HasPendingComplaint;
    public string StatusLabel => Dto.HasPendingComplaint ? "已投诉" : string.Empty;
}
