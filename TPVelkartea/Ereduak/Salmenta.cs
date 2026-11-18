namespace TPVelkartea.Ereduak;

/// <summary>
/// Amaitutako salmenta baten goiburua (administrazioko zerrendan erakusteko).
/// </summary>
public class Salmenta
{
    public int Id { get; set; }
    public string Erabiltzailea { get; set; } = "";
    public string DataOrdua { get; set; } = "";
    public decimal Guztira { get; set; }
}
