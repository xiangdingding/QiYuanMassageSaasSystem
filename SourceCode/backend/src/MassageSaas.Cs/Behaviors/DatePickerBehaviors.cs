using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MassageSaas.Cs.Behaviors;

/// <summary>
/// 日期选择器附加行为：点击输入框文本区域即展开日历（默认只有右侧日历按钮能展开）。
/// 通过全局样式 <c>FilterDatePicker</c> 的 OpenOnClick=True 统一启用，无需各页写后台代码。
/// </summary>
public static class DatePickerBehaviors
{
    public static readonly DependencyProperty OpenOnClickProperty =
        DependencyProperty.RegisterAttached(
            "OpenOnClick", typeof(bool), typeof(DatePickerBehaviors),
            new PropertyMetadata(false, OnOpenOnClickChanged));

    public static bool GetOpenOnClick(DependencyObject o) => (bool)o.GetValue(OpenOnClickProperty);
    public static void SetOpenOnClick(DependencyObject o, bool v) => o.SetValue(OpenOnClickProperty, v);

    private static void OnOpenOnClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DatePicker dp) return;
        if ((bool)e.NewValue)
            dp.PreviewMouseLeftButtonUp += OnPreviewMouseUp;
        else
            dp.PreviewMouseLeftButtonUp -= OnPreviewMouseUp;
    }

    private static void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DatePicker dp) return;
        if (e.OriginalSource is DependencyObject src)
        {
            // 点清除按钮（×）：清空日期，不展开日历
            if (FindAncestorByName(src, "PART_ClearButton") is not null)
            {
                dp.SelectedDate = null;
                e.Handled = true;
                return;
            }
            // 点到右侧自带的日历按钮时交给它自己切换开/关，避免冲突
            if (FindAncestor<ButtonBase>(src) is not null) return;
        }
        if (dp.IsDropDownOpen) return;
        // 同步置 true 会被 DatePicker 自身的输入处理立刻收回，延到输入事件之后再展开
        dp.Dispatcher.BeginInvoke(new Action(() => dp.IsDropDownOpen = true), DispatcherPriority.Input);
    }

    private static FrameworkElement? FindAncestorByName(DependencyObject? node, string name)
    {
        while (node is not null)
        {
            if (node is FrameworkElement fe && fe.Name == name) return fe;
            node = VisualTreeHelper.GetParent(node);
        }
        return null;
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
