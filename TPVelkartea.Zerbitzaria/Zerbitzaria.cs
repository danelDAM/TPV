using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TPVelkartea.Protokoloa;
using TPVelkartea.Zerbitzaria.Datuak;

namespace TPVelkartea.Zerbitzaria;

public class Zerbitzaria
{
    // ================================================================
    //  DATOS PRINCIPALES DEL SERVIDOR
    // ================================================================

    // Base de datos que utilizarán las conexiones de los clientes.
    private readonly DatuBasea _datuBasea;

    // Puerto TCP en el que escuchará el servidor.
    private readonly int _ataka;

    // Número de clientes conectados actualmente.
    private int _bezeroKopurua;

    // Objeto utilizado para proteger el contador con lock.
    private readonly object _blokeoKontagailua = new object();

    // El servidor recibe la base de datos y el puerto al crearse.
    public Zerbitzaria(DatuBasea datuBasea, int ataka)
    {
        _datuBasea = datuBasea;
        _ataka = ataka;
    }

    // ================================================================
    //  INICIAR EL SERVIDOR
    // ================================================================

    // Empieza a escuchar conexiones de clientes.
    public void Abiarazi()
    {
        // Creamos el servidor TCP usando cualquier dirección de red
        // y el puerto configurado.
        TcpListener entzulea =
            new TcpListener(IPAddress.Any, _ataka);

        entzulea.Start();

        Idatzi($"Zerbitzaria martxan {_ataka} atakan. Gehienez {Konfigurazioa.GehienezkoBezeroak} bezero.");

        // El servidor queda esperando clientes indefinidamente.
        while (true)
        {
            // Esperamos hasta que un cliente se conecte.
            TcpClient bezeroa = entzulea.AcceptTcpClient();

            // Comprobamos si todavía se pueden aceptar más clientes.
            if (!BezeroaOnartu())
            {
                Idatzi("Bezero bat baztertuta: zerbitzaria beteta dago.");

                // Informamos al cliente y cerramos su conexión.
                BidaliErroreaEtaItxi(bezeroa, $"Zerbitzaria beteta dago (gehienez {Konfigurazioa.GehienezkoBezeroak} bezero). Saiatu geroago.");
                continue;
            }

            // Creamos un objeto que gestionará este cliente.
            BezeroKonexioa konexioa =
                new BezeroKonexioa(bezeroa, _datuBasea, this);

            // Cada cliente se ejecuta en su propio hilo.
            Thread haria =
                new Thread(konexioa.Exekutatu);

            haria.IsBackground = true;
            haria.Start();
        }
    }

    // ================================================================
    //  CONTROLAR LOS CLIENTES CONECTADOS
    // ================================================================

    // Devuelve true si hay espacio para otro cliente.
    private bool BezeroaOnartu()
    {
        // Solo un hilo puede consultar y modificar el contador cada vez.
        lock (_blokeoKontagailua)
        {
            if (_bezeroKopurua >= Konfigurazioa.GehienezkoBezeroak)
            {
                return false;
            }

            _bezeroKopurua++;

            Idatzi($"Bezero berria konektatuta ({_bezeroKopurua}/{Konfigurazioa.GehienezkoBezeroak}).");

            return true;
        }
    }

    // Se llama cuando un cliente termina su conexión.
    public void BezeroaDeskonektatu()
    {
        lock (_blokeoKontagailua)
        {
            _bezeroKopurua--;

            Idatzi($"Bezero bat deskonektatu da ({_bezeroKopurua}/{Konfigurazioa.GehienezkoBezeroak}).");
        }
    }

    // ================================================================
    //  RECHAZAR UN CLIENTE CUANDO EL SERVIDOR ESTÁ LLENO
    // ================================================================

    // Envía un error al cliente y cierra su conexión.
    private static void BidaliErroreaEtaItxi(TcpClient bezeroa, string mezua)
    {
        try
        {
            Erantzuna erantzuna = new Erantzuna
            {
                Ondo = false,
                Mezua = mezua
            };

            StreamWriter idazlea =
                new StreamWriter(
                    bezeroa.GetStream(),
                    new UTF8Encoding(false));

            idazlea.WriteLine(JsonSerializer.Serialize(erantzuna));
            idazlea.Flush();
        }
        catch (IOException)
        {
            // Si el cliente ya se ha desconectado, no hay nada más que hacer.
        }

        bezeroa.Close();
    }

    // Escribe un mensaje en la consola con la hora actual.
    public static void Idatzi(string mezua)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {mezua}");
    }
}
