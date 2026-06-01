using System.Windows;
using System.Windows.Controls;

namespace MassageSaas.Cs.Views;

/// <summary>简易"登记投诉"弹窗：收集 Tags + Comment。从订单详情的某行 OrderItem 触发。</summary>
public class ComplaintCreateDialog : Window
{
    private readonly TextBox _tagsBox;
    private readonly TextBox _commentBox;

    public string Tags => _tagsBox.Text;
    public string Comment => _commentBox.Text;

    public ComplaintCreateDialog(string serviceName, string technicianName)
    {
        Title = "登记投诉";
        Width = 460; Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock
        {
            Text = $"项目：{serviceName}",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"技师：{technicianName}",
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 12)
        });

        panel.Children.Add(new TextBlock { Text = "标签（逗号分隔，如：态度差,力度不合适）", Margin = new Thickness(0, 0, 0, 4) });
        _tagsBox = new TextBox { Padding = new Thickness(6), Margin = new Thickness(0, 0, 0, 12), MaxLength = 200 };
        panel.Children.Add(_tagsBox);

        panel.Children.Add(new TextBlock { Text = "描述（客户原话/补充）", Margin = new Thickness(0, 0, 0, 4) });
        _commentBox = new TextBox
        {
            Padding = new Thickness(6), Height = 72,
            AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxLength = 500
        };
        panel.Children.Add(_commentBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        var ok = new Button { Content = "登记投诉", Width = 100, IsDefault = true,
            Background = System.Windows.Media.Brushes.DarkOrange,
            Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(8, 4, 8, 4) };
        ok.Click += (_, _) => { DialogResult = true; Close(); };
        var cancel = new Button { Content = "取消", Width = 80, Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);

        Content = panel;
    }
}
