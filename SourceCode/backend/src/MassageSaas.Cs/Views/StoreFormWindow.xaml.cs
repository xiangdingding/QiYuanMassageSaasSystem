using System.Globalization;
using System.Windows;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs.Views;

public partial class StoreFormWindow : Window
{
    public StoreFormWindow(StoreDto? editing)
    {
        InitializeComponent();
        if (editing is not null)
        {
            Title = $"编辑门店 - {editing.Name}";
            NameBox.Text = editing.Name;
            AddressBox.Text = editing.Address ?? string.Empty;
            PhoneBox.Text = editing.Phone ?? string.Empty;
            ParentBox.Text = editing.ParentStoreId?.ToString() ?? string.Empty;
            ParentBox.IsEnabled = false;
            ActiveBox.IsChecked = editing.IsActive;
        }
    }

    private long? ParentId =>
        long.TryParse(ParentBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    public CreateStoreRequest BuildCreateRequest() => new(
        Name: NameBox.Text.Trim(),
        Address: string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        ParentStoreId: ParentId);

    public UpdateStoreRequest BuildUpdateRequest() => new(
        Name: NameBox.Text.Trim(),
        Address: string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { MessageBox.Show("名称必填"); return; }
        DialogResult = true;
        Close();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
