# FH6 Telemetry HUD

这是一个基于 Avalonia 11 的静态 HUD 前端，按参考图制作了彩色状态灯带、三列读数和半透明背景。当前数值是演示值，后续可以直接替换 `ViewModels/HudViewModel.cs` 中的绑定属性。

## 运行

需要 .NET 10 SDK，并且可以访问 NuGet.org：

```powershell
cd C:\Workspace\FH6-Telemetry\src
dotnet restore
dotnet run
```

窗口默认置顶、无边框、可拖动，按 `Esc` 关闭。窗口样式和尺寸可在 `MainWindow.axaml` 中调整。
