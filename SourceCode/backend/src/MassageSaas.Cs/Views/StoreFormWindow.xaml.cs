using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MassageSaas.Cs.Services;
using MassageSaas.Shared.Stores;

namespace MassageSaas.Cs.Views;

/// <summary>
/// 新建分店 / 编辑门店。字段与逻辑对齐 BS StoresView 的表单弹窗：
/// 「所属总店」用下拉选总店、仅新建时显示；「状态」开关仅编辑时显示；窗口自己调接口，成功才关、出错只提示保持打开。
/// </summary>
public partial class StoreFormWindow : Window
{
    private readonly IApiClient _api;
    private readonly StoreDto? _editing;

    private record StoreOpt(long? Id, string Name);

    public StoreFormWindow(IApiClient api, StoreDto? editing, IReadOnlyList<StoreDto> headquarters)
    {
        InitializeComponent();
        _api = api;
        _editing = editing;
        ParentBox.ItemsSource = headquarters.Select(s => new StoreOpt(s.Id, s.Name)).ToList();

        if (editing is not null)
        {
            Title = $"编辑门店 - {editing.Name}";
            NameBox.Text = editing.Name;
            AddressBox.Text = editing.Address ?? string.Empty;
            PhoneBox.Text = editing.Phone ?? string.Empty;
            ActiveBox.IsChecked = editing.IsActive;
            CutoffBox.Text = FormatCutoff(editing.DayCloseCutoffMinutes);
            ParentPanel.Visibility = Visibility.Collapsed;   // 编辑不改上级
            StatusPanel.Visibility = Visibility.Visible;     // 仅编辑显示状态
        }
        else
        {
            Title = "新建分店";
            ParentBox.SelectedValue = headquarters.FirstOrDefault()?.Id;
            ParentPanel.Visibility = Visibility.Visible;
            StatusPanel.Visibility = Visibility.Collapsed;   // 新建不显示状态
        }
    }

    private int Cutoff => ParseCutoff(CutoffBox.Text);

    public CreateStoreRequest BuildCreateRequest() => new(
        Name: NameBox.Text.Trim(),
        Address: string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        ParentStoreId: ParentBox.SelectedValue as long?,
        DayCloseCutoffMinutes: Cutoff);

    public UpdateStoreRequest BuildUpdateRequest() => new(
        Name: NameBox.Text.Trim(),
        Address: string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
        Phone: string.IsNullOrWhiteSpace(PhoneBox.Text) ? null : PhoneBox.Text.Trim(),
        IsActive: ActiveBox.IsChecked == true,
        DayCloseCutoffMinutes: Cutoff);

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) { MessageBox.Show("请输入名称"); return; }
        var cutoff = Cutoff;
        if (cutoff < 0 || cutoff > 1439)
        {
            MessageBox.Show("营业日切日时间格式应为 HH:mm，且在 00:00 ~ 23:59 之间");
            return;
        }

        // 窗口自己调接口：成功才关闭；异常只提示、保持窗口打开，输入不丢失
        var btn = sender as Button;
        if (btn is not null) btn.IsEnabled = false;
        try
        {
            if (_editing is null)
                await _api.CreateStoreAsync(BuildCreateRequest());
            else
                await _api.UpdateStoreAsync(_editing.Id, BuildUpdateRequest());
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorReporter.Show(ex);
        }
        finally
        {
            if (btn is not null) btn.IsEnabled = true;
        }
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
