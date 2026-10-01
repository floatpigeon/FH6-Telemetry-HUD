using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace FH6TelemetryHud.Telemetry;

public sealed class ForzaUdpReceiver : IDisposable
{
    public const int DefaultPort = 5301;

    private readonly int _port;
    private CancellationTokenSource? _cancellation;
    private UdpClient? _client;
    private Task? _receiveTask;

    public ForzaUdpReceiver(int port = DefaultPort)
    {
        _port = port is >= 1 and <= 65535 ? port : DefaultPort;
    }

    public int Port => _port;

    public event EventHandler<ForzaTelemetryData>? TelemetryReceived;

    public event EventHandler<Exception>? ReceiveError;

    public void Start()
    {
        if (_receiveTask is not null)
        {
            return;
        }

        _client = new UdpClient(new IPEndPoint(IPAddress.Any, _port));
        _cancellation = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_client, _cancellation.Token);
    }

    public void Dispose()
    {
        _cancellation?.Cancel();
        _client?.Dispose();
        _cancellation?.Dispose();
        _cancellation = null;
        _client = null;
        _receiveTask = null;
    }

    private async Task ReceiveLoopAsync(UdpClient client, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (ForzaPacketParser.TryParse(result.Buffer, out var telemetry))
                {
                    TelemetryReceived?.Invoke(this, telemetry);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Forza UDP receiver stopped: {exception}");
            ReceiveError?.Invoke(this, exception);
        }
    }
}
