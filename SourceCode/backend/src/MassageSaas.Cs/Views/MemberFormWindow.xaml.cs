using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Members;

namespace MassageSaas.Cs.Views;

public partial class MemberFormWindow : Window
{
    private readonly MemberDto? _editing;
    private readonly long _storeId;

    public MemberFormWindow(MemberDto? editing, long storeId)
    {
        InitializeComponent();
        _editing = editing;
        _storeId = storeId;
        if (editing is not null)
        {
            Title = $"编辑会员 - {editing.CardNo}";
            CardNoBox.Text = editing.CardNo;
            CardNoBox.IsEnabled = false;
            PhoneBox.Text = editing.Phone;
            NameBox.Text = editing.Name ?? string.Empty;
            DiscountBox.Text = editing.Discount.ToString("F2");
            foreach (ComboBoxItem item in GenderBox.Items)
            {
                if ((item.Content as string) == (editing.Gender ?? string.Empty))
                {
                    GenderBox.SelectedItem = item;
                    break;
                }
            }
            InitialBalanceBox.IsEnabled = false;
        }
        else
        {
            Title = "开卡";
        }
    }

    private decimal ParseDecimal(TextBox box, decimal fallback)
    {
        return decimal.TryParse(box.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : fallback;
    }

    private string? Gender =>
        (GenderBox.SelectedItem as ComboBoxItem)?.Content as string is { Length: > 0 } g ? g : null;

    public CreateMemberRequest BuildCreateRequest() => new(
        StoreId: _storeId,
        CardNo: CardNoBox.Text.Trim(),
        Phone: PhoneBox.Text.Trim(),
        Name: string.IsNullOrWhiteSpace(NameBox.Text) ? null : NameBox.Text.Trim(),
        Gender: Gender,
        Birthday: null,
        Discount: ParseDecimal(DiscountBox, 1m),
        InitialBalance: ParseDecimal(InitialBalanceBox, 0m),
        Remark: null);

    public UpdateMemberRequest BuildUpdateRequest() => new(
        Phone: PhoneBox.Text.Trim(),
        Name: string.IsNullOrWhiteSpace(NameBox.Text) ? null : NameBox.Text.Trim(),
        Gender: Gender,
        Birthday: null,
        Discount: ParseDecimal(DiscountBox, 1m),
        Remark: null);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CardNoBox.Text) || string.IsNullOrWhiteSpace(PhoneBox.Text))
        {
            MessageBox.Show("卡号与手机号必填");
            return;
        }
        var d = ParseDecimal(DiscountBox, -1m);
        if (d <= 0 || d > 1)
        {
            MessageBox.Show("折扣需在 (0, 1] 之间");
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
