using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TPVelkartea.Ereduak;

namespace TPVelkartea;

/// <summary>
/// ADMINISTRAZIOA fitxa: produktuak eta biltegiko stocka, erabiltzaileak
/// eta salmenten historia. Administratzaileek bakarrik ikusten dute.
/// (Zerbitzariak ere egiaztatzen du administratzailea dela.)
/// </summary>
public partial class LeihoNagusia
{
    private async Task HasieratuAdministrazioa()
    {
        ProduktuKategoriaKutxa.ItemsSource = Produktua.Kategoriak;
        ErabiltzaileRolaKutxa.ItemsSource = Erabiltzailea.Rolak;

        GarbituProduktuFormularioa();
        GarbituErabiltzaileFormularioa();
        await KargatuAdministrazioa();
    }

    private async Task KargatuAdministrazioa()
    {
        try
        {
            ProduktuTaula.ItemsSource = await _zerbitzaria.LortuProduktuakAsync();
            ErabiltzaileTaula.ItemsSource = await _zerbitzaria.LortuErabiltzaileakAsync();

            List<Salmenta> salmentak = await _zerbitzaria.LortuSalmentakAsync();
            SalmentaTaula.ItemsSource = salmentak;

            // Gaurko salmenten laburpena
            string gaur = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ProduktuMezua, errorea.Message, false);
        }
    }

    // =====================================================================
    //  PRODUKTUAK ETA STOCKA
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
        StockGehitzeaKutxa.Text = "";
        ProduktuMezua.Text = "";
    }

    private async void GordeProduktua_Click(object sender, RoutedEventArgs e)
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

        try
        {
            await _zerbitzaria.GordeProduktuaAsync(produktua);
            GarbituProduktuFormularioa();
            ErakutsiMezua(ProduktuMezua, $"\"{izena}\" gordeta.", true);
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ProduktuMezua, errorea.Message, false);
        }

        await KargatuAdministrazioa();
        await KargatuProduktuak();
    }

    /// <summary>
    /// Biltegira unitate berriak sartu: hautatutako produktuaren stockari kopurua gehitzen dio.
    /// Batuketa zerbitzarian egiten da (Stocka = Stocka + kopurua), beraz bi
    /// administratzailek aldi berean gehitzen badute ere, ez da ezer galtzen.
    /// </summary>
    private async void GehituStocka_Click(object sender, RoutedEventArgs e)
    {
        if (ProduktuTaula.SelectedItem is not Produktua produktua)
        {
            ErakutsiMezua(ProduktuMezua, "Aukeratu taulako produktu bat.", false);
            return;
        }

        if (!int.TryParse(StockGehitzeaKutxa.Text, out int kopurua) || kopurua <= 0)
        {
            ErakutsiMezua(ProduktuMezua, "Idatzi gehitu nahi den unitate kopurua (0 baino handiagoa).", false);
            return;
        }

        try
        {
            await _zerbitzaria.GehituStockaAsync(produktua.Id, kopurua);
            GarbituProduktuFormularioa();
            ErakutsiMezua(ProduktuMezua, $"{kopurua} unitate gehituta: {produktua.Izena}.", true);
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ProduktuMezua, errorea.Message, false);
        }

        await KargatuAdministrazioa();
        await KargatuProduktuak();
    }

    private async void EzabatuProduktua_Click(object sender, RoutedEventArgs e)
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

        if (erantzuna != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _zerbitzaria.EzabatuProduktuaAsync(produktua.Id);
            GarbituProduktuFormularioa();
            ErakutsiMezua(ProduktuMezua, "Produktua ezabatuta.", true);
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ProduktuMezua, errorea.Message, false);
        }

        await KargatuAdministrazioa();
        await KargatuProduktuak();
    }

    /// <summary>
    /// Prezioa irakurtzen du, komarekin (8,50) edo puntuarekin (8.50) idatzita egon arren.
    /// </summary>
    private static bool IrakurriPrezioa(string testua, out decimal prezioa)
    {
        string normalizatua = testua.Trim().Replace(',', '.');
        bool zuzena = decimal.TryParse(normalizatua, NumberStyles.Number, CultureInfo.InvariantCulture, out prezioa);

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

    private async void GordeErabiltzailea_Click(object sender, RoutedEventArgs e)
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

        Erabiltzailea erabiltzailea = new Erabiltzailea();
        if (hautatua != null)
        {
            erabiltzailea.Id = hautatua.Id;
        }
        erabiltzailea.Izena = izena;
        erabiltzailea.Rola = (string)ErabiltzaileRolaKutxa.SelectedItem;

        try
        {
            await _zerbitzaria.GordeErabiltzaileaAsync(erabiltzailea, pasahitza);
            GarbituErabiltzaileFormularioa();
            ErakutsiMezua(ErabiltzaileMezua, $"\"{izena}\" gordeta.", true);
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ErabiltzaileMezua, errorea.Message, false);
        }

        await KargatuAdministrazioa();
    }

    private async void EzabatuErabiltzailea_Click(object sender, RoutedEventArgs e)
    {
        if (ErabiltzaileTaula.SelectedItem is not Erabiltzailea erabiltzailea)
        {
            ErakutsiMezua(ErabiltzaileMezua, "Aukeratu taulako erabiltzaile bat.", false);
            return;
        }

        MessageBoxResult erantzuna = MessageBox.Show(
            $"\"{erabiltzailea.Izena}\" erabiltzailea ezabatu nahi duzu?",
            "Erabiltzailea ezabatu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (erantzuna != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _zerbitzaria.EzabatuErabiltzaileaAsync(erabiltzailea.Id);
            GarbituErabiltzaileFormularioa();
            ErakutsiMezua(ErabiltzaileMezua, "Erabiltzailea ezabatuta.", true);
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ErabiltzaileMezua, errorea.Message, false);
        }

        await KargatuAdministrazioa();
    }
}
