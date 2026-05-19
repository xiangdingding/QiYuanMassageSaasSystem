using System.Globalization;
using System.Windows;

namespace MassageSaas.Cs.Views;

public partial class PayrollGenerateWindow : Window
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string? Remark { get; private set; }

    public PayrollGenerateWindow()
    {
        InitializeComponent();
        // 默认结算上一个自然月
        var lastMonth = System.DateTime.Today.AddMonths(-1);
        YearBox.Text = lastMonth.Year.ToString(CultureInfo.InvariantCulture);
        MonthBox.Text = lastMonth.Month.ToString(CultureInfo.InvariantCulture);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(YearBox.Text, out var y) || y < 2000 || y > 2100)
        {
            MessageBox.Show("请输入有效年份");
            return;
        }
        if (!int.TryParse(MonthBox.Text, out var m) || m < 1 || m > 12)
        {
            MessageBox.Show("月份应为 1-12");
            return;
        }
        Year = y;
        Month = m;
        Remark = string.IsNullOrWhiteSpace(RemarkBox.Text) ? null : RemarkBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
