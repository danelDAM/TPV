using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TPVelkartea.Ereduak;
using TPVelkartea.Protokoloa;
using TPVelkartea.Zerbitzaria.Datuak;

namespace TPVelkartea.Zerbitzaria;

/// <summary>
/// Bezero bakar baten konexioa. Bere hari propioan exekutatzen da.
///
/// Protokoloa: bezeroak lerro bat bidaltzen du (Eskaera JSON-en) eta
/// zerbitzariak lerro bat itzultzen du (Erantzuna JSON-en).
///
/// Saioa hasi duen erabiltzailea hemen gordetzen da. Horregatik zerbitzariak
/// badaki nor den eta zein rol duen: bezeroak ezin du "administratzailea naiz" esan.
/// </summary>
public class BezeroKonexioa
{
    private readonly TcpClient _bezeroa;
    private readonly DatuBasea _datuBasea;
    private readonly Zerbitzaria _zerbitzaria;

    private Erabiltzailea? _erabiltzailea;   // null = saioa ez da hasi

    public BezeroKonexioa(TcpClient bezeroa, DatuBasea datuBasea, Zerbitzaria zerbitzaria)
    {
        _bezeroa = bezeroa;
        _datuBasea = datuBasea;
        _zerbitzaria = zerbitzaria;
    }

    /// <summary>
    /// Hariaren metodo nagusia: bezeroa deskonektatu arte eskaerak irakurri eta erantzun.
    /// </summary>
    public void Exekutatu()
    {
        try
        {
            NetworkStream fluxua = _bezeroa.GetStream();
            StreamReader irakurlea = new StreamReader(fluxua, Encoding.UTF8);
            StreamWriter idazlea = new StreamWriter(fluxua, new UTF8Encoding(false));
            idazlea.AutoFlush = true;

            while (true)
            {
                string? lerroa = irakurlea.ReadLine();
                if (lerroa == null)
                {
                    break;   // bezeroak konexioa itxi du
                }

                Erantzuna erantzuna = Prozesatu(lerroa);
                idazlea.WriteLine(JsonSerializer.Serialize(erantzuna));
            }
        }
        catch (IOException)
        {
            // Konexioa bat-batean moztu da (adibidez, bezeroaren programa itxi da)
        }
        finally
        {
            _bezeroa.Close();
            _zerbitzaria.BezeroaDeskonektatu();
        }
    }

    private Erantzuna Prozesatu(string lerroa)
    {
        Erantzuna erantzuna = new Erantzuna();

        try
        {
            Eskaera? eskaera = JsonSerializer.Deserialize<Eskaera>(lerroa);
            if (eskaera == null)
            {
                throw new InvalidOperationException("Eskaera hutsa.");
            }

            object? emaitza = ExekutatuEkintza(eskaera);
            erantzuna.Ondo = true;
            erantzuna.Datuak = JsonSerializer.Serialize(emaitza);
        }
        catch (InvalidOperationException errorea)
        {
            // Erabiltzaileari erakusteko moduko erroreak (stock gutxi, mahaia okupatuta...)
            erantzuna.Ondo = false;
            erantzuna.Mezua = errorea.Message;
        }
        catch (Exception errorea)
        {
            // Ustekabeko erroreak: kontsolan idatzi eta bezeroari jakinarazi
            Zerbitzaria.Idatzi("ERROREA: " + errorea);
            erantzuna.Ondo = false;
            erantzuna.Mezua = "Zerbitzariko errorea: " + errorea.Message;
        }

        return erantzuna;
    }

    /// <summary>
    /// Ekintza bakoitza dagokion DatuBasea metodora bideratzen du.
    /// Itzultzen den objektua JSON bihurtu eta bezeroari bidaltzen zaio.
    /// </summary>
    private object? ExekutatuEkintza(Eskaera eskaera)
    {
        // Saioa hasi gabe egin daitekeen ekintza bakarra
        if (eskaera.Ekintza == Ekintzak.SaioaHasi)
        {
            return SaioaHasi(Irakurri<SaioDatuak>(eskaera));
        }

        if (_erabiltzailea == null)
        {
            throw new InvalidOperationException("Lehenengo saioa hasi behar da.");
        }

        Zerbitzaria.Idatzi($"{_erabiltzailea.Izena}: {eskaera.Ekintza}");

        switch (eskaera.Ekintza)
        {
            // --- Produktuak ---
            case Ekintzak.Produktuak:
                return _datuBasea.LortuProduktuak();

            case Ekintzak.ProduktuaGorde:
                AdministratzaileaBehar();
                GordeProduktua(Irakurri<Produktua>(eskaera));
                return null;

            case Ekintzak.ProduktuaEzabatu:
                AdministratzaileaBehar();
                _datuBasea.EzabatuProduktua(Irakurri<int>(eskaera));
                return null;

            case Ekintzak.StockaGehitu:
                AdministratzaileaBehar();
                StockDatuak stockDatuak = Irakurri<StockDatuak>(eskaera);
                if (stockDatuak.Kopurua <= 0)
                {
                    throw new InvalidOperationException("Gehitzeko kopuruak positiboa izan behar du.");
                }
                _datuBasea.GehituStocka(stockDatuak.ProduktuId, stockDatuak.Kopurua);
                return null;

            // --- Erabiltzaileak ---
            case Ekintzak.Erabiltzaileak:
                AdministratzaileaBehar();
                return _datuBasea.LortuErabiltzaileak();

            case Ekintzak.ErabiltzaileaGorde:
                AdministratzaileaBehar();
                GordeErabiltzailea(Irakurri<ErabiltzaileDatuak>(eskaera));
                return null;

            case Ekintzak.ErabiltzaileaEzabatu:
                AdministratzaileaBehar();
                EzabatuErabiltzailea(Irakurri<int>(eskaera));
                return null;

            // --- Salmentak ---
            case Ekintzak.SalmentaGorde:
                return _datuBasea.GordeSalmenta(_erabiltzailea.Izena, Irakurri<List<SaskiLerroa>>(eskaera));

            case Ekintzak.Salmentak:
                AdministratzaileaBehar();
                return _datuBasea.LortuSalmentak();

            // --- Erreserbak ---
            case Ekintzak.Erreserbak:
                ErreserbaBilaketa bilaketa = Irakurri<ErreserbaBilaketa>(eskaera);
                return _datuBasea.LortuErreserbak(bilaketa.Data, bilaketa.Txanda);

            case Ekintzak.ErreserbaGorde:
                GordeErreserba(Irakurri<Erreserba>(eskaera));
                return null;

            case Ekintzak.ErreserbaAldatu:
                AldatuErreserba(Irakurri<Erreserba>(eskaera));
                return null;

            case Ekintzak.ErreserbaEzabatu:
                EzabatuErreserba(Irakurri<int>(eskaera));
                return null;

            default:
                throw new InvalidOperationException("Ekintza ezezaguna: " + eskaera.Ekintza);
        }
    }

    // =====================================================================
    //  EKINTZEN LOGIKA ETA BAIMENAK
    // =====================================================================

    private Erabiltzailea SaioaHasi(SaioDatuak datuak)
    {
        Erabiltzailea? erabiltzailea = _datuBasea.SaioaHasi(datuak.Izena, datuak.Pasahitza);
        if (erabiltzailea == null)
        {
            throw new InvalidOperationException("Erabiltzailea edo pasahitza ez da zuzena.");
        }

        _erabiltzailea = erabiltzailea;
        Zerbitzaria.Idatzi($"{erabiltzailea.Izena}-k saioa hasi du ({erabiltzailea.Rola}).");
        return erabiltzailea;
    }

    private void AdministratzaileaBehar()
    {
        if (!_erabiltzailea!.AdministratzaileaDa())
        {
            throw new InvalidOperationException("Ekintza hau administratzaileek bakarrik egin dezakete.");
        }
    }

    private void GordeProduktua(Produktua produktua)
    {
        if (produktua.Izena.Trim() == "")
        {
            throw new InvalidOperationException("Produktuaren izena hutsik dago.");
        }

        if (produktua.Prezioa < 0 || produktua.Stocka < 0)
        {
            throw new InvalidOperationException("Prezioa eta stocka ezin dira negatiboak izan.");
        }

        _datuBasea.GordeProduktua(produktua);
    }

    private void GordeErabiltzailea(ErabiltzaileDatuak datuak)
    {
        Erabiltzailea erabiltzailea = datuak.Erabiltzailea;

        if (erabiltzailea.Izena.Trim() == "")
        {
            throw new InvalidOperationException("Erabiltzailearen izena hutsik dago.");
        }

        if (erabiltzailea.Id == 0 && datuak.Pasahitza == "")
        {
            throw new InvalidOperationException("Erabiltzaile berriak pasahitza behar du.");
        }

        if (erabiltzailea.Id == _erabiltzailea!.Id && !erabiltzailea.AdministratzaileaDa())
        {
            throw new InvalidOperationException("Ezin duzu zure administratzaile-rola kendu.");
        }

        _datuBasea.GordeErabiltzailea(erabiltzailea, datuak.Pasahitza);
    }

    private void EzabatuErabiltzailea(int id)
    {
        if (id == _erabiltzailea!.Id)
        {
            throw new InvalidOperationException("Ezin duzu zure burua ezabatu.");
        }

        _datuBasea.EzabatuErabiltzailea(id);
    }

    private void GordeErreserba(Erreserba erreserba)
    {
        ErreserbaZuzenaDa(erreserba);

        erreserba.Id = 0;
        erreserba.Erabiltzailea = _erabiltzailea!.Izena;   // beti saioa hasi duena
        _datuBasea.GordeErreserba(erreserba);
    }

    private void AldatuErreserba(Erreserba berria)
    {
        ErreserbaZuzenaDa(berria);

        Erreserba zaharra = _datuBasea.LortuErreserba(berria.Id);
        BaimenaErreserbarako(zaharra);

        berria.Erabiltzailea = zaharra.Erabiltzailea;
        _datuBasea.GordeErreserba(berria);
    }

    private void EzabatuErreserba(int id)
    {
        Erreserba erreserba = _datuBasea.LortuErreserba(id);
        BaimenaErreserbarako(erreserba);
        _datuBasea.EzabatuErreserba(id);
    }

    /// <summary>
    /// Langile batek bere erreserbak bakarrik alda/ezaba ditzake; administratzaileak denak.
    /// </summary>
    private void BaimenaErreserbarako(Erreserba erreserba)
    {
        if (!_erabiltzailea!.AdministratzaileaDa() && erreserba.Erabiltzailea != _erabiltzailea.Izena)
        {
            throw new InvalidOperationException("Beste erabiltzaile baten erreserba ezin duzu aldatu edo ezabatu.");
        }
    }

    private static void ErreserbaZuzenaDa(Erreserba erreserba)
    {
        if (erreserba.MahaiZenbakia <= 0)
        {
            throw new InvalidOperationException("Aukeratu mahai bat.");
        }

        if (erreserba.Txanda != Erreserba.Bazkaria && erreserba.Txanda != Erreserba.Afaria)
        {
            throw new InvalidOperationException("Txanda ez da zuzena.");
        }

        // "uuuu-hh-ee" formatuko datak testu gisa konpara daitezke
        if (erreserba.Data != "" && string.Compare(erreserba.Data, DateTime.Today.ToString("yyyy-MM-dd")) < 0)
        {
            throw new InvalidOperationException("Ezin da iraganeko egun batean erreserbatu.");
        }
    }

    /// <summary>
    /// Eskaerako JSON datuak T motako objektu bihurtzen ditu.
    /// </summary>
    private static T Irakurri<T>(Eskaera eskaera)
    {
        T? datuak = JsonSerializer.Deserialize<T>(eskaera.Datuak);
        if (datuak == null)
        {
            throw new InvalidOperationException("Eskaeraren datuak ez dira zuzenak.");
        }

        return datuak;
    }
}
