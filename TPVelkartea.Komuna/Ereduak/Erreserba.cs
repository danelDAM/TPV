namespace TPVelkartea.Ereduak;

/// <summary>
/// Mahai baten erreserba txanda baterako.
/// Data hutsik ("") badago, erreserba data zehaztu gabe egin da.
/// </summary>
public class Erreserba
{
    // Txanda posibleak
    public const string Bazkaria = "Bazkaria";
    public const string Afaria = "Afaria";

    public int Id { get; set; }
    public int MahaiZenbakia { get; set; }
    public string Data { get; set; } = "";   // "uuuu-hh-ee" formatuan, edo "" data gabe
    public string Txanda { get; set; } = Bazkaria;
    public string Erabiltzailea { get; set; } = "";

    // ListBox-ean testu hau agertzen da
    public override string ToString()
    {
        string data = Data == "" ? "data gabe" : Data;
        return $"{MahaiZenbakia}. mahaia  ·  {data}  ·  {Txanda}  ·  {Erabiltzailea}";
    }
}
