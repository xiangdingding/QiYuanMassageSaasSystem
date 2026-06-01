using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Reviews;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 门店端"代客录入评价"：选技师 + 日期 → 查当日已完成服务项 → 选一项打分。
/// 与投诉登记同一选项模式（GET /orders/items/by-technician），店员身份直接提交。
/// </summary>
public partial class ReviewCreateWindow : Window
{
    private readonly IApiClient _api;
    private readonly long _storeId;

    public ReviewCreateWindow(IApiClient api, long storeId)
    {
        InitializeComponent();
        _api = api;
        _storeId = storeId;
        DateBox.SelectedDate = System.DateTime.Today;
        RatingBox.SelectedIndex = 0; // 默认 5 星
        Loaded += async (_, _) => await LoadTechniciansAsync();
    }

    private async System.Threading.Tasks.Task LoadTechniciansAsync()
    {
        try
        {
            var resp = await _api.GetStaffAsync(role: "Technician", storeId: _storeId, page: 1, pageSize: 200);
            TechBox.ItemsSource = resp.Items
                .Select(s => new TechOption(s.Id, $"{s.RealName ?? s.Username}（工号 {(s.EmployeeNo?.ToString() ?? "—")}）"))
                .ToList();
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private async void Query_Click(object sender, RoutedEventArgs e)
    {
        if (TechBox.SelectedValue is not long tid) { MessageBox.Show("请先选择技师"); return; }
        if (DateBox.SelectedDate is not System.DateTime d) { MessageBox.Show("请先选择日期"); return; }
        try
        {
            var rows = await _api.GetServedItemsByTechnicianAsync(_storeId, tid, d.Date);
            ItemsGrid.ItemsSource = rows;
            if (rows.Count == 0) MessageBox.Show("该技师在该日没有已完成的服务项目。");
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is not TechnicianServedItemDto item)
        {
            MessageBox.Show("请先在列表中选择要评价的服务项");
            return;
        }
        if (item.HasReview)
        {
            MessageBox.Show("该服务项已评价，不能重复评价");
            return;
        }
        if (RatingBox.SelectedItem is not ComboBoxItem ci || !int.TryParse((string)ci.Tag, out var rating))
        {
            MessageBox.Show("请选择评分");
            return;
        }
        try
        {
            await _api.SubmitReviewAsync(new SubmitReviewRequest(
                item.OrderId,
                item.ItemId,
                rating,
                string.IsNullOrWhiteSpace(TagsBox.Text) ? null : TagsBox.Text.Trim(),
                string.IsNullOrWhiteSpace(CommentBox.Text) ? null : CommentBox.Text.Trim()));
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>技师下拉项：Id 用于提交，Display 用于展示。</summary>
    private record TechOption(long Id, string Display);
}
