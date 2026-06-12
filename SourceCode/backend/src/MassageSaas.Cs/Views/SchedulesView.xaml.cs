using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MassageSaas.Cs.Views;

public partial class SchedulesView : UserControl
{
    public SchedulesView() => InitializeComponent();

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
