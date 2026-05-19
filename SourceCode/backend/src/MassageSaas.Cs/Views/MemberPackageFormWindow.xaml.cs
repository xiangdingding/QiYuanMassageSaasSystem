using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.MemberPackages;
using MassageSaas.Shared.Members;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.Views;

public partial class MemberPackageFormWindow : Window
{
    public record MemberPick(long Id, string Display);
    public record ServicePick(long? Id, string Display);

    private readonly long _storeId;

    public MemberPackageFormWindow(long storeId, IEnumerable<MemberDto> members, IEnumerable<ServiceItemDto> services)
    {
        InitializeComponent();
        _storeId = storeId;

        MemberBox.ItemsSource = members
            .Where(m => m.IsActive)
            .Select(m => new MemberPick(m.Id, $"{m.Name ?? "（未填名）"}　{m.CardNo}"))
            .ToList();

        var svc = new List<ServicePick> { new(null, "（不限服务）") };
        svc.AddRange(services.Where(s => s.IsActive).Select(s => new ServicePick(s.Id, s.Name)));
        ServiceBox.ItemsSource = svc;
        ServiceBox.SelectedIndex = 0;

        KindBox.SelectedIndex = 0;
        if (MemberBox.Items.Count > 0) MemberBox.SelectedIndex = 0;
    }

    private string SelectedKind() =>
        (KindBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Counter";

    private void KindBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CountLabel is null) return;
        var counter = SelectedKind() == "Counter";
        CountLabel.Text = counter ? "总次数（计次卡）" : "总次数（期限卡可填 0）";
        ExpiryLabel.Text = counter ? "到期日期（可空）" : "到期日期（期限卡必填）";
    }

    private decimal ParseDec(TextBox b, decimal fallback)
        => decimal.TryParse(b.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    private int ParseInt(TextBox b, int fallback)
        => int.TryParse(b.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public CreateMemberPackageRequest BuildRequest()
    {
        var member = (MemberPick)MemberBox.SelectedItem;
        var service = (ServicePick)ServiceBox.SelectedItem;
        return new CreateMemberPackageRequest(
            MemberId: member.Id,
            StoreId: _storeId,
            Kind: SelectedKind(),
            ServiceId: service.Id,
            Title: TitleBox.Text.Trim(),
            PaidAmount: ParseDec(PaidBox, 0m),
            TotalCount: ParseInt(CountBox, 0),
            ValidFrom: FromBox.SelectedDate?.Date,
            ExpiresAt: ExpiryBox.SelectedDate?.Date,
            Remark: string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim());
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (MemberBox.SelectedItem is null) { MessageBox.Show("请选择会员"); return; }
        if (string.IsNullOrWhiteSpace(TitleBox.Text)) { MessageBox.Show("请填写套餐名称"); return; }
        var counter = SelectedKind() == "Counter";
        if (counter && ParseInt(CountBox, 0) <= 0)
        {
            MessageBox.Show("计次卡的总次数必须大于 0");
            return;
        }
        if (!counter && ExpiryBox.SelectedDate is null)
        {
            MessageBox.Show("期限卡必须设置到期日期");
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
