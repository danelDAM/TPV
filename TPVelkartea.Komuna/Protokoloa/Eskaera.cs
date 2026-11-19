namespace TPVelkartea.Protokoloa;

/// <summary>
/// Bezeroak zerbitzariari bidaltzen dion mezua.
/// Sarean JSON lerro bakar bat bezala bidaltzen da.
/// </summary>
public class Eskaera
{
    public string Ekintza { get; set; } = "";   // adibidez Ekintzak.Produktuak
    public string Datuak { get; set; } = "";    // ekintzaren datuak, JSON testu gisa
}
