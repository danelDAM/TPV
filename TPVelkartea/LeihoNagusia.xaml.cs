using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TPVelkartea.Datuak;
using TPVelkartea.Ereduak;

namespace TPVelkartea;

/// <summary>
/// Aplikazioaren leiho nagusia.
/// Kodea luzea ez izateko, klasea lau fitxategitan banatuta dago (partial class):
///   - LeihoNagusia.xaml.cs                → saioa hasi/itxi eta metodo laguntzaileak
///   - LeihoNagusia.Salmenta.cs            → produktuen katalogoa eta saskia
///   - LeihoNagusia.Erreserbak.cs          → mahai-planoa eta erreserbak
///   - LeihoNagusia.Administrazioa.cs      → produktuak, erabiltzaileak eta salmentak kudeatu
/// </summary>
public partial class LeihoNagusia : Window
{
    private readonly DatuBasea _datuBasea = new DatuBasea();

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

    private void Sartu_Click(object sender, RoutedEventArgs e)
    {
        SaioaHasi();
    }

    private void PasahitzaKutxa_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SaioaHasi();
        }
    }

    private void SaioaHasi()
    {
        string izena = ErabiltzaileaKutxa.Text.Trim();
        string pasahitza = PasahitzaKutxa.Password;

        if (izena == "" || pasahitza == "")
        {
            SaioMezua.Text = "Idatzi erabiltzailea eta pasahitza.";
            return;
        }

        _erabiltzailea = _datuBasea.SaioaHasi(izena, pasahitza);
        PasahitzaKutxa.Clear();

        if (_erabiltzailea == null)
        {
            SaioMezua.Text = "Erabiltzailea edo pasahitza ez da zuzena.";
            return;
        }

        SaioMezua.Text = "";
        ErabiltzaileTestua.Text = $"{_erabiltzailea.Izena} ({_erabiltzailea.Rola})";

        // Administrazio-fitxa administratzaileek bakarrik ikusten dute
        if (_erabiltzailea.AdministratzaileaDa())
        {
            AdministrazioFitxa.Visibility = Visibility.Visible;
            HasieratuAdministrazioa();
        }
        else
        {
            AdministrazioFitxa.Visibility = Visibility.Collapsed;
        }

        FitxaNagusiak.SelectedIndex = 0;
        SaioHasieraPanela.Visibility = Visibility.Collapsed;
        AplikazioPanela.Visibility = Visibility.Visible;

        KargatuProduktuak();
        KargatuMahaiak();
    }

    private void SaioaItxi_Click(object sender, RoutedEventArgs e)
    {
        _erabiltzailea = null;

        // Hurrengo erabiltzaileak ez dezala aurrekoaren saskia ikusi
        _saskia.Clear();
        EguneratuSaskia();
        SalmentaMezua.Text = "";
        ErreserbaMezua.Text = "";

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
