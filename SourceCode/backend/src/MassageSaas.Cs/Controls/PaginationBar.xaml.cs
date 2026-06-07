using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace MassageSaas.Cs.Controls;

/// <summary>
/// 复刻 BS 端（Element Plus）订单流水分页器：共N条 + 每页条数 + 上一页 + 页码（含省略号跳跃） + 下一页 + 跳页。
/// 绑定 VM 的 Total / Page(双向) / PageSize(双向)，翻页/改条数后执行 PageChangedCommand（通常绑 ReloadCommand）。
/// </summary>
public partial class PaginationBar : UserControl
{
    private const int PagerCount = 7;     // 与 EP 默认 pager-count 一致：最多展示 7 个页码块
    private bool _building;

    public PaginationBar()
    {
        InitializeComponent();
        BuildSizeOptions();
        Rebuild();
    }

    public static readonly DependencyProperty TotalProperty =
        DependencyProperty.Register(nameof(Total), typeof(int), typeof(PaginationBar),
            new FrameworkPropertyMetadata(0, OnPagingChanged));
    public int Total { get => (int)GetValue(TotalProperty); set => SetValue(TotalProperty, value); }

    public static readonly DependencyProperty PageProperty =
        DependencyProperty.Register(nameof(Page), typeof(int), typeof(PaginationBar),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPagingChanged));
    public int Page { get => (int)GetValue(PageProperty); set => SetValue(PageProperty, value); }

    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(PaginationBar),
            new FrameworkPropertyMetadata(20, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPageSizeChanged));
    public int PageSize { get => (int)GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }

    public static readonly DependencyProperty PageChangedCommandProperty =
        DependencyProperty.Register(nameof(PageChangedCommand), typeof(ICommand), typeof(PaginationBar));
    /// <summary>翻页 / 改每页条数后执行（一般绑 VM 的 ReloadCommand）。</summary>
    public ICommand? PageChangedCommand
    {
        get => (ICommand?)GetValue(PageChangedCommandProperty);
        set => SetValue(PageChangedCommandProperty, value);
    }

    public static readonly DependencyProperty PageSizeOptionsProperty =
        DependencyProperty.Register(nameof(PageSizeOptions), typeof(string), typeof(PaginationBar),
            new PropertyMetadata("10,20,50", OnSizeOptionsChanged));
    /// <summary>每页条数候选，逗号分隔，默认 "10,20,50"。</summary>
    public string PageSizeOptions
    {
        get => (string)GetValue(PageSizeOptionsProperty);
        set => SetValue(PageSizeOptionsProperty, value);
    }

    private int TotalPages => Total <= 0 ? 1 : (Total + PageSize - 1) / PageSize;

    private static void OnPagingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PaginationBar)d).Rebuild();

    private static void OnPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bar = (PaginationBar)d;
        bar.SyncSizeSelection();
        bar.Rebuild();
    }

    private static void OnSizeOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PaginationBar)d).BuildSizeOptions();

    private void BuildSizeOptions()
    {
        if (SizeCombo is null) return;
        _building = true;
        SizeCombo.Items.Clear();
        foreach (var n in ParseSizes())
            SizeCombo.Items.Add(new ComboBoxItem { Content = $"{n} 条/页", Tag = n });
        _building = false;
        SyncSizeSelection();
    }

    private IEnumerable<int> ParseSizes()
    {
        var sizes = (PageSizeOptions ?? "10,20,50")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var v) ? v : 0)
            .Where(v => v > 0)
            .ToList();
        return sizes.Count == 0 ? new List<int> { 10, 20, 50 } : sizes;
    }

    private void SyncSizeSelection()
    {
        if (SizeCombo is null) return;
        _building = true;
        ComboBoxItem? match = SizeCombo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(it => it.Tag is int n && n == PageSize);
        SizeCombo.SelectedItem = match;
        _building = false;
    }

    private void SizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_building) return;
        if (SizeCombo.SelectedItem is not ComboBoxItem { Tag: int size } || size == PageSize) return;
        PageSize = size;     // 双向回填 VM
        Page = 1;            // 改每页条数回到第 1 页（与 EP 一致）
        PageChangedCommand?.Execute(null);
    }

    private void JumpBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (int.TryParse(JumpBox.Text, out var target)) GoTo(target);
        JumpBox.Clear();
    }

    private void GoTo(int target)
    {
        int tp = TotalPages;
        target = Math.Clamp(target, 1, tp);
        if (target == Page) return;
        Page = target;       // 双向回填 VM
        PageChangedCommand?.Execute(null);
    }

    private void Rebuild()
    {
        if (PagerPanel is null) return;
        TotalText.Text = $"共 {Total} 条";

        int tp = TotalPages;
        int cur = Math.Clamp(Page, 1, tp);

        PagerPanel.Children.Clear();
        PagerPanel.Children.Add(MakeArrow("‹", cur > 1, () => GoTo(cur - 1), "上一页"));
        foreach (var token in BuildTokens(tp, cur))
        {
            if (token is int p)
                PagerPanel.Children.Add(MakeNumber(p, p == cur));
            else if ((string)token == "prev")
                PagerPanel.Children.Add(MakeEllipsis(() => GoTo(Math.Max(1, cur - (PagerCount - 2)))));
            else
                PagerPanel.Children.Add(MakeEllipsis(() => GoTo(Math.Min(tp, cur + (PagerCount - 2)))));
        }
        PagerPanel.Children.Add(MakeArrow("›", cur < tp, () => GoTo(cur + 1), "下一页"));
    }

    /// <summary>计算页码序列（含 EP 风格的首/末页固定 + 省略号），元素为 int 页码或 "prev"/"next" 省略号。</summary>
    private static IEnumerable<object> BuildTokens(int totalPages, int current)
    {
        var list = new List<object>();
        if (totalPages <= PagerCount)
        {
            for (int i = 1; i <= totalPages; i++) list.Add(i);
            return list;
        }

        int half = (PagerCount - 1) / 2;            // 3
        bool prevMore = current > PagerCount - half; // current > 4
        bool nextMore = current < totalPages - half; // current < totalPages - 3

        list.Add(1);
        if (prevMore) list.Add("prev");

        int start, end;
        if (prevMore && !nextMore) { start = totalPages - (PagerCount - 2); end = totalPages - 1; }
        else if (!prevMore && nextMore) { start = 2; end = PagerCount - 1; }
        else if (prevMore) { int offset = PagerCount / 2 - 1; start = current - offset; end = current + offset; }
        else { start = 2; end = totalPages - 1; }
        for (int i = start; i <= end; i++) list.Add(i);

        if (nextMore) list.Add("next");
        list.Add(totalPages);
        return list;
    }

    private Button MakeNumber(int page, bool active)
    {
        var b = new Button
        {
            Content = page.ToString(),
            Style = (Style)FindResource(active ? "PagerActiveButton" : "PagerButton")
        };
        AutomationProperties.SetName(b, $"第 {page} 页");
        b.Click += (_, _) => GoTo(page);
        return b;
    }

    private Button MakeArrow(string glyph, bool enabled, Action onClick, string name)
    {
        var b = new Button
        {
            Content = glyph,
            FontSize = 16,
            IsEnabled = enabled,
            Style = (Style)FindResource("PagerButton")
        };
        AutomationProperties.SetName(b, name);
        b.Click += (_, _) => onClick();
        return b;
    }

    private Button MakeEllipsis(Action onClick)
    {
        var b = new Button
        {
            Content = "···",
            Style = (Style)FindResource("PagerButton")
        };
        AutomationProperties.SetName(b, "跳跃翻页");
        b.Click += (_, _) => onClick();
        return b;
    }
}
