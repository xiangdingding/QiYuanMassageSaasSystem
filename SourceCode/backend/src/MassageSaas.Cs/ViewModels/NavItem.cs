using CommunityToolkit.Mvvm.ComponentModel;

namespace MassageSaas.Cs.ViewModels;

public partial class NavItem : ObservableObject
{
    public NavItem(string title, string key, Func<object> factory)
    {
        Title = title;
        Key = key;
        Factory = factory;
    }

    public string Title { get; }
    public string Key { get; }
    public Func<object> Factory { get; }

    [ObservableProperty]
    private bool isSelected;
}
