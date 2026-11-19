namespace TPVelkartea.Ereduak;

/// <summary>
/// Amaitutako salmenta bat (ticket-a).
/// </summary>
public class Salmenta
{
    public int Id { get; set; }
    public string Erabiltzailea { get; set; } = "";
    public string DataOrdua { get; set; } = "";
    public decimal Guztira { get; set; }

    // Ticket-eko lerroak (salmenta egin berritan bakarrik betetzen da)
    public List<SaskiLerroa> Lerroak { get; set; } = new List<SaskiLerroa>();
}
