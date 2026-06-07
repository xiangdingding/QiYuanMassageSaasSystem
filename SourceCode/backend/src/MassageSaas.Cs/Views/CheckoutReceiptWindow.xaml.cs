using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Orders;
using MassageSaas.Shared.Reviews;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 结账成功对话框（对齐 BS 端 PosView 的「结账成功」el-dialog）：
/// 展示订单摘要（订单号/合计/实收/消费次数/找零/优惠）+ 消费明细 + 逐项满意度评价 + 计时房费明细。
/// 点「完成」按当前满意度（默认满意=4）提交各项评价后关闭。
/// </summary>
public partial class CheckoutReceiptWindow : Window
{
    private readonly IApiClient _api;
    private readonly long _orderId;
    private readonly List<ReceiptItemRow> _rows;

    public CheckoutReceiptWindow(IApiClient api, OrderDto order, string payMethod, decimal cashReceived)
    {
        InitializeComponent();
        _api = api;
        _orderId = order.Id;

        var change = payMethod == "Cash" && cashReceived > order.PaidAmount ? cashReceived - order.PaidAmount : 0m;
        var headlineTotal = order.ListTotal > 0 ? order.ListTotal : order.Total;
        var punch = order.PunchCardUsedCount;

        OrderNoText.Text = $"订单号：{order.OrderNo}";
        TotalText.Text = punch > 0
            ? $"合计：¥{headlineTotal:F2}（面值，含次卡抵扣）"
            : $"合计：¥{headlineTotal:F2}";
        PaidText.Text = $"实收：¥{order.PaidAmount:F2}（{PayMethodLabel(payMethod)}）";

        if (punch > 0)
        {
            PunchText.Text = $"消费次数：{punch} 次（次卡核销）";
            PunchText.Visibility = Visibility.Visible;
        }
        if (change > 0)
        {
            ChangeText.Text = $"找零：¥{change:F2}";
            ChangeText.Visibility = Visibility.Visible;
        }
        if (order.DiscountAmount > 0)
        {
            DiscountText.Text = $"优惠：¥{order.DiscountAmount:F2}";
            DiscountText.Visibility = Visibility.Visible;
        }

        _rows = order.Items.Select(i => new ReceiptItemRow
        {
            ItemId = i.Id,
            ServiceName = i.ServiceName,
            TechnicianName = i.TechnicianName ?? "—",
            AssignTag = AssignTagOf(i.AssignmentSource),
            QuantityText = $"{i.Quantity} 次",
            AmountText = $"¥{ItemListAmount(i):F2}",
            IsPunch = i.MemberPackageId.HasValue,
            Rating = 4 // 默认满意
        }).ToList();
        ItemsGrid.ItemsSource = _rows;
        ReviewItems.ItemsSource = _rows;

        if (order.RoomCharges is { Count: > 0 } charges)
        {
            RoomGrid.ItemsSource = charges.Select(r => new ReceiptRoomRow
            {
                RoomNo = r.RoomNo,
                MinutesText = $"{r.Minutes} 分钟",
                RateText = $"¥{r.HourlyRate:F2}/时",
                AmountText = $"¥{r.Amount:F2}"
            }).ToList();
            RoomChargePanel.Visibility = Visibility.Visible;
        }
    }

    private async void Finish_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var r in _rows)
                await _api.SubmitReviewAsync(new SubmitReviewRequest(_orderId, r.ItemId, r.Rating, null, null));
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { ErrorReporter.Show(ex); }
    }

    private static string PayMethodLabel(string m) => m switch
    {
        "Cash" => "现金",
        "MemberCard" => "会员卡",
        "Wechat" => "微信",
        "Alipay" => "支付宝",
        "BankCard" => "银行卡",
        _ => m
    };

    private static string AssignTagOf(string source) => source switch
    {
        "Rotation" => "轮",
        "Designation" => "点",
        _ => string.Empty
    };

    /// <summary>明细面值小计：优先 ListAmount，否则按 ListUnitPrice 算，再不济退回 ItemTotal。</summary>
    private static decimal ItemListAmount(OrderItemDto i)
    {
        if (i.ListAmount > 0) return i.ListAmount;
        if (i.ListUnitPrice > 0) return Math.Round(i.ListUnitPrice * i.Quantity, 2);
        return i.ItemTotal;
    }
}

/// <summary>结账成功明细行：含逐项满意度（复用 CheckoutReviewWindow 的档位）。</summary>
public class ReceiptItemRow
{
    public long ItemId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string TechnicianName { get; set; } = string.Empty;
    public string AssignTag { get; set; } = string.Empty;
    public bool ShowAssignTag => !string.IsNullOrEmpty(AssignTag);
    public string QuantityText { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public bool IsPunch { get; set; }
    public int Rating { get; set; } = 4;
    public IReadOnlyList<RatingOption> Options => CheckoutReviewWindow.RatingOptions;
    public string SatisfactionAutomationName => $"{ServiceName} {TechnicianName} 满意度";
    /// <summary>服务评价区每行标签：服务名 · 技师。</summary>
    public string ReviewLabel => $"{ServiceName} · {TechnicianName}";
}

/// <summary>结账成功里的计时房费明细行。</summary>
public class ReceiptRoomRow
{
    public string RoomNo { get; set; } = string.Empty;
    public string MinutesText { get; set; } = string.Empty;
    public string RateText { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
}
