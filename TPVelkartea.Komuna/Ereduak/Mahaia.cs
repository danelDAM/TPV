namespace TPVelkartea.Ereduak;

/// <summary>
/// Jantokiko mahai bat eta planoan duen posizioa.
/// Mahaiak ez daude datu-basean: kodean finkatuta daude (ikus LeihoNagusia.Erreserbak.cs).
/// </summary>
public class Mahaia
{
    public int Zenbakia { get; set; }
    public int Eserlekuak { get; set; }
    public double X { get; set; }          // erdigunearen posizioa planoan
    public double Y { get; set; }
    public double Zabalera { get; set; }
    public double Altuera { get; set; }
    public bool Biribila { get; set; }

    public Mahaia(int zenbakia, int eserlekuak, double x, double y, double zabalera, double altuera, bool biribila)
    {
        Zenbakia = zenbakia;
        Eserlekuak = eserlekuak;
        X = x;
        Y = y;
        Zabalera = zabalera;
        Altuera = altuera;
        Biribila = biribila;
    }
}
