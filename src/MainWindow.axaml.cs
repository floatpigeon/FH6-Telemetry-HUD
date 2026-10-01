using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FH6TelemetryHud.Telemetry;
using FH6TelemetryHud.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

namespace FH6TelemetryHud;

public partial class MainWindow : Window
{
    private SettingsWindow? _settingsWindow;
    private ForzaUdpReceiver? _telemetryReceiver;
    private readonly DispatcherTimer _shiftLightTimer;
    private bool _shiftLightFlashPhase;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new HudViewModel();
        BuildIndicatorLights();
        ApplyAppearance(CurrentBackgroundOpacity, CurrentBorderOpacity);

        // 100 ms per phase gives a 200 ms full on/off cycle (5 Hz).
        _shiftLightTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _shiftLightTimer.Tick += OnShiftLightTimerTick;
        _shiftLightTimer.Start();

        CurrentTelemetryPort = ReadTelemetryPort();
        Closed += OnMainWindowClosed;
        TryStartTelemetryReceiver(CurrentTelemetryPort);
    }

    internal double CurrentBackgroundOpacity { get; private set; } = 0.25;

    internal double CurrentBorderOpacity { get; private set; } = 0.25;

    internal int CurrentTelemetryPort { get; private set; } = ForzaUdpReceiver.DefaultPort;

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

    private void OnTelemetryReceived(object? sender, ForzaTelemetryData telemetry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is HudViewModel viewModel)
            {
                viewModel.ApplyTelemetry(telemetry);
            }

            UpdateProgressBars(telemetry.Brake / 255d, telemetry.Accel / 255d);
            RefreshIndicatorLights();
        });
    }

    private void OnShiftLightTimerTick(object? sender, EventArgs e)
    {
        _shiftLightFlashPhase = !_shiftLightFlashPhase;
        if (DataContext is HudViewModel viewModel)
        {
            viewModel.SetFlashPhase(_shiftLightFlashPhase);
            RefreshIndicatorLights();
        }
    }

    private void OnTelemetryError(object? sender, Exception exception)
    {
        Debug.WriteLine($"Forza telemetry error: {exception.Message}");
    }

    private void OnMainWindowClosed(object? sender, EventArgs e)
    {
        _shiftLightTimer.Stop();
        _telemetryReceiver?.Dispose();
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

    private void RefreshIndicatorLights()
    {
        if (DataContext is not HudViewModel viewModel)
        {
            return;
        }

        for (var index = 0; index < Math.Min(IndicatorLightsPanel.Children.Count, viewModel.StatusLights.Count); index++)
        {
            if (IndicatorLightsPanel.Children[index] is Ellipse ellipse)
            {
                ellipse.Fill = viewModel.StatusLights[index].Color;
            }
        }
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

    private void UpdateProgressBars(double brake, double accel)
    {
        SetCenterOutProgress(LeftBrakeLeftHalf, brake, fillFromRight: true);
        SetCenterOutProgress(LeftBrakeRightHalf, brake, fillFromRight: false);
        SetCenterOutProgress(RightThrottleLeftHalf, accel, fillFromRight: true);
        SetCenterOutProgress(RightThrottleRightHalf, accel, fillFromRight: false);
    }

    private static void SetCenterOutProgress(Grid half, double value, bool fillFromRight)
    {
        var fill = Math.Clamp(value, 0, 1);
        var empty = 1 - fill;
        if (fillFromRight)
        {
            half.ColumnDefinitions[0].Width = new GridLength(empty, GridUnitType.Star);
            half.ColumnDefinitions[1].Width = new GridLength(fill, GridUnitType.Star);
        }
        else
        {
            half.ColumnDefinitions[0].Width = new GridLength(fill, GridUnitType.Star);
            half.ColumnDefinitions[1].Width = new GridLength(empty, GridUnitType.Star);
        }
    }

    internal bool TryChangeTelemetryPort(int port, out string error)
    {
        error = string.Empty;
        if (port is < 1 or > 65535)
        {
            error = "端口必须在 1-65535 之间。";
            return false;
        }

        if (port == CurrentTelemetryPort && _telemetryReceiver is not null)
        {
            return true;
        }

        var nextReceiver = new ForzaUdpReceiver(port);
        nextReceiver.TelemetryReceived += OnTelemetryReceived;
        nextReceiver.ReceiveError += OnTelemetryError;
        try
        {
            nextReceiver.Start();
        }
        catch (SocketException exception)
        {
            nextReceiver.Dispose();
            error = $"无法监听 UDP {port}：{exception.Message}";
            return false;
        }

        _telemetryReceiver?.Dispose();
        _telemetryReceiver = nextReceiver;
        CurrentTelemetryPort = port;
        Debug.WriteLine($"Forza telemetry listening on UDP {port}");
        return true;
    }

    internal ShiftLightThresholds CurrentShiftLightThresholds =>
        ((HudViewModel)DataContext!).ShiftLightThresholds;

    internal bool TryApplyShiftLightThresholds(
        double greenStart,
        double greenEnd,
        double yellowEnd,
        double orangeEnd,
        double shiftPoint,
        out string error)
    {
        var applied = ((HudViewModel)DataContext!).TrySetShiftLightThresholds(
            greenStart, greenEnd, yellowEnd, orangeEnd, shiftPoint, out error);
        if (applied)
        {
            RefreshIndicatorLights();
        }

        return applied;
    }

    private void TryStartTelemetryReceiver(int port)
    {
        if (!TryChangeTelemetryPort(port, out var error))
        {
            Debug.WriteLine($"Unable to bind Forza telemetry UDP port: {error}");
        }
    }

    private static int ReadTelemetryPort()
    {
        var configuredPort = Environment.GetEnvironmentVariable("FH6_TELEMETRY_PORT");
        return int.TryParse(configuredPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) &&
               port is >= 1 and <= 65535
            ? port
            : ForzaUdpReceiver.DefaultPort;
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
