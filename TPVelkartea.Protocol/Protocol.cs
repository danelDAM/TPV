using System.Text.Json;

namespace TPVelkartea.Protocol;

public static class ProtocolConstants
{
    public const int DefaultPort = 5050;
    public const int MaxClients = 10;
}

public sealed class TcpRequest
{
    public string Operation { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public JsonElement Payload { get; set; }
}

public sealed class TcpResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public JsonElement Data { get; set; }

    public static TcpResponse Ok<T>(T data) => new()
    {
        Success = true,
        Data = JsonSerializer.SerializeToElement(data)
    };

    public static TcpResponse Fail(string error) => new() { Success = false, Error = error };
}
