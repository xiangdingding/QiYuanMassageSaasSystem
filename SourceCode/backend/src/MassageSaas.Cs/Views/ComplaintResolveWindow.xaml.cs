using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Shared.Complaints;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.Views;

public partial class ComplaintResolveWindow : Window
{
    public record TechPick(long Id, string Name);

    private readonly long _originalTechnicianId;

    public ComplaintResolveWindow(ComplaintDto complaint, IEnumerable<StaffDto> technicians)
    {
        InitializeComponent();
        _originalTechnicianId = complaint.OriginalTechnicianId;
        SummaryText.Text =
            $"订单 {complaint.OrderNo} · {complaint.ServiceName}\n" +
            $"被投诉技师：{complaint.OriginalTechnicianName}\n" +
            $"投诉内容：{complaint.Comment}";

        // 改派候选不含被投诉本人
        TechBox.ItemsSource = technicians
            .Where(t => t.Id != complaint.OriginalTechnicianId)
            .Select(t => new TechPick(t.Id, t.RealName ?? t.Username))
            .ToList();

        ResolutionBox.SelectedIndex = 0;
    }

    private string SelectedResolution() =>
        (ResolutionBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "Reassigned";

    private void ResolutionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TechBox is null) return;
        var reassign = SelectedResolution() == "Reassigned";
        var vis = reassign ? Visibility.Visible : Visibility.Collapsed;
        TechBox.Visibility = vis;
        TechLabel.Visibility = vis;
    }

    public ResolveComplaintRequest BuildRequest()
    {
        var resolution = SelectedResolution();
        long? techId = resolution == "Reassigned" && TechBox.SelectedItem is TechPick p ? p.Id : null;
        return new ResolveComplaintRequest(
            Resolution: resolution,
            ReassignedToTechnicianId: techId,
            ResolutionNote: string.IsNullOrWhiteSpace(NoteBox.Text) ? null : NoteBox.Text.Trim());
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedResolution() == "Reassigned" && TechBox.SelectedItem is null)
        {
            MessageBox.Show("改派需选择一名新技师");
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
