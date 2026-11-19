using TPVelkartea.Server;

int port = args.Length > 0 && int.TryParse(args[0], out int configuredPort)
    ? configuredPort
    : TPVelkartea.Protocol.ProtocolConstants.DefaultPort;

await using TcpServer server = new(port);
Console.WriteLine($"TPV server escuchando en 0.0.0.0:{port}. Máximo de clientes: {TPVelkartea.Protocol.ProtocolConstants.MaxClients}.");
Console.WriteLine("Pulsa Ctrl+C para detenerlo.");
await server.RunAsync();
