using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using AvaloniaColor = Avalonia.Media.Color;
using FH6TelemetryHud.Telemetry;

namespace FH6TelemetryHud.ViewModels;

public sealed class HudViewModel : INotifyPropertyChanged
{
    private const int ShiftLightCount = 16;
    private readonly ShiftLightThresholds _shiftLightThresholds = new();
    private string _leftPercentage = "30%";
    private string _leftValue = "6567";
    private string _gear = "6";
    private string _rightPercentage = "100%";
    private string _speed = "210";
    private double _rpmRatio;
    private bool _hasTelemetry;
    private bool _flashPhase;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string LeftPercentage
    {
        get => _leftPercentage;
        private set => SetField(ref _leftPercentage, value);
    }

    public string LeftValue
    {
        get => _leftValue;
        private set => SetField(ref _leftValue, value);
    }

    public string Gear
    {
        get => _gear;
        private set => SetField(ref _gear, value);
    }

    public string RightPercentage
    {
        get => _rightPercentage;
        private set => SetField(ref _rightPercentage, value);
    }

    public string Speed
    {
        get => _speed;
        private set => SetField(ref _speed, value);
    }

    public string SpeedUnit { get; } = "km/h";

    public ShiftLightThresholds ShiftLightThresholds => _shiftLightThresholds;

    public void ApplyTelemetry(ForzaTelemetryData telemetry)
    {
        _hasTelemetry = true;
        _rpmRatio = telemetry.EngineMaxRpm > 0
            ? Math.Clamp(telemetry.CurrentEngineRpm / telemetry.EngineMaxRpm, 0, 1.5f)
            : 0;
        LeftPercentage = FormatPercent(telemetry.Brake);
        RightPercentage = FormatPercent(telemetry.Accel);
        LeftValue = FormatNumber(telemetry.CurrentEngineRpm);
        Gear = telemetry.Gear.ToString(CultureInfo.InvariantCulture);
        Speed = FormatNumber(Math.Max(0, telemetry.Speed) * 3.6f);
        UpdateShiftLights();
    }

    public void SetFlashPhase(bool isOn)
    {
        _flashPhase = isOn;
        if (_hasTelemetry)
        {
            UpdateShiftLights();
        }
    }

    public bool TrySetShiftLightThresholds(
        double greenStart,
        double greenEnd,
        double yellowEnd,
        double orangeEnd,
        double shiftPoint,
        out string error)
    {
        if (greenStart < 0 || greenStart >= greenEnd || greenEnd >= yellowEnd || yellowEnd >= orangeEnd || orangeEnd >= shiftPoint || shiftPoint > 1)
        {
            error = "阈值必须递增，并且都在 0%-100% 之间。";
            return false;
        }

        _shiftLightThresholds.Set(greenStart, greenEnd, yellowEnd, orangeEnd, shiftPoint);
        UpdateShiftLights();
        error = string.Empty;
        return true;
    }

    private void UpdateShiftLights()
    {
        if (!_hasTelemetry)
        {
            return;
        }

        for (var index = 0; index < StatusLights.Count; index++)
        {
            var lightThreshold = GetLightActivationThreshold(index);
            var color = GetColorForIndex(index);
            var isLit = _rpmRatio >= lightThreshold;
            var atShiftPoint = _rpmRatio >= _shiftLightThresholds.ShiftPoint;
            var alpha = atShiftPoint
                ? (_flashPhase ? (byte)255 : (byte)45)
                : (isLit ? (byte)255 : (byte)45);
            StatusLights[index].SetColor(color, alpha);
        }
    }

    private double GetLightActivationThreshold(int index)
    {
        if (index < 8)
        {
            return _shiftLightThresholds.GreenStart +
                   (_shiftLightThresholds.GreenEnd - _shiftLightThresholds.GreenStart) * index / 7d;
        }

        if (index < 12)
        {
            return _shiftLightThresholds.GreenEnd +
                   (_shiftLightThresholds.YellowEnd - _shiftLightThresholds.GreenEnd) * (index - 8) / 3d;
        }

        if (index < 14)
        {
            return _shiftLightThresholds.YellowEnd +
                   (_shiftLightThresholds.OrangeEnd - _shiftLightThresholds.YellowEnd) * (index - 12) / 1d;
        }

        return _shiftLightThresholds.OrangeEnd +
               (_shiftLightThresholds.ShiftPoint - _shiftLightThresholds.OrangeEnd) * (index - 14) / 1d;
    }

    private static AvaloniaColor GetColorForIndex(int index)
    {
        return index < 8
            ? ShiftLightColor.Green
            : index < 12
                ? ShiftLightColor.Yellow
                : index < 14
                    ? ShiftLightColor.Orange
                    : ShiftLightColor.Red;
    }

    public ObservableCollection<StatusLight> StatusLights { get; } =
    [
        new("#20DD80"), new("#20DD80"), new("#20DD80"),
        new("#FFD33D"), new("#FFD33D"),
        new("#FF892B"), new("#FF892B"),
        new("#FF3C37"), new("#FF3C37"),
        new("#FF892B"), new("#FF892B"),
        new("#FFD33D"), new("#FFD33D"),
        new("#20DD80"), new("#20DD80"), new("#20DD80")
    ];

    private static string FormatPercent(byte value) =>
        $"{Math.Clamp(value / 255d * 100d, 0, 100):0}%";

    private static string FormatNumber(float value) =>
        float.IsFinite(value)
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : "0";

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class StatusLight
{
    public StatusLight(string color) => Color = new SolidColorBrush(AvaloniaColor.Parse(color));

    public IBrush Color { get; private set; }

    public void SetColor(AvaloniaColor color, byte alpha)
    {
        Color = new SolidColorBrush(AvaloniaColor.FromArgb(alpha, color.R, color.G, color.B));
    }
}

public sealed class ShiftLightThresholds
{
    public double GreenStart { get; private set; } = 0.00;
    public double GreenEnd { get; private set; } = 0.50;
    public double YellowEnd { get; private set; } = 0.75;
    public double OrangeEnd { get; private set; } = 0.90;
    public double ShiftPoint { get; private set; } = 1.00;

    public void Set(double greenStart, double greenEnd, double yellowEnd, double orangeEnd, double shiftPoint)
    {
        GreenStart = greenStart;
        GreenEnd = greenEnd;
        YellowEnd = yellowEnd;
        OrangeEnd = orangeEnd;
        ShiftPoint = shiftPoint;
    }
}

internal static class ShiftLightColor
{
    public static readonly AvaloniaColor Green = AvaloniaColor.Parse("#20DD80");
    public static readonly AvaloniaColor Yellow = AvaloniaColor.Parse("#FFD33D");
    public static readonly AvaloniaColor Orange = AvaloniaColor.Parse("#FF892B");
    public static readonly AvaloniaColor Red = AvaloniaColor.Parse("#FF3C37");
}
