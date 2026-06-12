using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MassageSaas.Cs.ViewModels;

namespace MassageSaas.Cs.Views;

public partial class ComplaintsView : UserControl
{
    public ComplaintsView() => InitializeComponent();

    /// <summary>状态分段标签切换：把选中段的 CommandParameter（Pending/Resolved/Cancelled/""）写回 VM，触发重查。</summary>
    private void StatusFilter_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is ComplaintsViewModel vm && sender is RadioButton rb)
            vm.StatusFilter = rb.CommandParameter as string ?? string.Empty;
    }

    /// <summary>点击日期文本区域即展开日历（默认只有右侧小按钮能展开，这里让整块都能点）。</summary>
    private void DatePicker_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DatePicker dp) return;
        if (e.OriginalSource is DependencyObject d && FindAncestor<ButtonBase>(d) is not null) return;
        if (dp.IsDropDownOpen) return;
        dp.Dispatcher.BeginInvoke(new Action(() => dp.IsDropDownOpen = true), DispatcherPriority.Input);
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T t) return t;
            node = VisualTreeHelper.GetParent(node);
        }
        return null;
    }
}
