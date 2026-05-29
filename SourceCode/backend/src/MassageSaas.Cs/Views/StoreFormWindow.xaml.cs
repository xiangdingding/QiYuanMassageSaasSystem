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
            CutoffBox.Text = FormatCutoff(editing.DayCloseCutoffMinutes);
        }
    }

    private long? ParentId =>
        long.TryParse(ParentBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private int Cutoff => ParseCutoff(CutoffBox.Text);

    public CreateStoreRequest BuildCreateRequest() => new(
        Name: NameBox.Text.Trim(),
        Address: string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        ParentStoreId: ParentId,
        DayCloseCutoffMinutes: Cutoff);

    public UpdateStoreRequest BuildUpdateRequest() => new(
        Name: NameBox.Text.Trim(),
        Address: string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true,
        DayCloseCutoffMinutes: Cutoff);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { MessageBox.Show("名称必填"); return; }
        var cutoff = Cutoff;
        if (cutoff < 0 || cutoff > 1439)
        {
            MessageBox.Show("营业日切日时间格式应为 HH:mm，且在 00:00 ~ 23:59 之间");
            return;
        }
        DialogResult = true;
        Close();
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

    private static string FormatCutoff(int minutes)
    {
        var safe = Math.Clamp(minutes, 0, 1439);
        return $"{safe / 60:D2}:{safe % 60:D2}";
    }

    private static int ParseCutoff(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var parts = text.Trim().Split(':');
        if (parts.Length != 2) return -1;
        if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m)) return -1;
        if (h < 0 || h > 23 || m < 0 || m > 59) return -1;
        return h * 60 + m;
    }
}
