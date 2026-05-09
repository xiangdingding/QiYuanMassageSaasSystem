using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

public partial class StaffFormWindow : Window
{
    private readonly StaffDto? _editing;
    private readonly long _storeId;

    public StaffFormWindow(StaffDto? editing, long storeId)
    {
        InitializeComponent();
        _editing = editing;
        _storeId = storeId;
        if (editing is not null)
        {
            Title = $"编辑员工 - {editing.Username}";
            UsernameBox.Text = editing.Username;
            UsernameBox.IsEnabled = false;
            RealNameBox.Text = editing.RealName ?? string.Empty;
            PhoneBox.Text = editing.Phone ?? string.Empty;
            EmployeeNoBox.Text = editing.EmployeeNo?.ToString() ?? string.Empty;
            BlindBox.IsChecked = editing.IsBlind;
            ActiveBox.IsChecked = editing.IsActive;
            PasswordBox.IsEnabled = false;
            foreach (ComboBoxItem item in RoleBox.Items)
            {
                if ((item.Content as string) == editing.Role) { RoleBox.SelectedItem = item; break; }
            }
        }
    }

    private string Role =>
        ((RoleBox.SelectedItem as ComboBoxItem)?.Content as string) ?? "Technician";

    private int? EmpNo => int.TryParse(EmployeeNoBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    public CreateStaffRequest BuildCreateRequest() => new(
        StoreId: _storeId,
        Username: UsernameBox.Text.Trim(),
        Password: PasswordBox.Password,
        Role: Role,
        RealName: string.IsNullOrWhiteSpace(RealNameBox.Text) ? null : RealNameBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        EmployeeNo: EmpNo,
        IsBlind: BlindBox.IsChecked == true);

    public UpdateStaffRequest BuildUpdateRequest() => new(
        StoreId: _storeId,
        Role: Role,
        RealName: string.IsNullOrWhiteSpace(RealNameBox.Text) ? null : RealNameBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        EmployeeNo: EmpNo,
        IsBlind: BlindBox.IsChecked == true,
        IsActive: ActiveBox.IsChecked == true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsernameBox.Text))
        {
            MessageBox.Show("用户名必填");
            return;
        }
        if (_editing is null && string.IsNullOrEmpty(PasswordBox.Password))
        {
            MessageBox.Show("新建员工需要设定初始密码");
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
