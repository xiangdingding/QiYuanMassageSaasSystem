using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.ViewModels;
using MassageSaas.Shared.Queue;

namespace MassageSaas.Cs.Views;

public partial class QueueView : UserControl
{
    public QueueView()
    {
        InitializeComponent();
    }

    private void OnDuty_Click(object sender, RoutedEventArgs e) => Trigger(sender, "OnDuty");
    private void Resting_Click(object sender, RoutedEventArgs e) => Trigger(sender, "Resting");
    private void OffDuty_Click(object sender, RoutedEventArgs e) => Trigger(sender, "OffDuty");

    private void Trigger(object sender, string state)
    {
        if (sender is not Button b) return;
        if (b.Tag is not TechnicianQueueItemDto item) return;
        if (DataContext is not QueueViewModel vm) return;
        vm.SetStateCommand.Execute(new object[] { item, state });
    }
}
