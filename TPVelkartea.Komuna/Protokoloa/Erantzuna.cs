namespace TPVelkartea.Protokoloa;

/// <summary>
/// Zerbitzariak bezeroari itzultzen dion mezua.
/// Sarean JSON lerro bakar bat bezala bidaltzen da.
/// </summary>
public class Erantzuna
{
    public bool Ondo { get; set; }              // false bada, Mezua-n dago errorea
    public string Mezua { get; set; } = "";
    public string Datuak { get; set; } = "";    // emaitza, JSON testu gisa
}
