using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using System.Globalization;

namespace FH6TelemetryHud;

public partial class SettingsWindow : Window
{
    private readonly MainWindow? _hud;

    public SettingsWindow()
    {
        InitializeComponent();
        ApplyAppearance(0.25, 0.25);
        UpdateValueLabels();
    }

    public SettingsWindow(MainWindow hud) : this()
    {
        _hud = hud;

        BackgroundSlider.Value = hud.CurrentBackgroundOpacity;
        BorderSlider.Value = hud.CurrentBorderOpacity;
        WidthSlider.Value = hud.Width;
        HeightSlider.Value = hud.Height;
        PortTextBox.Text = hud.CurrentTelemetryPort.ToString(CultureInfo.InvariantCulture);
        PortStatusText.Text = $"当前监听：UDP {hud.CurrentTelemetryPort}";

        var thresholds = hud.CurrentShiftLightThresholds;
        GreenStartThresholdSlider.Value = thresholds.GreenStart;
        GreenThresholdSlider.Value = thresholds.GreenEnd;
        YellowThresholdSlider.Value = thresholds.YellowEnd;
        OrangeThresholdSlider.Value = thresholds.OrangeEnd;
        ShiftThresholdSlider.Value = thresholds.ShiftPoint;
        ThresholdStatusText.Text = "当前阈值已应用。";

        BackgroundSlider.ValueChanged += OnBackgroundChanged;
        BorderSlider.ValueChanged += OnBorderChanged;
        WidthSlider.ValueChanged += OnWidthChanged;
        HeightSlider.ValueChanged += OnHeightChanged;
        GreenStartThresholdSlider.ValueChanged += OnThresholdSliderChanged;
        GreenThresholdSlider.ValueChanged += OnThresholdSliderChanged;
        YellowThresholdSlider.ValueChanged += OnThresholdSliderChanged;
        OrangeThresholdSlider.ValueChanged += OnThresholdSliderChanged;
        ShiftThresholdSlider.ValueChanged += OnThresholdSliderChanged;

        ApplyAppearance(hud.CurrentBackgroundOpacity, hud.CurrentBorderOpacity);
        UpdateValueLabels();
    }

    internal void ApplyAppearance(double backgroundOpacity, double borderOpacity)
    {
        var surfaceBrush = CreateSurfaceBrush(backgroundOpacity);
        var sectionBrush = CreateSectionBrush(backgroundOpacity);
        var borderBrush = CreateBorderBrush(borderOpacity);

        Background = surfaceBrush;
        SettingsSurface.Background = surfaceBrush;
        BackgroundSection.Background = sectionBrush;
        BorderSection.Background = sectionBrush;
        WindowSection.Background = sectionBrush;
        ShiftLightSection.Background = sectionBrush;
        NetworkSection.Background = sectionBrush;
        SettingsSurface.BorderBrush = borderBrush;
        BackgroundSection.BorderBrush = borderBrush;
        BorderSection.BorderBrush = borderBrush;
        WindowSection.BorderBrush = borderBrush;
    }

    private void OnBackgroundChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_hud is { } hud)
        {
            hud.ApplyAppearance(e.NewValue, BorderSlider.Value);
        }
        else
        {
            ApplyAppearance(e.NewValue, BorderSlider.Value);
        }

        UpdateValueLabels();
    }

    private void OnBorderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_hud is { } hud)
        {
            hud.ApplyAppearance(BackgroundSlider.Value, e.NewValue);
        }
        else
        {
            ApplyAppearance(BackgroundSlider.Value, e.NewValue);
        }

        UpdateValueLabels();
    }

    private void OnWidthChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_hud is { } hud)
        {
            hud.Width = e.NewValue;
        }

        UpdateValueLabels();
    }

    private void OnHeightChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_hud is { } hud)
        {
            hud.Height = e.NewValue;
        }

        UpdateValueLabels();
    }

    private void OnThresholdSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        UpdateValueLabels();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnApplyPortClick(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
        {
            PortStatusText.Text = "端口格式无效。";
            return;
        }

        if (_hud is null)
        {
            PortStatusText.Text = "HUD 尚未连接。";
            return;
        }

        if (_hud.TryChangeTelemetryPort(port, out var error))
        {
            PortTextBox.Text = port.ToString(CultureInfo.InvariantCulture);
            PortStatusText.Text = $"当前监听：UDP {port}";
        }
        else
        {
            PortStatusText.Text = error;
        }
    }

    private void OnApplyThresholdsClick(object? sender, RoutedEventArgs e)
    {
        if (_hud is null)
        {
            ThresholdStatusText.Text = "HUD 尚未连接。";
            return;
        }

        if (_hud.TryApplyShiftLightThresholds(
                GreenStartThresholdSlider.Value,
                GreenThresholdSlider.Value,
                YellowThresholdSlider.Value,
                OrangeThresholdSlider.Value,
                ShiftThresholdSlider.Value,
                out var error))
        {
            ThresholdStatusText.Text = "换挡提示灯阈值已应用。";
        }
        else
        {
            ThresholdStatusText.Text = error;
        }

        UpdateValueLabels();
    }

    private void UpdateValueLabels()
    {
        BackgroundValueText.Text = $"{BackgroundSlider.Value:P0}";
        BorderValueText.Text = $"{BorderSlider.Value:P0}";
        var hudWidth = _hud?.Width ?? Width;
        var hudHeight = _hud?.Height ?? Height;
        WidthValueText.Text = $"{hudWidth:0} px";
        HeightValueText.Text = $"{hudHeight:0} px";
        GreenStartThresholdText.Text = $"{GreenStartThresholdSlider.Value:P0}";
        GreenThresholdText.Text = $"{GreenThresholdSlider.Value:P0}";
        YellowThresholdText.Text = $"{YellowThresholdSlider.Value:P0}";
        OrangeThresholdText.Text = $"{OrangeThresholdSlider.Value:P0}";
        ShiftThresholdText.Text = $"{ShiftThresholdSlider.Value:P0}";
    }

    private static SolidColorBrush CreateSurfaceBrush(double opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(opacity * 255), 0, 255);
        return new SolidColorBrush(Color.FromArgb(alpha, 29, 32, 36));
    }

    private static SolidColorBrush CreateBorderBrush(double opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(opacity * 255), 0, 255);
        return new SolidColorBrush(Color.FromArgb(alpha, 181, 190, 198));
    }

    private static SolidColorBrush CreateSectionBrush(double opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(opacity * 255), 0, 255);
        return new SolidColorBrush(Color.FromArgb(alpha, 42, 46, 51));
    }
}
