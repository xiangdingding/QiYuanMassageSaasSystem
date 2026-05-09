using System.Windows.Controls;
using MassageSaas.Cs.ViewModels.Pos;

namespace MassageSaas.Cs.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    private void Pay_Cash_Click(object sender, System.Windows.RoutedEventArgs e) => SetMethod("Cash");
    private void Pay_Member_Click(object sender, System.Windows.RoutedEventArgs e) => SetMethod("MemberCard");
    private void Pay_Wechat_Click(object sender, System.Windows.RoutedEventArgs e) => SetMethod("Wechat");
    private void Pay_Alipay_Click(object sender, System.Windows.RoutedEventArgs e) => SetMethod("Alipay");
    private void Pay_Bank_Click(object sender, System.Windows.RoutedEventArgs e) => SetMethod("BankCard");

    private void SetMethod(string m)
    {
        if (DataContext is PosViewModel vm) vm.PayMethod = m;
    }
}
