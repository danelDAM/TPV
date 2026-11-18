using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using TPVelkartea.Ereduak;

namespace TPVelkartea.Datuak;

/// <summary>
/// SQLite datu-basearekin lan egiten duen klasea.
/// Aplikazioaren SQL kontsulta guztiak klase honetan daude.
/// </summary>
public class DatuBasea
{
    private readonly string _konexioKatea;

    public DatuBasea()
        : this(Path.Combine(AppContext.BaseDirectory, "Datuak", "elkartea.db"))
    {
    }

    public DatuBasea(string fitxategia)
    {
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
    //  HASIERAKETA
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
                Data          TEXT NOT NULL,
                Txanda        TEXT NOT NULL,
                Erabiltzailea TEXT NOT NULL,
                UNIQUE (MahaiZenbakia, Data, Txanda)
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

    public void EzabatuErabiltzailea(int id)
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "DELETE FROM Erabiltzaileak WHERE Id = @id";
        komandoa.Parameters.AddWithValue("@id", id);
        komandoa.ExecuteNonQuery();
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

    public void EzabatuProduktua(int id)
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "DELETE FROM Produktuak WHERE Id = @id";
        komandoa.Parameters.AddWithValue("@id", id);
        komandoa.ExecuteNonQuery();
    }

    // =====================================================================
    //  SALMENTAK
    // =====================================================================

    /// <summary>
    /// Salmenta bat gordetzen du eta produktuen stocka jaisten du.
    /// Dena transakzio baten barruan egiten da: zerbaitek huts egiten badu,
    /// ez da ezer gordetzen (ez salmenta, ez stockaren aldaketa).
    /// Salmentaren zenbakia (Id) itzultzen du.
    /// </summary>
    public int GordeSalmenta(string erabiltzailea, List<SaskiLerroa> saskia)
    {
        if (saskia.Count == 0)
        {
            throw new InvalidOperationException("Saskia hutsik dago.");
        }

        decimal guztira = 0;
        foreach (SaskiLerroa lerroa in saskia)
        {
            guztira += lerroa.Guztira;
        }

        using SqliteConnection konexioa = Konektatu();
        using SqliteTransaction transakzioa = konexioa.BeginTransaction();

        // 1. Stocka jaitsi (nahikoa stock badago bakarrik)
        foreach (SaskiLerroa lerroa in saskia)
        {
            SqliteCommand stockKomandoa = konexioa.CreateCommand();
            stockKomandoa.Transaction = transakzioa;
            stockKomandoa.CommandText = "UPDATE Produktuak SET Stocka = Stocka - @kopurua WHERE Id = @id AND Stocka >= @kopurua";
            stockKomandoa.Parameters.AddWithValue("@kopurua", lerroa.Kopurua);
            stockKomandoa.Parameters.AddWithValue("@id", lerroa.Produktua.Id);

            int aldatutakoak = stockKomandoa.ExecuteNonQuery();
            if (aldatutakoak == 0)
            {
                transakzioa.Rollback();
                throw new InvalidOperationException($"Ez dago nahikoa stock: {lerroa.Produktua.Izena}.");
            }
        }

        // 2. Salmentaren goiburua gorde
        SqliteCommand salmentaKomandoa = konexioa.CreateCommand();
        salmentaKomandoa.Transaction = transakzioa;
        salmentaKomandoa.CommandText = "INSERT INTO Salmentak (Erabiltzailea, DataOrdua, Guztira) VALUES (@erabiltzailea, @dataOrdua, @guztira); SELECT last_insert_rowid();";
        salmentaKomandoa.Parameters.AddWithValue("@erabiltzailea", erabiltzailea);
        salmentaKomandoa.Parameters.AddWithValue("@dataOrdua", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        salmentaKomandoa.Parameters.AddWithValue("@guztira", (double)guztira);
        int salmentaId = Convert.ToInt32(salmentaKomandoa.ExecuteScalar());

        // 3. Salmentaren lerroak gorde
        foreach (SaskiLerroa lerroa in saskia)
        {
            SqliteCommand lerroKomandoa = konexioa.CreateCommand();
            lerroKomandoa.Transaction = transakzioa;
            lerroKomandoa.CommandText = "INSERT INTO SalmentaLerroak (SalmentaId, ProduktuId, ProduktuIzena, Kopurua, Prezioa) VALUES (@salmenta, @produktua, @izena, @kopurua, @prezioa)";
            lerroKomandoa.Parameters.AddWithValue("@salmenta", salmentaId);
            lerroKomandoa.Parameters.AddWithValue("@produktua", lerroa.Produktua.Id);
            lerroKomandoa.Parameters.AddWithValue("@izena", lerroa.Produktua.Izena);
            lerroKomandoa.Parameters.AddWithValue("@kopurua", lerroa.Kopurua);
            lerroKomandoa.Parameters.AddWithValue("@prezioa", (double)lerroa.Produktua.Prezioa);
            lerroKomandoa.ExecuteNonQuery();
        }

        transakzioa.Commit();
        return salmentaId;
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
    // =====================================================================

    public List<Erreserba> LortuErreserbak(DateTime data, string txanda)
    {
        List<Erreserba> erreserbak = new List<Erreserba>();

        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "SELECT Id, MahaiZenbakia, Data, Txanda, Erabiltzailea FROM Erreserbak WHERE Data = @data AND Txanda = @txanda ORDER BY MahaiZenbakia";
        komandoa.Parameters.AddWithValue("@data", DataTestura(data));
        komandoa.Parameters.AddWithValue("@txanda", txanda);

        using SqliteDataReader irakurlea = komandoa.ExecuteReader();
        while (irakurlea.Read())
        {
            Erreserba erreserba = new Erreserba();
            erreserba.Id = irakurlea.GetInt32(0);
            erreserba.MahaiZenbakia = irakurlea.GetInt32(1);
            erreserba.Data = irakurlea.GetString(2);
            erreserba.Txanda = irakurlea.GetString(3);
            erreserba.Erabiltzailea = irakurlea.GetString(4);
            erreserbak.Add(erreserba);
        }

        return erreserbak;
    }

    public void GordeErreserba(int mahaiZenbakia, DateTime data, string txanda, string erabiltzailea)
    {
        using SqliteConnection konexioa = Konektatu();

        // Lehenengo begiratu mahaia libre dagoen
        SqliteCommand begiratu = konexioa.CreateCommand();
        begiratu.CommandText = "SELECT COUNT(*) FROM Erreserbak WHERE MahaiZenbakia = @mahaia AND Data = @data AND Txanda = @txanda";
        begiratu.Parameters.AddWithValue("@mahaia", mahaiZenbakia);
        begiratu.Parameters.AddWithValue("@data", DataTestura(data));
        begiratu.Parameters.AddWithValue("@txanda", txanda);

        long kopurua = (long)begiratu.ExecuteScalar()!;
        if (kopurua > 0)
        {
            throw new InvalidOperationException("Mahai hori dagoeneko erreserbatuta dago data eta txanda horretarako.");
        }

        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "INSERT INTO Erreserbak (MahaiZenbakia, Data, Txanda, Erabiltzailea) VALUES (@mahaia, @data, @txanda, @erabiltzailea)";
        komandoa.Parameters.AddWithValue("@mahaia", mahaiZenbakia);
        komandoa.Parameters.AddWithValue("@data", DataTestura(data));
        komandoa.Parameters.AddWithValue("@txanda", txanda);
        komandoa.Parameters.AddWithValue("@erabiltzailea", erabiltzailea);
        komandoa.ExecuteNonQuery();
    }

    public void EzabatuErreserba(int id)
    {
        using SqliteConnection konexioa = Konektatu();
        SqliteCommand komandoa = konexioa.CreateCommand();
        komandoa.CommandText = "DELETE FROM Erreserbak WHERE Id = @id";
        komandoa.Parameters.AddWithValue("@id", id);
        komandoa.ExecuteNonQuery();
    }

    private static string DataTestura(DateTime data)
    {
        return data.ToString("yyyy-MM-dd");
    }
}
