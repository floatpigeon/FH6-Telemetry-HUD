# FH6 Telemetry HUD

这是一个基于 Avalonia 11 的 FH6 Telemetry HUD。界面按参考图制作了彩色状态灯带、三列读数和半透明背景，并已接入 Forza Horizon 6 Data Out UDP 数据接收。

## 运行

需要 .NET 10 SDK，并且可以访问 NuGet.org：

```powershell
cd C:\Workspace\FH6-Telemetry\src
dotnet restore
dotnet run
```

窗口默认置顶、无边框、可拖动，按 `Esc` 关闭。窗口样式和尺寸可在 `MainWindow.axaml` 中调整。

## Forza Data Out 配置

程序默认监听 `UDP 5301`。在游戏的 `SETTINGS > HUD AND GAMEPLAY` 中设置：

- `Data Out`: On
- `Data Out IP Address`: `127.0.0.1`
- `Data Out IP Port`: `5301`

程序解析固定 324 字节数据包，并将 `CurrentEngineRpm`、`Gear`、`Speed`、`Brake` 和 `Accel` 映射到 HUD。速度从 m/s 转换为 km/h，输入值从 `0-255` 转换为百分比。

换挡灯默认阈值为绿灯起始 0%、绿色结束 50%、黄色结束 75%、橙色结束 90%、红灯闪烁换挡点 100%。这些阈值可以在设置窗口中调整；绿灯起始阈值决定第一颗绿灯开始亮起的转速。

如需更换监听端口，可设置环境变量：

```powershell
$env:FH6_TELEMETRY_PORT = "5302"
dotnet run
```

也可以在 HUD 左下角设置窗口中直接修改监听端口并点击“应用端口”，程序会先验证新端口可用后再切换监听。
