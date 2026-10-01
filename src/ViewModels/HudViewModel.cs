using System.Collections.ObjectModel;
using Avalonia.Media;
using AvaloniaColor = Avalonia.Media.Color;

namespace FH6TelemetryHud.ViewModels;

public sealed class HudViewModel
{
    // Static demo values for the first UI pass; replace these with telemetry bindings later.
    public string LeftPercentage { get; } = "30%";
    public string LeftValue { get; } = "6567";
    public string Gear { get; } = "6";
    public string RightPercentage { get; } = "100%";
    public string Speed { get; } = "210";
    public string SpeedUnit { get; } = "km/h";

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
}

public sealed class StatusLight
{
    public StatusLight(string color) => Color = new SolidColorBrush(AvaloniaColor.Parse(color));

    public IBrush Color { get; }
}
