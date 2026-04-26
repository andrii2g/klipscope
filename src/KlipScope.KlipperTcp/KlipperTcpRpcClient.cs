using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using KlipScope.Core.Models;

namespace KlipScope.KlipperTcp;

public sealed class KlipperTcpRpcClient(ResolvedConnectionOptions options)
{
    private long _nextId;
    private const byte Etx = 0x03;
    private const int MaxMessageBytes = 4 * 1024 * 1024;

    public async Task<JsonNode?> CallAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        using var tcpClient = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(options.Timeout);

        await tcpClient.ConnectAsync(options.Host, options.KlipperPort ?? throw new InvalidOperationException("Missing --klipper-port."), timeoutCts.Token);
        await using var stream = tcpClient.GetStream();

        var request = new JsonObject
        {
            ["id"] = Interlocked.Increment(ref _nextId),
            ["method"] = method
        };

        if (parameters is not null)
        {
            request["params"] = JsonSerializer.SerializeToNode(parameters);
        }

        var payload = Encoding.UTF8.GetBytes(request.ToJsonString());
        await stream.WriteAsync(payload, timeoutCts.Token);
        await stream.WriteAsync(new[] { Etx }, timeoutCts.Token);
        await stream.FlushAsync(timeoutCts.Token);

        return JsonNode.Parse(await ReadMessageAsync(stream, timeoutCts.Token));
    }

    private static async Task<string> ReadMessageAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];

        while (true)
        {
            var count = await stream.ReadAsync(chunk, cancellationToken);
            if (count == 0)
            {
                break;
            }

            for (var index = 0; index < count; index++)
            {
                if (chunk[index] == Etx)
                {
                    return Encoding.UTF8.GetString(buffer.ToArray());
                }

                buffer.WriteByte(chunk[index]);
                if (buffer.Length > MaxMessageBytes)
                {
                    throw new InvalidOperationException("Received oversized Klipper TCP message.");
                }
            }
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
