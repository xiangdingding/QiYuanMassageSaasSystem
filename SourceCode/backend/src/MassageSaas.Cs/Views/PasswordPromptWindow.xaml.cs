using System.Windows;

namespace MassageSaas.Cs.Views;

public partial class PasswordPromptWindow : Window
{
    public string NewPassword { get; private set; } = string.Empty;

    public PasswordPromptWindow(string hint)
    {
        InitializeComponent();
        HintBlock.Text = hint;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Pwd1.Password) || Pwd1.Password.Length < 6)
        {
            ErrBlock.Text = "密码至少 6 位";
            return;
        }
        if (Pwd1.Password != Pwd2.Password)
        {
            ErrBlock.Text = "两次输入不一致";
            return;
        }
        NewPassword = Pwd1.Password;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
