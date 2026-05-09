using CommunityToolkit.Mvvm.ComponentModel;
using MassageSaas.Shared.Staff;

namespace MassageSaas.Cs.ViewModels.Pos;

public partial class CartItemViewModel : ObservableObject
{
    public long ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private int quantity = 1;

    [ObservableProperty]
    private StaffDto? technician;

    public decimal LineTotal => UnitPrice * Quantity;

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));
}
