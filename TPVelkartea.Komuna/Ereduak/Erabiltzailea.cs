namespace TPVelkartea.Ereduak;

/// <summary>
/// Aplikazioan sar daitekeen pertsona bat.
/// Pasahitza ez dago hemen gordeta: datu-basean bakarrik dago (hash moduan).
/// </summary>
public class Erabiltzailea
{
    // Rol posibleak
    public const string Administratzailea = "Administratzailea";
    public const string Langilea = "Langilea";

    public static readonly string[] Rolak = { Administratzailea, Langilea };

    public int Id { get; set; }
    public string Izena { get; set; } = "";
    public string Rola { get; set; } = Langilea;

    public bool AdministratzaileaDa()
    {
        return Rola == Administratzailea;
    }
}
