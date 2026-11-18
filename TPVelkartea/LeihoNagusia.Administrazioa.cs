using System.Windows;
using System.Windows.Controls;
using TPVelkartea.Ereduak;

namespace TPVelkartea;

/// <summary>
/// ADMINISTRAZIOA fitxa: produktuak, erabiltzaileak eta salmenten historia.
/// Administratzaileek bakarrik ikusten dute.
/// </summary>
public partial class LeihoNagusia
{
    private void HasieratuAdministrazioa()
    {
        ProduktuKategoriaKutxa.ItemsSource = Produktua.Kategoriak;
        ErabiltzaileRolaKutxa.ItemsSource = Erabiltzailea.Rolak;

        GarbituProduktuFormularioa();
        GarbituErabiltzaileFormularioa();
        KargatuAdministrazioa();
    }

    private void KargatuAdministrazioa()
    {
        ProduktuTaula.ItemsSource = _datuBasea.LortuProduktuak();
        ErabiltzaileTaula.ItemsSource = _datuBasea.LortuErabiltzaileak();

        List<Salmenta> salmentak = _datuBasea.LortuSalmentak();
        SalmentaTaula.ItemsSource = salmentak;

        // Gaurko salmenten laburpena
        string gaur = DateTime.Today.ToString("yyyy-MM-dd");
        int gaurkoKopurua = 0;
        decimal gaurkoGuztira = 0;

        foreach (Salmenta salmenta in salmentak)
        {
            if (salmenta.DataOrdua.StartsWith(gaur))
            {
                gaurkoKopurua++;
                gaurkoGuztira += salmenta.Guztira;
            }
        }

        GaurkoSalmentakTestua.Text = $"Gaur: {gaurkoKopurua} salmenta  ·  {Euroak(gaurkoGuztira)}";
    }

    // =====================================================================
    //  PRODUKTUAK
    // =====================================================================

    private void ProduktuTaula_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProduktuTaula.SelectedItem is Produktua produktua)
        {
            ProduktuIzenaKutxa.Text = produktua.Izena;
            ProduktuKategoriaKutxa.SelectedItem = produktua.Kategoria;
            ProduktuPrezioaKutxa.Text = produktua.Prezioa.ToString("0.00");
            ProduktuStockaKutxa.Text = produktua.Stocka.ToString();
            ProduktuMezua.Text = "";
        }
    }

    private void ProduktuBerria_Click(object sender, RoutedEventArgs e)
    {
        GarbituProduktuFormularioa();
        ProduktuIzenaKutxa.Focus();
    }

    private void GarbituProduktuFormularioa()
    {
        ProduktuTaula.SelectedItem = null;
        ProduktuIzenaKutxa.Text = "";
        ProduktuKategoriaKutxa.SelectedIndex = 0;
        ProduktuPrezioaKutxa.Text = "";
        ProduktuStockaKutxa.Text = "";
        ProduktuMezua.Text = "";
    }

    private void GordeProduktua_Click(object sender, RoutedEventArgs e)
    {
        string izena = ProduktuIzenaKutxa.Text.Trim();

        if (izena == "")
        {
            ErakutsiMezua(ProduktuMezua, "Idatzi produktuaren izena.", false);
            return;
        }

        if (ProduktuKategoriaKutxa.SelectedItem == null)
        {
            ErakutsiMezua(ProduktuMezua, "Aukeratu kategoria bat.", false);
            return;
        }

        if (!IrakurriPrezioa(ProduktuPrezioaKutxa.Text, out decimal prezioa))
        {
            ErakutsiMezua(ProduktuMezua, "Prezioa ez da zuzena (adibidez: 8,50).", false);
            return;
        }

        if (!int.TryParse(ProduktuStockaKutxa.Text, out int stocka) || stocka < 0)
        {
            ErakutsiMezua(ProduktuMezua, "Stocka 0 edo zenbaki positibo bat izan behar da.", false);
            return;
        }

        Produktua produktua = new Produktua();
        if (ProduktuTaula.SelectedItem is Produktua hautatua)
        {
            produktua.Id = hautatua.Id;   // dagoena aldatu
        }
        produktua.Izena = izena;
        produktua.Kategoria = (string)ProduktuKategoriaKutxa.SelectedItem;
        produktua.Prezioa = prezioa;
        produktua.Stocka = stocka;

        _datuBasea.GordeProduktua(produktua);

        KargatuAdministrazioa();
        KargatuProduktuak();
        GarbituProduktuFormularioa();
        ErakutsiMezua(ProduktuMezua, $"\"{izena}\" gordeta.", true);
    }

    private void EzabatuProduktua_Click(object sender, RoutedEventArgs e)
    {
        if (ProduktuTaula.SelectedItem is not Produktua produktua)
        {
            ErakutsiMezua(ProduktuMezua, "Aukeratu taulako produktu bat.", false);
            return;
        }

        MessageBoxResult erantzuna = MessageBox.Show(
            $"\"{produktua.Izena}\" ezabatu nahi duzu?",
            "Produktua ezabatu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (erantzuna == MessageBoxResult.Yes)
        {
            _datuBasea.EzabatuProduktua(produktua.Id);
            KargatuAdministrazioa();
            KargatuProduktuak();
            GarbituProduktuFormularioa();
            ErakutsiMezua(ProduktuMezua, "Produktua ezabatuta.", true);
        }
    }

    /// <summary>
    /// Prezioa irakurtzen du, komarekin (8,50) edo puntuarekin (8.50) idatzita egon arren.
    /// </summary>
    private static bool IrakurriPrezioa(string testua, out decimal prezioa)
    {
        string normalizatua = testua.Trim().Replace(',', '.');
        bool zuzena = decimal.TryParse(normalizatua, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out prezioa);

        return zuzena && prezioa >= 0;
    }

    // =====================================================================
    //  ERABILTZAILEAK
    // =====================================================================

    private void ErabiltzaileTaula_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ErabiltzaileTaula.SelectedItem is Erabiltzailea erabiltzailea)
        {
            ErabiltzaileIzenaKutxa.Text = erabiltzailea.Izena;
            ErabiltzailePasahitzaKutxa.Clear();
            ErabiltzaileRolaKutxa.SelectedItem = erabiltzailea.Rola;
            ErabiltzaileMezua.Text = "";
        }
    }

    private void ErabiltzaileBerria_Click(object sender, RoutedEventArgs e)
    {
        GarbituErabiltzaileFormularioa();
        ErabiltzaileIzenaKutxa.Focus();
    }

    private void GarbituErabiltzaileFormularioa()
    {
        ErabiltzaileTaula.SelectedItem = null;
        ErabiltzaileIzenaKutxa.Text = "";
        ErabiltzailePasahitzaKutxa.Clear();
        ErabiltzaileRolaKutxa.SelectedItem = Erabiltzailea.Langilea;
        ErabiltzaileMezua.Text = "";
    }

    private void GordeErabiltzailea_Click(object sender, RoutedEventArgs e)
    {
        string izena = ErabiltzaileIzenaKutxa.Text.Trim();
        string pasahitza = ErabiltzailePasahitzaKutxa.Password;
        Erabiltzailea? hautatua = ErabiltzaileTaula.SelectedItem as Erabiltzailea;

        if (izena == "")
        {
            ErakutsiMezua(ErabiltzaileMezua, "Idatzi erabiltzailearen izena.", false);
            return;
        }

        // Erabiltzaile berriak pasahitza behar du derrigorrez
        if (hautatua == null && pasahitza == "")
        {
            ErakutsiMezua(ErabiltzaileMezua, "Erabiltzaile berriak pasahitza behar du.", false);
            return;
        }

        if (ErabiltzaileRolaKutxa.SelectedItem == null)
        {
            ErakutsiMezua(ErabiltzaileMezua, "Aukeratu rol bat.", false);
            return;
        }

        string rola = (string)ErabiltzaileRolaKutxa.SelectedItem;

        // Administratzaileak ezin dio bere buruari rola kendu
        if (hautatua != null && hautatua.Id == _erabiltzailea!.Id && rola != Erabiltzailea.Administratzailea)
        {
            ErakutsiMezua(ErabiltzaileMezua, "Ezin duzu zure administratzaile-rola kendu.", false);
            return;
        }

        Erabiltzailea erabiltzailea = new Erabiltzailea();
        if (hautatua != null)
        {
            erabiltzailea.Id = hautatua.Id;
        }
        erabiltzailea.Izena = izena;
        erabiltzailea.Rola = rola;

        try
        {
            _datuBasea.GordeErabiltzailea(erabiltzailea, pasahitza);
            KargatuAdministrazioa();
            GarbituErabiltzaileFormularioa();
            ErakutsiMezua(ErabiltzaileMezua, $"\"{izena}\" gordeta.", true);
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ErabiltzaileMezua, errorea.Message, false);
        }
    }

    private void EzabatuErabiltzailea_Click(object sender, RoutedEventArgs e)
    {
        if (ErabiltzaileTaula.SelectedItem is not Erabiltzailea erabiltzailea)
        {
            ErakutsiMezua(ErabiltzaileMezua, "Aukeratu taulako erabiltzaile bat.", false);
            return;
        }

        if (erabiltzailea.Id == _erabiltzailea!.Id)
        {
            ErakutsiMezua(ErabiltzaileMezua, "Ezin duzu zure burua ezabatu.", false);
            return;
        }

        MessageBoxResult erantzuna = MessageBox.Show(
            $"\"{erabiltzailea.Izena}\" erabiltzailea ezabatu nahi duzu?",
            "Erabiltzailea ezabatu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (erantzuna == MessageBoxResult.Yes)
        {
            _datuBasea.EzabatuErabiltzailea(erabiltzailea.Id);
            KargatuAdministrazioa();
            GarbituErabiltzaileFormularioa();
            ErakutsiMezua(ErabiltzaileMezua, "Erabiltzailea ezabatuta.", true);
        }
    }
}
