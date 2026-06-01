using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Reviews;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 结算后即时评价：列出本单各服务项，默认"满意"，收银员可逐项调整后提交。
/// 评分仍存 1-5 整数（满意=4）。跳过则不产生评价。
/// </summary>
public partial class CheckoutReviewWindow : Window
{
    private readonly IApiClient _api;
    private readonly List<ReviewRow> _rows;

    /// <summary>满意度档位（共享给每行 ComboBox）。</summary>
    public static readonly IReadOnlyList<RatingOption> RatingOptions = new[]
    {
        new RatingOption(5, "非常满意"),
        new RatingOption(4, "满意"),
        new RatingOption(3, "一般"),
        new RatingOption(2, "不满意"),
        new RatingOption(1, "非常不满意")
    };

    public CheckoutReviewWindow(IApiClient api, OrderDto order)
    {
        InitializeComponent();
        _api = api;
        _rows = order.Items
            .Select(i => new ReviewRow
            {
                OrderId = order.Id,
                ItemId = i.Id,
                Label = $"{i.ServiceName} · {i.TechnicianName}",
                Rating = 4 // 默认满意
            })
            .ToList();
        RowsControl.ItemsSource = _rows;
    }

    /// <summary>点"完成"：按当前满意度（默认满意）提交各项评价后关闭。</summary>
    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var r in _rows)
                await _api.SubmitReviewAsync(new SubmitReviewRequest(r.OrderId, r.ItemId, r.Rating, null, null));
            DialogResult = true;
            Close();
        }
        catch (System.Exception ex) { ErrorReporter.Show(ex); }
    }
}

/// <summary>满意度选项：Value 存 1-5，Label 展示文字。</summary>
public record RatingOption(int Value, string Label);

/// <summary>评价行：每个订单项一行；Rating 由 ComboBox 双向写回。</summary>
public class ReviewRow
{
    public long OrderId { get; set; }
    public long ItemId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Rating { get; set; } = 4;
    public IReadOnlyList<RatingOption> Options => CheckoutReviewWindow.RatingOptions;
}
