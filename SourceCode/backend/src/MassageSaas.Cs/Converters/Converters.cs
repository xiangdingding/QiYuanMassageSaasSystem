using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MassageSaas.Cs.Converters;

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
        ["OnlinePayment"] = "在线支付", ["OfflineManual"] = "线下激活"
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
