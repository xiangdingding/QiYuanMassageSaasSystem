using CommunityToolkit.Mvvm.ComponentModel;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs.Services;

/// <summary>
/// 当前会话内可变的"运行时上下文"：当前激活的门店、订阅状态等。
/// </summary>
public partial class AppContextService : ObservableObject
{
    [ObservableProperty]
    private List<StoreDto> stores = new();

    [ObservableProperty]
    private StoreDto? activeStore;

    public long? ActiveStoreId => ActiveStore?.Id;

    [ObservableProperty]
    private string? subscriptionStatus;

    [ObservableProperty]
    private DateTime? subscriptionExpireAt;

    [ObservableProperty]
    private int? daysToExpire;

    public bool IsSubscriptionActive => string.Equals(SubscriptionStatus, "Active", StringComparison.OrdinalIgnoreCase);
}
