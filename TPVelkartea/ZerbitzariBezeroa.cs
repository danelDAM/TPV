using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TPVelkartea.Ereduak;
using TPVelkartea.Protokoloa;

namespace TPVelkartea;

/// <summary>
/// Zerbitzariarekin TCP bidez hitz egiten duen klasea (bezeroaren aldea).
///
/// Metodo guztiak asinkronoak dira (async/await): zerbitzariaren erantzunaren
/// zain dagoen bitartean, leihoak erantzuten jarraitzen du (ez da "izozten").
/// </summary>
public class ZerbitzariBezeroa
{
    private TcpClient? _konexioa;
    private StreamReader? _irakurlea;
    private StreamWriter? _idazlea;

    // Eskaera bat bakarrik aldi berean: bestela bi erantzun nahas litezke
    private readonly SemaphoreSlim _txanda = new SemaphoreSlim(1, 1);

    public async Task KonektatuAsync(string helbidea)
    {
        Deskonektatu();

        try
        {
            _konexioa = new TcpClient();
            await _konexioa.ConnectAsync(helbidea, Konfigurazioa.Ataka);
        }
        catch (SocketException)
        {
            _konexioa = null;
            throw new InvalidOperationException($"Ezin da zerbitzarira konektatu ({helbidea}:{Konfigurazioa.Ataka}). Zerbitzaria martxan dago?");
        }

        NetworkStream fluxua = _konexioa.GetStream();
        _irakurlea = new StreamReader(fluxua, Encoding.UTF8);
        _idazlea = new StreamWriter(fluxua, new UTF8Encoding(false));
        _idazlea.AutoFlush = true;
    }

    public void Deskonektatu()
    {
        if (_konexioa != null)
        {
            _konexioa.Close();
            _konexioa = null;
        }
    }

    /// <summary>
    /// Eskaera bat bidali eta erantzuna jaso. Zerbitzariak errore bat itzultzen
    /// badu, InvalidOperationException jaurtitzen da mezu horrekin.
    /// </summary>
    private async Task<T> BidaliAsync<T>(string ekintza, object? datuak)
    {
        if (_konexioa == null)
        {
            throw new InvalidOperationException("Ez zaude zerbitzarira konektatuta.");
        }

        Eskaera eskaera = new Eskaera();
        eskaera.Ekintza = ekintza;
        eskaera.Datuak = JsonSerializer.Serialize(datuak);

        await _txanda.WaitAsync();
        try
        {
            await _idazlea!.WriteLineAsync(JsonSerializer.Serialize(eskaera));
            string? lerroa = await _irakurlea!.ReadLineAsync();

            if (lerroa == null)
            {
                throw new IOException();
            }

            Erantzuna erantzuna = JsonSerializer.Deserialize<Erantzuna>(lerroa)!;
            if (!erantzuna.Ondo)
            {
                throw new InvalidOperationException(erantzuna.Mezua);
            }

            return JsonSerializer.Deserialize<T>(erantzuna.Datuak)!;
        }
        catch (IOException)
        {
            Deskonektatu();
            throw new InvalidOperationException("Zerbitzariarekin konexioa galdu da. Hasi saioa berriro.");
        }
        finally
        {
            _txanda.Release();
        }
    }

    // Emaitzarik itzultzen ez duten ekintzetarako
    private async Task BidaliAsync(string ekintza, object? datuak)
    {
        await BidaliAsync<object?>(ekintza, datuak);
    }

    // =====================================================================
    //  EKINTZAK (bat zerbitzariko ekintza bakoitzeko)
    // =====================================================================

    public Task<Erabiltzailea> SaioaHasiAsync(string izena, string pasahitza)
    {
        SaioDatuak datuak = new SaioDatuak();
        datuak.Izena = izena;
        datuak.Pasahitza = pasahitza;
        return BidaliAsync<Erabiltzailea>(Ekintzak.SaioaHasi, datuak);
    }

    // --- Produktuak ---
    public Task<List<Produktua>> LortuProduktuakAsync()
    {
        return BidaliAsync<List<Produktua>>(Ekintzak.Produktuak, null);
    }

    public Task GordeProduktuaAsync(Produktua produktua)
    {
        return BidaliAsync(Ekintzak.ProduktuaGorde, produktua);
    }

    public Task EzabatuProduktuaAsync(int id)
    {
        return BidaliAsync(Ekintzak.ProduktuaEzabatu, id);
    }

    public Task GehituStockaAsync(int produktuId, int kopurua)
    {
        StockDatuak datuak = new StockDatuak();
        datuak.ProduktuId = produktuId;
        datuak.Kopurua = kopurua;
        return BidaliAsync(Ekintzak.StockaGehitu, datuak);
    }

    // --- Erabiltzaileak ---
    public Task<List<Erabiltzailea>> LortuErabiltzaileakAsync()
    {
        return BidaliAsync<List<Erabiltzailea>>(Ekintzak.Erabiltzaileak, null);
    }

    public Task GordeErabiltzaileaAsync(Erabiltzailea erabiltzailea, string pasahitza)
    {
        ErabiltzaileDatuak datuak = new ErabiltzaileDatuak();
        datuak.Erabiltzailea = erabiltzailea;
        datuak.Pasahitza = pasahitza;
        return BidaliAsync(Ekintzak.ErabiltzaileaGorde, datuak);
    }

    public Task EzabatuErabiltzaileaAsync(int id)
    {
        return BidaliAsync(Ekintzak.ErabiltzaileaEzabatu, id);
    }

    // --- Salmentak ---
    public Task<Salmenta> GordeSalmentaAsync(List<SaskiLerroa> saskia)
    {
        return BidaliAsync<Salmenta>(Ekintzak.SalmentaGorde, saskia);
    }

    public Task<List<Salmenta>> LortuSalmentakAsync()
    {
        return BidaliAsync<List<Salmenta>>(Ekintzak.Salmentak, null);
    }

    // --- Erreserbak ---
    public Task<List<Erreserba>> LortuErreserbakAsync(string data, string txanda)
    {
        ErreserbaBilaketa bilaketa = new ErreserbaBilaketa();
        bilaketa.Data = data;
        bilaketa.Txanda = txanda;
        return BidaliAsync<List<Erreserba>>(Ekintzak.Erreserbak, bilaketa);
    }

    public Task GordeErreserbaAsync(Erreserba erreserba)
    {
        return BidaliAsync(Ekintzak.ErreserbaGorde, erreserba);
    }

    public Task AldatuErreserbaAsync(Erreserba erreserba)
    {
        return BidaliAsync(Ekintzak.ErreserbaAldatu, erreserba);
    }

    public Task EzabatuErreserbaAsync(int id)
    {
        return BidaliAsync(Ekintzak.ErreserbaEzabatu, id);
    }
}
