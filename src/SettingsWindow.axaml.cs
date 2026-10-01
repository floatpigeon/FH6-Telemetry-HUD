using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;

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

        BackgroundSlider.ValueChanged += OnBackgroundChanged;
        BorderSlider.ValueChanged += OnBorderChanged;
        WidthSlider.ValueChanged += OnWidthChanged;
        HeightSlider.ValueChanged += OnHeightChanged;

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

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void UpdateValueLabels()
    {
        BackgroundValueText.Text = $"{BackgroundSlider.Value:P0}";
        BorderValueText.Text = $"{BorderSlider.Value:P0}";
        var hudWidth = _hud?.Width ?? Width;
        var hudHeight = _hud?.Height ?? Height;
        WidthValueText.Text = $"{hudWidth:0} px";
        HeightValueText.Text = $"{hudHeight:0} px";
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
