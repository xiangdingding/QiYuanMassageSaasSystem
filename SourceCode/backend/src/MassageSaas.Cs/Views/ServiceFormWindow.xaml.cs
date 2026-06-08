using System.Windows;
using MassageSaas.Shared.Services;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 新建/编辑服务项目。字段：编码/名称/时长/原价/会员价/说明/启用/排序。
/// 逻辑与 BS 端 ServicesView 的 save 一致（编码可改，排序新建默认 = 最大 + 1）。
/// </summary>
public partial class ServiceFormWindow : Window
{
    public ServiceFormWindow(ServiceItemDto? editing, int defaultSort)
    {
        InitializeComponent();
        if (editing is null)
        {
            SortBox.Value = defaultSort;
            return;
        }

        Title = $"编辑 - {editing.Name}";
        CodeBox.Text = editing.Code;
        NameBox.Text = editing.Name;
        DurationBox.Value = editing.DurationMinutes;
        PriceBox.Value = (double)editing.Price;
        MemberPriceBox.Value = (double)editing.MemberPrice;
        DescBox.Text = editing.Description ?? string.Empty;
        ActiveBox.IsChecked = editing.IsActive;
        SortBox.Value = editing.Sort;
    }

    public CreateServiceItemRequest BuildCreateRequest() => new(
        Code: CodeBox.Text.Trim(),
        Name: NameBox.Text.Trim(),
        DurationMinutes: (int)DurationBox.Value,
        Price: (decimal)PriceBox.Value,
        MemberPrice: (decimal)MemberPriceBox.Value,
        Description: string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true,
        Sort: (int)SortBox.Value);

    public UpdateServiceItemRequest BuildUpdateRequest() => new(
        Name: NameBox.Text.Trim(),
        DurationMinutes: (int)DurationBox.Value,
        Price: (decimal)PriceBox.Value,
        MemberPrice: (decimal)MemberPriceBox.Value,
        Description: string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true,
        Code: CodeBox.Text.Trim(),
        Sort: (int)SortBox.Value);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("编码与名称必填", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
