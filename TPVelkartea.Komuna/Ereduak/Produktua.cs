namespace TPVelkartea.Ereduak;

/// <summary>
/// Elkartean saltzen den produktu bat (platera, edaria edo postrea).
/// </summary>
public class Produktua
{
    // Kategoria posibleak
    public const string Platerak = "Platerak";
    public const string Edariak = "Edariak";
    public const string Postreak = "Postreak";

    public static readonly string[] Kategoriak = { Platerak, Edariak, Postreak };

    public int Id { get; set; }
    public string Izena { get; set; } = "";
    public string Kategoria { get; set; } = Platerak;
    public decimal Prezioa { get; set; }
    public int Stocka { get; set; }
}
