namespace TPVelkartea.Protokoloa;

/// <summary>
/// Bezeroak zerbitzariari eska diezazkiokeen ekintzen izenak.
/// Konstanteak erabiltzen dira testua gaizki idazteko arriskua ez izateko.
/// </summary>
public static class Ekintzak
{
    // Saioa
    public const string SaioaHasi = "SaioaHasi";

    // Produktuak (irakurri: denek; aldatu: administratzaileak)
    public const string Produktuak = "Produktuak";
    public const string ProduktuaGorde = "ProduktuaGorde";
    public const string ProduktuaEzabatu = "ProduktuaEzabatu";
    public const string StockaGehitu = "StockaGehitu";

    // Erabiltzaileak (administratzaileak bakarrik)
    public const string Erabiltzaileak = "Erabiltzaileak";
    public const string ErabiltzaileaGorde = "ErabiltzaileaGorde";
    public const string ErabiltzaileaEzabatu = "ErabiltzaileaEzabatu";

    // Salmentak
    public const string SalmentaGorde = "SalmentaGorde";
    public const string Salmentak = "Salmentak";

    // Erreserbak
    public const string Erreserbak = "Erreserbak";
    public const string ErreserbaGorde = "ErreserbaGorde";
    public const string ErreserbaAldatu = "ErreserbaAldatu";
    public const string ErreserbaEzabatu = "ErreserbaEzabatu";
}
