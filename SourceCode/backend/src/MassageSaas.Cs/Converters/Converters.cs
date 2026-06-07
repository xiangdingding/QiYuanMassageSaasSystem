using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Converters;

/// <summary>会员类型下拉项的灰色副标题（对齐 BS：充值卡 · 最低¥500 / 计次卡 · 服务 · 最少N次 [· X.X折]）。</summary>
public class MemberTypeMetaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MemberTypeDto t) return string.Empty;
        string meta = t.Kind == "StoredValue"
            ? $"充值卡 · 最低¥{t.MinRechargeAmount ?? 0m:0}"
            : $"计次卡 · {(string.IsNullOrEmpty(t.ServiceItemName) ? "" : t.ServiceItemName + " · ")}最少{t.MinPurchaseCount ?? 1}次";
        if (t.Discount < 1m) meta += $" · {t.Discount * 10m:0.0}折";
        return meta;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>资金流水类型 → 文字色（对齐 BS el-tag 着色）：充值绿/退卡橙/转出红/转入蓝/返佣紫。</summary>
public class RechargeKindBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = (value as string) switch
        {
            "Refund" => "#E6A23C",
            "TransferOut" => "#D9534F",
            "TransferIn" => "#409EFF",
            "ReferralBonus" => "#8957A1",
            _ => "#2D6A4F",       // Recharge
        };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return Visibility.Collapsed;
        if (value is string s && string.IsNullOrEmpty(s)) return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>NullToCollapsed 的反向：值为 null/空时可见，有值时折叠。用于"未应用券→显示输入区"。</summary>
public class InverseNullToCollapsedConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return Visibility.Visible;
        if (value is string s && string.IsNullOrEmpty(s)) return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CountToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var threshold = 1;
        if (parameter is string p && int.TryParse(p, out var n)) threshold = n;
        if (value is int i && i >= threshold) return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

public class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? parameter ?? string.Empty : Binding.DoNothing;
}

public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null && (value is not string s || !string.IsNullOrEmpty(s));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class DecimalGreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d) return d > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>把房间类型从历史英文值 standard/vip/couple 显示为中文。</summary>
public class RoomTypeDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var raw = value?.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        return raw.ToLowerInvariant() switch
        {
            "standard" => "标准间",
            "vip" => "VIP",
            "couple" => "情侣间",
            _ => raw
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>把服务评价分值（1-5）显示为满意度中文：5 非常满意 … 1 非常不满意。</summary>
public class RatingToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rating = value switch
        {
            int i => i,
            long l => (int)l,
            _ => int.TryParse(value?.ToString(), out var n) ? n : 0
        };
        return rating switch
        {
            5 => "非常满意",
            4 => "满意",
            3 => "一般",
            2 => "不满意",
            1 => "非常不满意",
            _ => string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>会员卡到期：null 显示"永久"，否则只显示到期日期 yyyy-MM-dd（到当天 23:59:59）。</summary>
public class CardExpiryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime dt ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "永久";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>会员卡剩余天数：null=永久，负=已过期，0=今天到期，正=剩 N 天。</summary>
public class CardDaysRemainingConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int d) return "永久";
        return d < 0 ? "已过期" : d == 0 ? "今天到期" : $"剩 {d} 天";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>差额着色：0=绿（持平）、负=红（短款）、正=橙（长款）。对齐 BS variance 颜色。</summary>
public class VarianceToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Ok = new((Color)ColorConverter.ConvertFromString("#67C23A")!);
    private static readonly SolidColorBrush Short = new((Color)ColorConverter.ConvertFromString("#F56C6C")!);
    private static readonly SolidColorBrush Over = new((Color)ColorConverter.ConvertFromString("#E6A23C")!);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value switch { decimal d => d, double db => (decimal)db, _ => 0m };
        if (System.Math.Abs(v) <= 0.005m) return Ok;
        return v < 0 ? Short : Over;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>投诉率着色：>5% 红，否则绿（对齐 BS 技师质量）。</summary>
public class ComplaintRateToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush High = new((Color)ColorConverter.ConvertFromString("#D9534F")!);
    private static readonly SolidColorBrush Ok = new((Color)ColorConverter.ConvertFromString("#2D6A4F")!);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var v = value switch { decimal d => (double)d, double db => db, int i => i, _ => 0d };
        return v > 5 ? High : Ok;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>布尔→中文标签：ConverterParameter="真值文案|假值文案"，如 "在职|停用"、"盲人|—"。</summary>
public class BoolLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter as string ?? "是|否").Split('|');
        var yes = parts.Length > 0 ? parts[0] : "是";
        var no = parts.Length > 1 ? parts[1] : "否";
        return value is true ? yes : no;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>请假类型/状态中文化（与 BS leaveTypeLabel/leaveStatusLabel 一致；Pending 在此为"待审批"而非订单的"待结账"）。</summary>
public class LeaveLabelConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["Sick"] = "病假", ["Personal"] = "事假", ["Annual"] = "年假", ["Training"] = "培训",
        ["Pending"] = "待审批", ["Approved"] = "已通过", ["Rejected"] = "已驳回", ["Cancelled"] = "已撤销"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value?.ToString() ?? string.Empty;
        return Map.TryGetValue(s, out var label) ? label : s;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>投诉状态/处理方式中文化（Pending 在此为"待处理"）。与 BS statusLabel/resolutionLabel 一致。</summary>
public class ComplaintLabelConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["Pending"] = "待处理", ["Resolved"] = "已处理", ["Cancelled"] = "已取消",
        ["Reassigned"] = "改派", ["Refunded"] = "退款", ["Apologized"] = "道歉/补偿", ["NoAction"] = "不予处理"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(s)) return "—";
        return Map.TryGetValue(s, out var label) ? label : s;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class EnumLabelConverter : IValueConverter
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["Active"] = "活跃", ["Expired"] = "已过期", ["Disabled"] = "已停用",
        ["Idle"] = "空闲", ["OnDuty"] = "在岗", ["Resting"] = "休息", ["OffDuty"] = "下班",
        ["Cash"] = "现金", ["MemberCard"] = "会员卡", ["Wechat"] = "微信",
        ["Alipay"] = "支付宝", ["BankCard"] = "银行卡", ["Unpaid"] = "未支付",
        ["Pending"] = "待结账", ["InProgress"] = "服务中", ["Completed"] = "已完成",
        ["Cancelled"] = "已取消", ["Refunded"] = "已退款",
        ["FixedAmount"] = "固定金额", ["Percentage"] = "百分比", ["Tiered"] = "阶梯", ["Timed"] = "按时计费",
        ["ShopOwner"] = "店主", ["StoreManager"] = "店长", ["Cashier"] = "收银员", ["Technician"] = "技师",
        ["OnlinePayment"] = "在线支付", ["OfflineManual"] = "线下激活",
        // 工资单状态 + 工资奖扣类型
        ["Draft"] = "草稿", ["Locked"] = "已封盘", ["Paid"] = "已发放",
        ["Bonus"] = "奖金", ["Deduction"] = "扣款",
        // 库存出入库类型
        ["PurchaseIn"] = "采购入库", ["Consume"] = "领用出库", ["Adjust"] = "盘点调整", ["Discard"] = "报损",
        // 会员资金流水类型
        ["Recharge"] = "充值", ["Refund"] = "退卡", ["TransferOut"] = "转出", ["TransferIn"] = "转入", ["ReferralBonus"] = "返佣"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        var s = value.ToString() ?? string.Empty;
        return Map.TryGetValue(s, out var label) ? label : s;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
