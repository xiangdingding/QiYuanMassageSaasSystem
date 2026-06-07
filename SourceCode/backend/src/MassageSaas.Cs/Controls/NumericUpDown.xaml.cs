using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MassageSaas.Cs.Controls;

/// <summary>
/// 数字步进输入框（对齐 BS 端 el-input-number）：左 − / 中输入 / 右 +，带最小/最大/步长/小数位。
/// </summary>
public partial class NumericUpDown : UserControl
{
    public NumericUpDown()
    {
        InitializeComponent();
        UpdateText();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(NumericUpDown),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(0d, OnLimitChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MaxValue, OnLimitChanged));

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step), typeof(double), typeof(NumericUpDown), new PropertyMetadata(1d));

    public static readonly DependencyProperty DecimalsProperty = DependencyProperty.Register(
        nameof(Decimals), typeof(int), typeof(NumericUpDown), new PropertyMetadata(0, OnDecimalsChanged));

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public int Decimals { get => (int)GetValue(DecimalsProperty); set => SetValue(DecimalsProperty, value); }

    /// <summary>值变化（步进/输入/外部赋值经钳制后）通知，供宿主重算联动金额。</summary>
    public event EventHandler? ValueChanged;

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var c = (NumericUpDown)d;
        var v = (double)baseValue;
        if (v < c.Minimum) v = c.Minimum;
        if (v > c.Maximum) v = c.Maximum;
        return v;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (NumericUpDown)d;
        c.UpdateText();
        c.ValueChanged?.Invoke(c, EventArgs.Empty);
    }

    private static void OnLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => d.CoerceValue(ValueProperty);

    private static void OnDecimalsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((NumericUpDown)d).UpdateText();

    private void UpdateText()
    {
        if (ValueBox is null) return;
        var text = Value.ToString("F" + Decimals, CultureInfo.InvariantCulture);
        if (ValueBox.Text != text) ValueBox.Text = text;
    }

    private void Minus_Click(object sender, RoutedEventArgs e) => Value -= Step;
    private void Plus_Click(object sender, RoutedEventArgs e) => Value += Step;

    private void ValueBox_LostFocus(object sender, RoutedEventArgs e) => Commit();

    private void ValueBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { Commit(); e.Handled = true; }
        else if (e.Key == Key.Up) { Value += Step; e.Handled = true; }
        else if (e.Key == Key.Down) { Value -= Step; e.Handled = true; }
    }

    private void Commit()
    {
        if (double.TryParse(ValueBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            Value = v;     // 经 CoerceValue 钳制，再由 OnValueChanged 回写规范文本
        UpdateText();      // 解析失败时还原成当前值
    }
}
