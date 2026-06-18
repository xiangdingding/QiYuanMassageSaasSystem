using System.Windows;

namespace MassageSaas.Cs.Views;

/// <summary>工资单明细大窗：宿主 PayrollViewModel，展示某月工资单的员工明细 + 选中员工的奖扣明细，
/// 草稿态可改加班/满勤、录入/删除奖扣。DataContext 由调用方（PayrollViewModel）注入为自身实例。</summary>
public partial class PayrollDetailWindow : Window
{
    public PayrollDetailWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
