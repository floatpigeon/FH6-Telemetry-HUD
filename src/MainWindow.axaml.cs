using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FH6TelemetryHud.ViewModels;

namespace FH6TelemetryHud;

public partial class MainWindow : Window
{
    private SettingsWindow? _settingsWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new HudViewModel();
        BuildIndicatorLights();
        ApplyAppearance(CurrentBackgroundOpacity, CurrentBorderOpacity);
    }

    internal double CurrentBackgroundOpacity { get; private set; } = 0.25;

    internal double CurrentBorderOpacity { get; private set; } = 0.25;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Button && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateIndicatorLayout();
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        var settings = new SettingsWindow(this);
        settings.Closed += (_, _) => _settingsWindow = null;
        settings.Show();
        _settingsWindow = settings;
    }

    internal void ApplyAppearance(double backgroundOpacity, double borderOpacity)
    {
        CurrentBackgroundOpacity = Math.Clamp(backgroundOpacity, 0.2, 1.0);
        CurrentBorderOpacity = Math.Clamp(borderOpacity, 0.1, 1.0);

        var surfaceBrush = CreateSurfaceBrush(CurrentBackgroundOpacity);
        var borderBrush = CreateBorderBrush(CurrentBorderOpacity);

        Background = surfaceBrush;
        HudSurface.Background = surfaceBrush;
        IndicatorBorder.Background = surfaceBrush;
        LeftModule.Background = surfaceBrush;
        MiddleModule.Background = surfaceBrush;
        RightModule.Background = surfaceBrush;
        LeftBarTrack.Background = CreateSectionBrush(CurrentBackgroundOpacity);
        RightBarTrack.Background = CreateSectionBrush(CurrentBackgroundOpacity);

        HudSurface.BorderBrush = borderBrush;
        IndicatorBorder.BorderBrush = borderBrush;
        LeftModuleBorder.BorderBrush = borderBrush;
        MiddleModuleBorder.BorderBrush = borderBrush;
        RightModuleBorder.BorderBrush = borderBrush;
        MainDivider.Fill = borderBrush;

        _settingsWindow?.ApplyAppearance(CurrentBackgroundOpacity, CurrentBorderOpacity);
    }

    private void BuildIndicatorLights()
    {
        if (DataContext is not HudViewModel viewModel)
        {
            return;
        }

        IndicatorLightsPanel.Children.Clear();
        foreach (var light in viewModel.StatusLights)
        {
            IndicatorLightsPanel.Children.Add(new Ellipse
            {
                Fill = light.Color,
                Stroke = new SolidColorBrush(Color.Parse("#69737C")),
                StrokeThickness = 0.7,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
        }

        UpdateIndicatorLayout();
    }

    private void UpdateIndicatorLayout()
    {
        if (IndicatorLightsPanel.Children.Count == 0)
        {
            return;
        }

        const double horizontalPadding = 32;
        const double reservedMetricHeight = 40;
        const double gapFactor = 0.5;
        const double lightCount = 16;

        var layoutWidth = HudLayout.Bounds.Width > 0 ? HudLayout.Bounds.Width : Width;
        var layoutHeight = HudLayout.Bounds.Height > 0 ? HudLayout.Bounds.Height : Height;
        if (layoutWidth <= 0 || layoutHeight <= 0)
        {
            return;
        }

        var widthLimit = (layoutWidth - horizontalPadding) /
                         (lightCount + (lightCount - 1) * gapFactor);
        var heightLimit = (layoutHeight - 1 - reservedMetricHeight) / 3;
        var diameter = Math.Clamp(Math.Min(widthLimit, heightLimit), 18, 72);
        var radius = diameter / 2;

        HudLayout.RowDefinitions[0].Height = new GridLength(diameter * 3);
        for (var index = 0; index < IndicatorLightsPanel.Children.Count; index++)
        {
            if (IndicatorLightsPanel.Children[index] is not Ellipse ellipse)
            {
                continue;
            }

            ellipse.Width = diameter;
            ellipse.Height = diameter;
            ellipse.Margin = new Thickness(0, 0, index == IndicatorLightsPanel.Children.Count - 1 ? 0 : radius, 0);
        }
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
