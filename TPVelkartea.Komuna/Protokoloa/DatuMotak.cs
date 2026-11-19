namespace TPVelkartea.Protokoloa;

/// <summary>
/// Erreserbak bilatzeko datuak. Data hutsik badago, data gabeko bilaketa da.
/// </summary>
public class ErreserbaBilaketa
{
    public string Data { get; set; } = "";
    public string Txanda { get; set; } = "";
}

/// <summary>
/// Erabiltzaile bat gordetzeko datuak (erabiltzailea + pasahitz berria).
/// </summary>
public class ErabiltzaileDatuak
{
    public TPVelkartea.Ereduak.Erabiltzailea Erabiltzailea { get; set; } = new TPVelkartea.Ereduak.Erabiltzailea();
    public string Pasahitza { get; set; } = "";
}

/// <summary>
/// Produktu baten stockari unitateak gehitzeko datuak.
/// </summary>
public class StockDatuak
{
    public int ProduktuId { get; set; }
    public int Kopurua { get; set; }
}

/// <summary>
/// Saioa hasteko datuak.
/// </summary>
public class SaioDatuak
{
    public string Izena { get; set; } = "";
    public string Pasahitza { get; set; } = "";
}
