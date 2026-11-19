using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using TPVelkartea.Ereduak;

namespace TPVelkartea.Zerbitzaria.Datuak;

/// <summary>
/// SQLite datu-basearekin lan egiten duen klasea. Zerbitzariak bakarrik erabiltzen du.
///
/// Hainbat bezero aldi berean konekta daitezkeenez (hari bat bezero bakoitzeko),
/// datu-basean IDAZTEN duten metodo guztiak lock bakar baten barruan exekutatzen dira.
/// Horrela, bi erabiltzailek ezin dute aldi berean taula bera aldatu.
/// </summary>
public class DatuBasea
{
    // Idazketa guztientzako blokeo bakarra (eskualde kritikoa)
    private readonly object _idazketaBlokeoa = new object();

    private readonly string _fitxategia;
    private readonly string _konexioKatea;

    public DatuBasea(string fitxategia)
    {
        _fitxategia = fitxategia;

        // Karpeta ez badago, sortu egiten da
        Directory.CreateDirectory(Path.GetDirectoryName(fitxategia)!);
        _konexioKatea = "Data Source=" + fitxategia;

        SortuTaulak();
        SartuHasierakoDatuak();
    }

    private SqliteConnection Konektatu()
    {
        SqliteConnection konexioa = new SqliteConnection(_konexioKatea);
        konexioa.Open();
        return konexioa;
    }

    // =====================================================================
    //  HASIERAKETA ETA BABESKOPIA
    // =====================================================================

    private void SortuTaulak()
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = @"
            CREATE TABLE IF NOT EXISTS Erabiltzaileak (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Izena     TEXT NOT NULL UNIQUE,
                Pasahitza TEXT NOT NULL,
                Rola      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Produktuak (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Izena     TEXT NOT NULL,
                Kategoria TEXT NOT NULL,
                Prezioa   REAL NOT NULL,
                Stocka    INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Erreserbak (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                MahaiZenbakia INTEGER NOT NULL,
                Data          TEXT NULL,
                Txanda        TEXT NOT NULL,
                Erabiltzailea TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Salmentak (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Erabiltzailea TEXT NOT NULL,
                DataOrdua     TEXT NOT NULL,
                Guztira       REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SalmentaLerroak (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                SalmentaId    INTEGER NOT NULL REFERENCES Salmentak(Id),
                ProduktuId    INTEGER NOT NULL,
                ProduktuIzena TEXT NOT NULL,
                Kopurua       INTEGER NOT NULL,
                Prezioa       REAL NOT NULL
            );";
        komandoa.ExecuteNonQuery();
    }

    private void SartuHasierakoDatuak()
    {
        if (LortuErabiltzaileak().Count == 0)
        {
            foreach (string[] datuak in HasierakoDatuak.Erabiltzaileak())
            {
                Erabiltzailea erabiltzailea = new Erabiltzailea();
                erabiltzailea.Izena = datuak[0];
                erabiltzailea.Rola = datuak[2];
                GordeErabiltzailea(erabiltzailea, datuak[1]);
            }
        }

        if (LortuProduktuak().Count == 0)
        {
            foreach (Produktua produktua in HasierakoDatuak.Produktuak())
            {
                GordeProduktua(produktua);
            }
        }
    }

    /// <summary>
    /// Datu-basearen kopia bat egiten du "Babeskopiak" karpetan.
    /// SQLite-ren "VACUUM INTO" aginduak kopia osoa eta koherentea sortzen du.
    /// Sortutako fitxategiaren bidea itzultzen du.
    /// </summary>
    public string EginBabeskopia()
    {
        string karpeta = Path.Combine(Path.GetDirectoryName(_fitxategia)!, "Babeskopiak");
        Directory.CreateDirectory(karpeta);
        string kopia = Path.Combine(karpeta, "elkartea_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".db");

        lock (_idazketaBlokeoa)
        {
            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();
            komandoa.CommandText = "VACUUM INTO @kopia";
            komandoa.Parameters.AddWithValue("@kopia", kopia);
            komandoa.ExecuteNonQuery();
        }

        return kopia;
    }

    // =====================================================================
    //  ERABILTZAILEAK
    // =====================================================================

    /// <summary>
    /// Izena eta pasahitza zuzenak badira, erabiltzailea itzultzen du. Bestela, null.
    /// </summary>
    public Erabiltzailea? SaioaHasi(string izena, string pasahitza)
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "SELECT Id, Izena, Rola FROM Erabiltzaileak WHERE Izena = @izena AND Pasahitza = @pasahitza";
        komandoa.Parameters.AddWithValue("@izena", izena);
        komandoa.Parameters.AddWithValue("@pasahitza", PasahitzarenHasha(pasahitza));

        using SqliteDataReader irakurlea = komandoa.ExecuteReader();
        if (!irakurlea.Read())
        {
            return null;
        }

        return IrakurriErabiltzailea(irakurlea);
    }

    public List<Erabiltzailea> LortuErabiltzaileak()
    {
        List<Erabiltzailea> erabiltzaileak = new List<Erabiltzailea>();

        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "SELECT Id, Izena, Rola FROM Erabiltzaileak ORDER BY Izena";

        using SqliteDataReader irakurlea = komandoa.ExecuteReader();
        while (irakurlea.Read())
        {
            erabiltzaileak.Add(IrakurriErabiltzailea(irakurlea));
        }

        return erabiltzaileak;
    }

    /// <summary>
    /// Erabiltzaile berria sortzen du (Id = 0) edo dagoena aldatzen du.
    /// Erabiltzaile bat aldatzean pasahitza hutsik badago, zaharra mantentzen da.
    /// </summary>
    public void GordeErabiltzailea(Erabiltzailea erabiltzailea, string pasahitza)
    {
        lock (_idazketaBlokeoa)
        {
            if (IzenaHartutaDago(erabiltzailea.Izena, erabiltzailea.Id))
            {
                throw new InvalidOperationException("Izen hori duen erabiltzaile bat badago jada.");
            }

            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();

            if (erabiltzailea.Id == 0)
            {
                komandoa.CommandText = "INSERT INTO Erabiltzaileak (Izena, Pasahitza, Rola) VALUES (@izena, @pasahitza, @rola)";
            }
            else if (pasahitza == "")
            {
                komandoa.CommandText = "UPDATE Erabiltzaileak SET Izena = @izena, Rola = @rola WHERE Id = @id";
            }
            else
            {
                komandoa.CommandText = "UPDATE Erabiltzaileak SET Izena = @izena, Pasahitza = @pasahitza, Rola = @rola WHERE Id = @id";
            }

            komandoa.Parameters.AddWithValue("@id", erabiltzailea.Id);
            komandoa.Parameters.AddWithValue("@izena", erabiltzailea.Izena);
            komandoa.Parameters.AddWithValue("@pasahitza", PasahitzarenHasha(pasahitza));
            komandoa.Parameters.AddWithValue("@rola", erabiltzailea.Rola);
            komandoa.ExecuteNonQuery();
        }
    }

    public void EzabatuErabiltzailea(int id)
    {
        lock (_idazketaBlokeoa)
        {
            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();
            komandoa.CommandText = "DELETE FROM Erabiltzaileak WHERE Id = @id";
            komandoa.Parameters.AddWithValue("@id", id);
            komandoa.ExecuteNonQuery();
        }
    }

    private bool IzenaHartutaDago(string izena, int idHauEzik)
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "SELECT COUNT(*) FROM Erabiltzaileak WHERE Izena = @izena AND Id <> @id";
        komandoa.Parameters.AddWithValue("@izena", izena);
        komandoa.Parameters.AddWithValue("@id", idHauEzik);

        long kopurua = (long)komandoa.ExecuteScalar()!;
        return kopurua > 0;
    }

    private static Erabiltzailea IrakurriErabiltzailea(SqliteDataReader irakurlea)
    {
        Erabiltzailea erabiltzailea = new Erabiltzailea();
        erabiltzailea.Id = irakurlea.GetInt32(0);
        erabiltzailea.Izena = irakurlea.GetString(1);
        erabiltzailea.Rola = irakurlea.GetString(2);
        return erabiltzailea;
    }

    /// <summary>
    /// Pasahitzak ez dira testu arruntean gordetzen: SHA-256 hash-a gordetzen da.
    /// </summary>
    private static string PasahitzarenHasha(string pasahitza)
    {
        byte[] byteak = SHA256.HashData(Encoding.UTF8.GetBytes(pasahitza));
        return Convert.ToHexString(byteak);
    }

    // =====================================================================
    //  PRODUKTUAK
    // =====================================================================

    public List<Produktua> LortuProduktuak()
    {
        List<Produktua> produktuak = new List<Produktua>();

        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "SELECT Id, Izena, Kategoria, Prezioa, Stocka FROM Produktuak ORDER BY Kategoria, Izena";

        using SqliteDataReader irakurlea = komandoa.ExecuteReader();
        while (irakurlea.Read())
        {
            Produktua produktua = new Produktua();
            produktua.Id = irakurlea.GetInt32(0);
            produktua.Izena = irakurlea.GetString(1);
            produktua.Kategoria = irakurlea.GetString(2);
            produktua.Prezioa = irakurlea.GetDecimal(3);
            produktua.Stocka = irakurlea.GetInt32(4);
            produktuak.Add(produktua);
        }

        return produktuak;
    }

    /// <summary>
    /// Produktu berria sortzen du (Id = 0) edo dagoena aldatzen du.
    /// </summary>
    public void GordeProduktua(Produktua produktua)
    {
        lock (_idazketaBlokeoa)
        {
            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();

            if (produktua.Id == 0)
            {
                komandoa.CommandText = "INSERT INTO Produktuak (Izena, Kategoria, Prezioa, Stocka) VALUES (@izena, @kategoria, @prezioa, @stocka)";
            }
            else
            {
                komandoa.CommandText = "UPDATE Produktuak SET Izena = @izena, Kategoria = @kategoria, Prezioa = @prezioa, Stocka = @stocka WHERE Id = @id";
            }

            komandoa.Parameters.AddWithValue("@id", produktua.Id);
            komandoa.Parameters.AddWithValue("@izena", produktua.Izena);
            komandoa.Parameters.AddWithValue("@kategoria", produktua.Kategoria);
            komandoa.Parameters.AddWithValue("@prezioa", (double)produktua.Prezioa);
            komandoa.Parameters.AddWithValue("@stocka", produktua.Stocka);
            komandoa.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Biltegira unitateak sartu: stockari kopurua gehitzen dio.
    /// </summary>
    public void GehituStocka(int produktuId, int kopurua)
    {
        lock (_idazketaBlokeoa)
        {
            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();
            komandoa.CommandText = "UPDATE Produktuak SET Stocka = Stocka + @kopurua WHERE Id = @id";
            komandoa.Parameters.AddWithValue("@kopurua", kopurua);
            komandoa.Parameters.AddWithValue("@id", produktuId);

            if (komandoa.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Produktu hori ez da existitzen.");
            }
        }
    }

    public void EzabatuProduktua(int id)
    {
        lock (_idazketaBlokeoa)
        {
            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();
            komandoa.CommandText = "DELETE FROM Produktuak WHERE Id = @id";
            komandoa.Parameters.AddWithValue("@id", id);
            komandoa.ExecuteNonQuery();
        }
    }

    // =====================================================================
    //  SALMENTAK
    // =====================================================================

    /// <summary>
    /// Salmenta bat gordetzen du eta produktuen stocka jaisten du.
    /// Prezioak datu-basetik hartzen dira (ez bezeroak bidalitakoetatik).
    /// Dena transakzio baten barruan egiten da: zerbaitek huts egiten badu,
    /// ez da ezer gordetzen. Gordetako salmenta (ticket-a) itzultzen du.
    /// </summary>
    public Salmenta GordeSalmenta(string erabiltzailea, List<SaskiLerroa> saskia)
    {
        if (saskia.Count == 0)
        {
            throw new InvalidOperationException("Saskia hutsik dago.");
        }

        lock (_idazketaBlokeoa)
        {
            using SqliteConnection konexioa = Konektatu();
            using SqliteTransaction transakzioa = konexioa.BeginTransaction();

            // 1. Lerro bakoitzeko: datu-baseko izena eta prezioa irakurri, eta stocka jaitsi
            decimal guztira = 0;
            foreach (SaskiLerroa lerroa in saskia)
            {
                if (lerroa.Kopurua <= 0)
                {
                    throw new InvalidOperationException("Kopuruak positiboa izan behar du.");
                }

                SqliteCommand irakurri = konexioa.CreateCommand();
                irakurri.Transaction = transakzioa;
                irakurri.CommandText = "SELECT Izena, Prezioa, Stocka FROM Produktuak WHERE Id = @id";
                irakurri.Parameters.AddWithValue("@id", lerroa.Produktua.Id);

                using (SqliteDataReader irakurlea = irakurri.ExecuteReader())
                {
                    if (!irakurlea.Read())
                    {
                        throw new InvalidOperationException("Produktu bat ez da existitzen: " + lerroa.Produktua.Izena);
                    }

                    lerroa.Produktua.Izena = irakurlea.GetString(0);
                    lerroa.Produktua.Prezioa = irakurlea.GetDecimal(1);
                    int stocka = irakurlea.GetInt32(2);

                    if (stocka < lerroa.Kopurua)
                    {
                        throw new InvalidOperationException($"Ez dago nahikoa stock: {lerroa.Produktua.Izena} ({stocka} geratzen dira).");
                    }
                }

                SqliteCommand stockKomandoa = konexioa.CreateCommand();
                stockKomandoa.Transaction = transakzioa;
                stockKomandoa.CommandText = "UPDATE Produktuak SET Stocka = Stocka - @kopurua WHERE Id = @id";
                stockKomandoa.Parameters.AddWithValue("@kopurua", lerroa.Kopurua);
                stockKomandoa.Parameters.AddWithValue("@id", lerroa.Produktua.Id);
                stockKomandoa.ExecuteNonQuery();

                guztira += lerroa.Guztira;
            }

            // 2. Salmentaren goiburua gorde
            Salmenta salmenta = new Salmenta();
            salmenta.Erabiltzailea = erabiltzailea;
            salmenta.DataOrdua = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            salmenta.Guztira = guztira;

            SqliteCommand salmentaKomandoa = konexioa.CreateCommand();
            salmentaKomandoa.Transaction = transakzioa;
            salmentaKomandoa.CommandText = "INSERT INTO Salmentak (Erabiltzailea, DataOrdua, Guztira) VALUES (@erabiltzailea, @dataOrdua, @guztira); SELECT last_insert_rowid();";
            salmentaKomandoa.Parameters.AddWithValue("@erabiltzailea", salmenta.Erabiltzailea);
            salmentaKomandoa.Parameters.AddWithValue("@dataOrdua", salmenta.DataOrdua);
            salmentaKomandoa.Parameters.AddWithValue("@guztira", (double)salmenta.Guztira);
            salmenta.Id = Convert.ToInt32(salmentaKomandoa.ExecuteScalar());

            // 3. Salmentaren lerroak gorde
            foreach (SaskiLerroa lerroa in saskia)
            {
                SqliteCommand lerroKomandoa = konexioa.CreateCommand();
                lerroKomandoa.Transaction = transakzioa;
                lerroKomandoa.CommandText = "INSERT INTO SalmentaLerroak (SalmentaId, ProduktuId, ProduktuIzena, Kopurua, Prezioa) VALUES (@salmenta, @produktua, @izena, @kopurua, @prezioa)";
                lerroKomandoa.Parameters.AddWithValue("@salmenta", salmenta.Id);
                lerroKomandoa.Parameters.AddWithValue("@produktua", lerroa.Produktua.Id);
                lerroKomandoa.Parameters.AddWithValue("@izena", lerroa.Produktua.Izena);
                lerroKomandoa.Parameters.AddWithValue("@kopurua", lerroa.Kopurua);
                lerroKomandoa.Parameters.AddWithValue("@prezioa", (double)lerroa.Produktua.Prezioa);
                lerroKomandoa.ExecuteNonQuery();
            }

            // Salbuespen bat gertatzen bada Commit-aren aurretik, transakzioa
            // "using"-ak ixtean automatikoki desegiten da (rollback).
            transakzioa.Commit();

            salmenta.Lerroak = saskia;
            return salmenta;
        }
    }

    public List<Salmenta> LortuSalmentak()
    {
        List<Salmenta> salmentak = new List<Salmenta>();

        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "SELECT Id, Erabiltzailea, DataOrdua, Guztira FROM Salmentak ORDER BY Id DESC";

        using SqliteDataReader irakurlea = komandoa.ExecuteReader();
        while (irakurlea.Read())
        {
            Salmenta salmenta = new Salmenta();
            salmenta.Id = irakurlea.GetInt32(0);
            salmenta.Erabiltzailea = irakurlea.GetString(1);
            salmenta.DataOrdua = irakurlea.GetString(2);
            salmenta.Guztira = irakurlea.GetDecimal(3);
            salmentak.Add(salmenta);
        }

        return salmentak;
    }

    // =====================================================================
    //  ERRESERBAK
    //  Data "" bada, erreserba data zehaztu gabe dago. Data gabeko erreserba batek
    //  mahai hori txanda horretan okupatzen du egun guztietan.
    // =====================================================================

    /// <summary>
    /// Data bat emanda: egun horretako erreserbak + data gabekoak.
    /// Data hutsik emanda: gaurtik aurrerako erreserba guztiak + data gabekoak.
    /// </summary>
    public List<Erreserba> LortuErreserbak(string data, string txanda)
    {
        List<Erreserba> erreserbak = new List<Erreserba>();

        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();

        if (data == "")
        {
            komandoa.CommandText = "SELECT Id, MahaiZenbakia, Data, Txanda, Erabiltzailea FROM Erreserbak " +
                                   "WHERE Txanda = @txanda AND (Data IS NULL OR Data >= @gaur) ORDER BY MahaiZenbakia, Data";
        }
        else
        {
            komandoa.CommandText = "SELECT Id, MahaiZenbakia, Data, Txanda, Erabiltzailea FROM Erreserbak " +
                                   "WHERE Txanda = @txanda AND (Data IS NULL OR Data = @data) ORDER BY MahaiZenbakia";
        }

        komandoa.Parameters.AddWithValue("@txanda", txanda);
        komandoa.Parameters.AddWithValue("@data", data);
        komandoa.Parameters.AddWithValue("@gaur", DateTime.Today.ToString("yyyy-MM-dd"));

        using SqliteDataReader irakurlea = komandoa.ExecuteReader();
        while (irakurlea.Read())
        {
            Erreserba erreserba = new Erreserba();
            erreserba.Id = irakurlea.GetInt32(0);
            erreserba.MahaiZenbakia = irakurlea.GetInt32(1);
            erreserba.Data = irakurlea.IsDBNull(2) ? "" : irakurlea.GetString(2);
            erreserba.Txanda = irakurlea.GetString(3);
            erreserba.Erabiltzailea = irakurlea.GetString(4);
            erreserbak.Add(erreserba);
        }

        return erreserbak;
    }

    public Erreserba LortuErreserba(int id)
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "SELECT Id, MahaiZenbakia, Data, Txanda, Erabiltzailea FROM Erreserbak WHERE Id = @id";
        komandoa.Parameters.AddWithValue("@id", id);

        using SqliteDataReader irakurlea = komandoa.ExecuteReader();
        if (!irakurlea.Read())
        {
            throw new InvalidOperationException("Erreserba hori ez da existitzen.");
        }

        Erreserba erreserba = new Erreserba();
        erreserba.Id = irakurlea.GetInt32(0);
        erreserba.MahaiZenbakia = irakurlea.GetInt32(1);
        erreserba.Data = irakurlea.IsDBNull(2) ? "" : irakurlea.GetString(2);
        erreserba.Txanda = irakurlea.GetString(3);
        erreserba.Erabiltzailea = irakurlea.GetString(4);
        return erreserba;
    }

    /// <summary>
    /// Erreserba berria sortzen du (Id = 0) edo dagoena aldatzen du.
    /// Mahaia libre ez badago, salbuespena jaurtitzen du.
    /// </summary>
    public void GordeErreserba(Erreserba erreserba)
    {
        lock (_idazketaBlokeoa)
        {
            // Begiratu eta gorde lock beraren barruan: bestela bi bezerok
            // mahai bera erreserba lezakete aldi berean.
            if (MahaiaOkupatutaDago(erreserba))
            {
                throw new InvalidOperationException("Mahai hori dagoeneko erreserbatuta dago txanda horretan.");
            }

            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();

            if (erreserba.Id == 0)
            {
                komandoa.CommandText = "INSERT INTO Erreserbak (MahaiZenbakia, Data, Txanda, Erabiltzailea) VALUES (@mahaia, @data, @txanda, @erabiltzailea)";
            }
            else
            {
                komandoa.CommandText = "UPDATE Erreserbak SET MahaiZenbakia = @mahaia, Data = @data, Txanda = @txanda WHERE Id = @id";
            }

            komandoa.Parameters.AddWithValue("@id", erreserba.Id);
            komandoa.Parameters.AddWithValue("@mahaia", erreserba.MahaiZenbakia);
            komandoa.Parameters.AddWithValue("@data", erreserba.Data == "" ? DBNull.Value : erreserba.Data);
            komandoa.Parameters.AddWithValue("@txanda", erreserba.Txanda);
            komandoa.Parameters.AddWithValue("@erabiltzailea", erreserba.Erabiltzailea);
            komandoa.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Mahaia txanda horretan beste erreserba batek okupatzen duen begiratzen du.
    /// Data gabeko erreserbek egun guztiak okupatzen dituzte.
    /// </summary>
    private bool MahaiaOkupatutaDago(Erreserba erreserba)
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();

        if (erreserba.Data == "")
        {
            // Data gabe: mahai horrek ezin du erreserbarik izan txanda horretan (gaurtik aurrera)
            komandoa.CommandText = "SELECT COUNT(*) FROM Erreserbak WHERE MahaiZenbakia = @mahaia AND Txanda = @txanda " +
                                   "AND Id <> @id AND (Data IS NULL OR Data >= @gaur)";
        }
        else
        {
            komandoa.CommandText = "SELECT COUNT(*) FROM Erreserbak WHERE MahaiZenbakia = @mahaia AND Txanda = @txanda " +
                                   "AND Id <> @id AND (Data IS NULL OR Data = @data)";
        }

        komandoa.Parameters.AddWithValue("@mahaia", erreserba.MahaiZenbakia);
        komandoa.Parameters.AddWithValue("@txanda", erreserba.Txanda);
        komandoa.Parameters.AddWithValue("@id", erreserba.Id);
        komandoa.Parameters.AddWithValue("@data", erreserba.Data);
        komandoa.Parameters.AddWithValue("@gaur", DateTime.Today.ToString("yyyy-MM-dd"));

        long kopurua = (long)komandoa.ExecuteScalar()!;
        return kopurua > 0;
    }

    public void EzabatuErreserba(int id)
    {
        lock (_idazketaBlokeoa)
        {
            using SqliteConnection konexioa = Konektatu();
            SqliteCommand komandoa = konexioa.CreateCommand();
            komandoa.CommandText = "DELETE FROM Erreserbak WHERE Id = @id";
            komandoa.Parameters.AddWithValue("@id", id);
            komandoa.ExecuteNonQuery();
        }
    }
}
