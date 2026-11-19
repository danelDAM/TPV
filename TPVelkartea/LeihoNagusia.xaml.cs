using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TPVelkartea.Ereduak;

namespace TPVelkartea;

/// <summary>
/// Aplikazioaren (bezeroaren) leiho nagusia.
/// Datu guztiak zerbitzaritik hartzen dira TCP bidez (ikus ZerbitzariBezeroa.cs).
///
/// Kodea luzea ez izateko, klasea lau fitxategitan banatuta dago (partial class):
///   - LeihoNagusia.xaml.cs                → saioa hasi/itxi eta metodo laguntzaileak
///   - LeihoNagusia.Salmenta.cs            → produktuen katalogoa, saskia eta ticket-a
///   - LeihoNagusia.Erreserbak.cs          → mahai-planoa eta erreserbak
///   - LeihoNagusia.Administrazioa.cs      → produktuak, erabiltzaileak eta salmentak kudeatu
///
/// Zerbitzariari deitzen dioten metodoak "async" dira: erantzunaren zain
/// dagoen bitartean leihoak funtzionatzen jarraitzen du.
/// </summary>
public partial class LeihoNagusia : Window
{
    private readonly ZerbitzariBezeroa _zerbitzaria = new ZerbitzariBezeroa();

    // Saioa hasi duen erabiltzailea (null saioa itxita badago)
    private Erabiltzailea? _erabiltzailea;

    public LeihoNagusia()
    {
        InitializeComponent();
        HasieratuErreserbak();
        ErabiltzaileaKutxa.Focus();
    }

    // =====================================================================
    //  SAIO-HASIERA
    // =====================================================================

    private async void Sartu_Click(object sender, RoutedEventArgs e)
    {
        await SaioaHasi();
    }

    private async void PasahitzaKutxa_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await SaioaHasi();
        }
    }

    private async Task SaioaHasi()
    {
        string helbidea = ZerbitzariaKutxa.Text.Trim();
        string izena = ErabiltzaileaKutxa.Text.Trim();
        string pasahitza = PasahitzaKutxa.Password;

        if (helbidea == "" || izena == "" || pasahitza == "")
        {
            SaioMezua.Text = "Idatzi zerbitzaria, erabiltzailea eta pasahitza.";
            return;
        }

        SartuBotoia.IsEnabled = false;
        SaioMezua.Text = "Zerbitzarira konektatzen...";

        try
        {
            await _zerbitzaria.KonektatuAsync(helbidea);
            _erabiltzailea = await _zerbitzaria.SaioaHasiAsync(izena, pasahitza);
        }
        catch (InvalidOperationException errorea)
        {
            SaioMezua.Text = errorea.Message;
            _zerbitzaria.Deskonektatu();
            return;
        }
        finally
        {
            SartuBotoia.IsEnabled = true;
            PasahitzaKutxa.Clear();
        }

        SaioMezua.Text = "";
        ErabiltzaileTestua.Text = $"{_erabiltzailea.Izena} ({_erabiltzailea.Rola})";

        // Administrazio-fitxa administratzaileek bakarrik ikusten dute
        if (_erabiltzailea.AdministratzaileaDa())
        {
            AdministrazioFitxa.Visibility = Visibility.Visible;
        }
        else
        {
            AdministrazioFitxa.Visibility = Visibility.Collapsed;
        }

        FitxaNagusiak.SelectedIndex = 0;
        SaioHasieraPanela.Visibility = Visibility.Collapsed;
        AplikazioPanela.Visibility = Visibility.Visible;

        await KargatuProduktuak();
        await KargatuMahaiak();
        if (_erabiltzailea.AdministratzaileaDa())
        {
            await HasieratuAdministrazioa();
        }
    }

    private void SaioaItxi_Click(object sender, RoutedEventArgs e)
    {
        _erabiltzailea = null;
        _zerbitzaria.Deskonektatu();

        // Hurrengo erabiltzaileak ez dezala aurrekoaren saskia ikusi
        _saskia.Clear();
        EguneratuSaskia();
        SalmentaMezua.Text = "";
        ErreserbaMezua.Text = "";
        AmaituErreserbaAldaketa();

        AplikazioPanela.Visibility = Visibility.Collapsed;
        SaioHasieraPanela.Visibility = Visibility.Visible;
        ErabiltzaileaKutxa.Focus();
    }

    // =====================================================================
    //  METODO LAGUNTZAILEAK (fitxa guztiek erabiltzen dituzte)
    // =====================================================================

    /// <summary>
    /// Mezu bat erakusten du: berdez ondo joan bada, gorriz errorea bada.
    /// </summary>
    private static void ErakutsiMezua(TextBlock testua, string mezua, bool ondo)
    {
        testua.Text = mezua;
        testua.Foreground = ondo ? Brushes.SeaGreen : Brushes.Firebrick;
    }

    /// <summary>
    /// "#RRGGBB" moduko testu bat WPF-ko kolore (Brush) bihurtzen du.
    /// </summary>
    private static Brush Kolorea(string hex)
    {
        return (Brush)new BrushConverter().ConvertFromString(hex)!;
    }

    private static string Euroak(decimal kopurua)
    {
        return kopurua.ToString("0.00") + " €";
    }
}
