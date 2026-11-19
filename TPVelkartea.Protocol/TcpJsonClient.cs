using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TPVelkartea.Protocol;

public sealed class TcpJsonClient : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public TcpJsonClient(string host = "127.0.0.1", int port = ProtocolConstants.DefaultPort)
    {
        _host = host;
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_client?.Connected == true)
        {
            return;
        }

        _client = new TcpClient();
        await _client.ConnectAsync(_host, _port, cancellationToken);
        NetworkStream stream = _client.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        _writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
    }

    public async Task<TcpResponse> SendAsync(TcpRequest request, CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await ConnectAsync(cancellationToken);
            string json = JsonSerializer.Serialize(request);
            await _writer!.WriteLineAsync(json);
            string? responseJson = await _reader!.ReadLineAsync(cancellationToken);
            if (responseJson is null)
            {
                throw new IOException("El servidor cerró la conexión.");
            }

            return JsonSerializer.Deserialize<TcpResponse>(responseJson)
                ?? throw new InvalidDataException("Respuesta TCP no válida.");
        }
        catch
        {
            await DisconnectAsync();
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task DisconnectAsync()
    {
        if (_writer is not null)
        {
            await _writer.DisposeAsync();
            _writer = null;
        }

        _reader?.Dispose();
        _reader = null;
        _client?.Dispose();
        _client = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _sendLock.Dispose();
    }
}
