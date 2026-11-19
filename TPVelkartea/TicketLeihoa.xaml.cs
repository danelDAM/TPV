using System.Text;
using System.Windows;
using System.Windows.Controls;
using TPVelkartea.Ereduak;

namespace TPVelkartea;

/// <summary>
/// Salmenta baten ticket-a erakusten duen leihoa: produktuak, kopuruak eta
/// ordaindu beharreko guztizkoa. "Inprimatu" botoiarekin inprimagailura bidal daiteke.
/// </summary>
public partial class TicketLeihoa : Window
{
    private readonly Salmenta _salmenta;

    public TicketLeihoa(Salmenta salmenta)
    {
        InitializeComponent();
        _salmenta = salmenta;
        BeteTicketa();
    }

    private void BeteTicketa()
    {
        GoiburuaTestua.Text =
            $"Ticket zk.:  {_salmenta.Id}\n" +
            $"Data:        {_salmenta.DataOrdua}\n" +
            $"Langilea:    {_salmenta.Erabiltzailea}";

        // Lerro bakoitza zutabeetan: kopurua, izena eta guztira.
        // {x,-22} → ezkerrean, 22 karaktere; {x,8} → eskuinean, 8 karaktere
        StringBuilder lerroak = new StringBuilder();
        foreach (SaskiLerroa lerroa in _salmenta.Lerroak)
        {
            string izena = lerroa.Produktua.Izena;
            if (izena.Length > 22)
            {
                izena = izena.Substring(0, 21) + ".";
            }

            lerroak.AppendLine($"{lerroa.Kopurua,3} x {izena,-22} {lerroa.Guztira,8:0.00} €");
        }
        LerroakTestua.Text = lerroak.ToString().TrimEnd();

        GuztiraTestua.Text = $"GUZTIRA: {_salmenta.Guztira:0.00} €";
    }

    private void Inprimatu_Click(object sender, RoutedEventArgs e)
    {
        PrintDialog inprimatzeLeihoa = new PrintDialog();
        if (inprimatzeLeihoa.ShowDialog() == true)
        {
            // Ticket-aren zatia bakarrik inprimatzen da (botoiak ez)
            inprimatzeLeihoa.PrintVisual(TicketPapera, $"Ticket-a {_salmenta.Id}");
        }
    }

    private void Itxi_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
