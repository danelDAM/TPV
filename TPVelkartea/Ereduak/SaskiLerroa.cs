namespace TPVelkartea.Ereduak;

/// <summary>
/// Saskiko (ticket-eko) lerro bat: produktu bat eta zenbat unitate.
/// </summary>
public class SaskiLerroa
{
    public Produktua Produktua { get; set; }
    public int Kopurua { get; set; }

    public SaskiLerroa(Produktua produktua, int kopurua)
    {
        Produktua = produktua;
        Kopurua = kopurua;
    }

    public decimal Guztira
    {
        get { return Produktua.Prezioa * Kopurua; }
    }

    // ListBox-ean testu hau agertzen da
    public override string ToString()
    {
        return $"{Kopurua} x {Produktua.Izena}  =  {Guztira:0.00} €";
    }
}
