using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TPVelkartea.Protokoloa;
using TPVelkartea.Zerbitzaria.Datuak;

namespace TPVelkartea.Zerbitzaria;

/// <summary>
/// TCP zerbitzaria. Bezeroen konexioak onartzen ditu eta bakoitzari
/// HARI (Thread) independente bat ematen dio. Horrela bezero baten eskaera
/// motel batek ez ditu besteak blokeatzen.
/// Gehienez Konfigurazioa.GehienezkoBezeroak bezero onartzen dira aldi berean.
/// </summary>
public class Zerbitzaria
{
    private readonly DatuBasea _datuBasea;
    private readonly int _ataka;

    // Konektatuta dauden bezeroen kopurua. Hari batek baino gehiagok aldatzen
    // dutenez, lock baten barruan irakurri eta aldatzen da.
    private int _bezeroKopurua = 0;
    private readonly object _kontagailuBlokeoa = new object();

    public Zerbitzaria(DatuBasea datuBasea, int ataka)
    {
        _datuBasea = datuBasea;
        _ataka = ataka;
    }

    public void Abiarazi()
    {
        TcpListener entzulea = new TcpListener(IPAddress.Any, _ataka);
        entzulea.Start();
        Idatzi($"Zerbitzaria martxan {_ataka} atakan. Gehienez {Konfigurazioa.GehienezkoBezeroak} bezero.");

        while (true)
        {
            // Bezero berri bat konektatu arte itxaroten du
            TcpClient bezeroa = entzulea.AcceptTcpClient();

            if (!BezeroaOnartu())
            {
                Idatzi("Bezero bat baztertuta: zerbitzaria beteta dago.");
                BidaliErroreaEtaItxi(bezeroa, $"Zerbitzaria beteta dago (gehienez {Konfigurazioa.GehienezkoBezeroak} bezero). Saiatu geroago.");
                continue;
            }

            // Bezero bakoitzak bere haria
            BezeroKonexioa konexioa = new BezeroKonexioa(bezeroa, _datuBasea, this);
            Thread haria = new Thread(konexioa.Exekutatu);
            haria.IsBackground = true;
            haria.Start();
        }
    }

    /// <summary>
    /// Lekua badago, kontagailua handitu eta true itzultzen du.
    /// </summary>
    private bool BezeroaOnartu()
    {
        lock (_kontagailuBlokeoa)
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

    /// <summary>
    /// Bezero baten haria amaitzean deitzen da.
    /// </summary>
    public void BezeroaDeskonektatu()
    {
        lock (_kontagailuBlokeoa)
        {
            _bezeroKopurua--;
            Idatzi($"Bezero bat deskonektatu da ({_bezeroKopurua}/{Konfigurazioa.GehienezkoBezeroak}).");
        }
    }

    private static void BidaliErroreaEtaItxi(TcpClient bezeroa, string mezua)
    {
        try
        {
            Erantzuna erantzuna = new Erantzuna();
            erantzuna.Ondo = false;
            erantzuna.Mezua = mezua;

            StreamWriter idazlea = new StreamWriter(bezeroa.GetStream(), new UTF8Encoding(false));
            idazlea.WriteLine(JsonSerializer.Serialize(erantzuna));
            idazlea.Flush();
        }
        catch (IOException)
        {
            // Bezeroa jada deskonektatu bada, ez dago ezer egiteko
        }

        bezeroa.Close();
    }

    /// <summary>
    /// Kontsolan mezu bat idazten du orduarekin.
    /// </summary>
    public static void Idatzi(string mezua)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {mezua}");
    }
}
