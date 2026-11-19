using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TPVelkartea.Datuak;
using TPVelkartea.Ereduak;
using TPVelkartea.Protocol;

namespace TPVelkartea.Server;

public sealed class TcpServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly SemaphoreSlim _clients = new(ProtocolConstants.MaxClients, ProtocolConstants.MaxClients);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly DatuBasea _database;
    private readonly CancellationTokenSource _stop = new();

    public TcpServer(int port)
    {
        string databasePath = Path.Combine(AppContext.BaseDirectory, "Datuak", "elkartea.db");
        _database = new DatuBasea(databasePath);
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task RunAsync()
    {
        _listener.Start();
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_stop.Token);
                if (!await _clients.WaitAsync(0))
                {
                    client.Close();
                    continue;
                }

                _ = Task.Run(() => ServeClientAsync(client));
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
    }

    private async Task ServeClientAsync(TcpClient client)
    {
        try
        {
            await using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
            await using StreamWriter writer = new(stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };

            while (!_stop.IsCancellationRequested && client.Connected)
            {
                string? line = await reader.ReadLineAsync(_stop.Token);
                if (line is null)
                {
                    break;
                }

                TcpResponse response;
                try
                {
                    TcpRequest request = JsonSerializer.Deserialize<TcpRequest>(line)
                        ?? throw new InvalidDataException("Petición vacía.");
                    response = await DispatchAsync(request);
                }
                catch (Exception exception) when (exception is InvalidOperationException or JsonException or InvalidDataException)
                {
                    response = TcpResponse.Fail(exception.Message);
                }

                await writer.WriteLineAsync(JsonSerializer.Serialize(response));
            }
        }
        catch (IOException)
        {
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
        finally
        {
            client.Dispose();
            _clients.Release();
        }
    }

    private async Task<TcpResponse> DispatchAsync(TcpRequest request)
    {
        switch (request.Operation)
        {
            case "login":
                return TcpResponse.Ok(_database.SaioaHasi(request.Username ?? "", request.Password ?? ""));
            case "products":
                return TcpResponse.Ok(_database.LortuProduktuak());
            case "reservations":
                DateTime date = request.Payload.GetProperty("date").GetDateTime();
                return TcpResponse.Ok(_database.LortuErreserbak(date, request.Payload.GetProperty("shift").GetString() ?? Erreserba.Bazkaria));
            case "sales":
                RequireAdmin(request);
                return TcpResponse.Ok(_database.LortuSalmentak());
            case "users":
                RequireAdmin(request);
                return TcpResponse.Ok(_database.LortuErabiltzaileak());
            case "save-product":
                RequireAdmin(request);
                await _writeLock.WaitAsync();
                try { _database.GordeProduktua(request.Payload.Deserialize<Produktua>() ?? throw new InvalidDataException("Producto no válido.")); }
                finally { _writeLock.Release(); }
                return TcpResponse.Ok(true);
            case "update-reservation":
                RequireAuthenticated(request);
                await _writeLock.WaitAsync();
                try
                {
                    _database.AldatuErreserba(request.Payload.GetProperty("id").GetInt32(), request.Payload.GetProperty("table").GetInt32(), request.Payload.GetProperty("date").GetDateTime(), request.Payload.GetProperty("shift").GetString() ?? Erreserba.Bazkaria, request.Username!, request.Role == Erabiltzailea.Administratzailea);
                }
                finally { _writeLock.Release(); }
                return TcpResponse.Ok(true);
            case "delete-product":
                RequireAdmin(request);
                await _writeLock.WaitAsync();
                try { _database.EzabatuProduktua(request.Payload.GetProperty("id").GetInt32()); }
                finally { _writeLock.Release(); }
                return TcpResponse.Ok(true);
            case "save-user":
                RequireAdmin(request);
                Erabiltzailea user = request.Payload.GetProperty("user").Deserialize<Erabiltzailea>() ?? throw new InvalidDataException("Usuario no válido.");
                string password = request.Payload.GetProperty("password").GetString() ?? "";
                await _writeLock.WaitAsync();
                try { _database.GordeErabiltzailea(user, password); }
                finally { _writeLock.Release(); }
                return TcpResponse.Ok(true);
            case "delete-user":
                RequireAdmin(request);
                await _writeLock.WaitAsync();
                try { _database.EzabatuErabiltzailea(request.Payload.GetProperty("id").GetInt32()); }
                finally { _writeLock.Release(); }
                return TcpResponse.Ok(true);
            case "save-reservation":
                await _writeLock.WaitAsync();
                try
                {
                    RequireAuthenticated(request);
                    _database.GordeErreserba(request.Payload.GetProperty("table").GetInt32(), request.Payload.GetProperty("date").GetDateTime(), request.Payload.GetProperty("shift").GetString() ?? Erreserba.Bazkaria, request.Username!);
                }
                finally { _writeLock.Release(); }
                return TcpResponse.Ok(true);
            case "delete-reservation":
                await _writeLock.WaitAsync();
                try
                {
                    RequireAuthenticated(request);
                    _database.EzabatuErreserba(request.Payload.GetProperty("id").GetInt32(), request.Username!, request.Role == Erabiltzailea.Administratzailea);
                }
                finally { _writeLock.Release(); }
                return TcpResponse.Ok(true);
            case "sale":
                RequireAuthenticated(request);
                List<SaskiLerroa> basket = request.Payload.Deserialize<List<SaskiLerroa>>() ?? throw new InvalidDataException("Carrito no válido.");
                await _writeLock.WaitAsync();
                try { return TcpResponse.Ok(_database.GordeSalmenta(request.Username!, basket)); }
                finally { _writeLock.Release(); }
            default:
                return TcpResponse.Fail($"Operación desconocida: {request.Operation}");
        }
    }

    private static void RequireAuthenticated(TcpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new InvalidOperationException("La sesión no está autenticada.");
        }
    }

    private static void RequireAdmin(TcpRequest request)
    {
        if (request.Role != Erabiltzailea.Administratzailea)
        {
            throw new InvalidOperationException("Se requieren permisos de administrador.");
        }
    }

    public ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        _clients.Dispose();
        _writeLock.Dispose();
        _stop.Dispose();
        return ValueTask.CompletedTask;
    }
}
