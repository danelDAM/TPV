namespace TPVelkartea.Ereduak;

/// <summary>
/// Mahai baten erreserba data eta txanda jakin baterako.
/// </summary>
public class Erreserba
{
    // Txanda posibleak
    public const string Bazkaria = "Bazkaria";
    public const string Afaria = "Afaria";

    public int Id { get; set; }
    public int MahaiZenbakia { get; set; }
    public string Data { get; set; } = "";   // "uuuu-hh-ee" formatuan
    public string Txanda { get; set; } = Bazkaria;
    public string Erabiltzailea { get; set; } = "";

    // ListBox-ean testu hau agertzen da
    public override string ToString()
    {
        return $"{MahaiZenbakia}. mahaia  ·  {Txanda}  ·  {Erabiltzailea}";
    }
}
