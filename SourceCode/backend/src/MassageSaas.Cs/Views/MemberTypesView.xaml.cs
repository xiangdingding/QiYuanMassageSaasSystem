using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.ViewModels;

namespace MassageSaas.Cs.Views;

public partial class MemberTypesView : UserControl
{
    public MemberTypesView()
    {
        InitializeComponent();
    }

    /// <summary>卡种筛选标签切换：把选中段的 CommandParameter（""/StoredValue/CountBased）写回 VM，触发重新加载。</summary>
    private void KindFilter_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is MemberTypesViewModel vm && sender is RadioButton rb)
            vm.FilterKind = rb.CommandParameter as string ?? string.Empty;
    }
}
