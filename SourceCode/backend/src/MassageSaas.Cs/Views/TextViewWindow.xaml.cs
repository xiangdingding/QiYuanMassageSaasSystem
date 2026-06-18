using System.Windows;

namespace MassageSaas.Cs.Views;

/// <summary>通用只读长文本查看窗口（协议、说明等）。键盘可滚动、读屏可读。</summary>
public partial class TextViewWindow : Window
{
    public TextViewWindow(string title, string content)
    {
        InitializeComponent();
        Title = title;
        HeaderText.Text = title;
        BodyText.Text = content;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
