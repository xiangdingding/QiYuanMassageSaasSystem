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

    /// <summary>菜单快捷键提示文案（如 "F2" / "Ctrl+Q"），显示在菜单项右侧。空串则不显示。</summary>
    public string Shortcut { get; set; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;
}
