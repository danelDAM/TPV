using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TPVelkartea.Ereduak;

namespace TPVelkartea;

/// <summary>
/// ERRESERBAK fitxa: jantokiaren planoa marraztu eta mahaiak erreserbatu.
/// </summary>
public partial class LeihoNagusia
{
    // Jantokiko mahaiak: zenbakia, eserlekuak, X, Y, zabalera, altuera, biribila
    private static readonly Mahaia[] Mahaiak =
    {
        new Mahaia(1, 2, 290, 135, 74, 58, true),
        new Mahaia(2, 4, 485, 135, 88, 62, false),
        new Mahaia(3, 6, 685, 135, 112, 66, false),
        new Mahaia(4, 4, 290, 350, 88, 62, false),
        new Mahaia(5, 2, 485, 350, 74, 58, true),
        new Mahaia(6, 8, 685, 350, 122, 70, false)
    };

    private int _aukeratutakoMahaia = 0;   // 0 = ez dago mahairik aukeratuta

    private void HasieratuErreserbak()
    {
        TxandaKutxa.Items.Add(Erreserba.Bazkaria);
        TxandaKutxa.Items.Add(Erreserba.Afaria);
        TxandaKutxa.SelectedIndex = 0;

        // Ezin da iraganeko egun bat aukeratu
        DataAukeratzailea.DisplayDateStart = DateTime.Today;
        DataAukeratzailea.SelectedDate = DateTime.Today;
    }

    private string AukeratutakoTxanda()
    {
        if (TxandaKutxa.SelectedItem == null)
        {
            return Erreserba.Bazkaria;
        }

        return (string)TxandaKutxa.SelectedItem;
    }

    private DateTime AukeratutakoData()
    {
        if (DataAukeratzailea.SelectedDate == null)
        {
            return DateTime.Today;
        }

        return DataAukeratzailea.SelectedDate.Value;
    }

    // =====================================================================
    //  PLANOA
    // =====================================================================

    /// <summary>
    /// Planoa berriz marrazten du eta erreserben zerrenda eguneratzen du.
    /// </summary>
    private void KargatuMahaiak()
    {
        List<Erreserba> erreserbak = _datuBasea.LortuErreserbak(AukeratutakoData(), AukeratutakoTxanda());

        // Erreserbatuta dauden mahaien zenbakiak
        List<int> erreserbatuak = new List<int>();
        foreach (Erreserba erreserba in erreserbak)
        {
            erreserbatuak.Add(erreserba.MahaiZenbakia);
        }

        Planoa.Children.Clear();
        MarraztuEremua("SUKALDEA", 24, 22, 160, 68, "#E7E5E4");
        MarraztuEremua("BARRA", 24, 112, 160, 105, "#FDECC8");
        MarraztuEremua("SARRERA", 24, 438, 160, 62, "#DBEAFE");
        MarraztuEremua("LEIHOAK", 250, 22, 535, 35, "#DDF4F0");
        MarraztuTestua("Zerbitzu-pasabidea", 328, 250);

        foreach (Mahaia mahaia in Mahaiak)
        {
            MarraztuMahaia(mahaia, erreserbatuak.Contains(mahaia.Zenbakia));
        }

        ErreserbaZerrenda.Items.Clear();
        foreach (Erreserba erreserba in erreserbak)
        {
            ErreserbaZerrenda.Items.Add(erreserba);
        }
    }

    private void MarraztuEremua(string izena, double x, double y, double zabalera, double altuera, string kolorea)
    {
        Border eremua = new Border
        {
            Width = zabalera,
            Height = altuera,
            Background = Kolorea(kolorea),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(14),
            Child = new TextBlock
            {
                Text = izena,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Kolorea("#5B5349"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        Canvas.SetLeft(eremua, x);
        Canvas.SetTop(eremua, y);
        Planoa.Children.Add(eremua);
    }

    private void MarraztuTestua(string testua, double x, double y)
    {
        TextBlock etiketa = new TextBlock
        {
            Text = testua,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Kolorea("#8B8174")
        };

        Canvas.SetLeft(etiketa, x);
        Canvas.SetTop(etiketa, y);
        Planoa.Children.Add(etiketa);
    }

    private void MarraztuMahaia(Mahaia mahaia, bool erreserbatua)
    {
        // 1. Aulkiak, mahaiaren inguruan zirkuluan banatuta
        double erradioaX = mahaia.Zabalera / 2 + 19;
        double erradioaY = mahaia.Altuera / 2 + 18;

        for (int i = 0; i < mahaia.Eserlekuak; i++)
        {
            double angelua = -Math.PI / 2 + 2 * Math.PI * i / mahaia.Eserlekuak;

            Border aulkia = new Border
            {
                Width = 20,
                Height = 17,
                Background = Kolorea("#714F3A"),
                CornerRadius = new CornerRadius(6),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(angelua * 180 / Math.PI + 90)
            };

            Canvas.SetLeft(aulkia, mahaia.X + Math.Cos(angelua) * erradioaX - aulkia.Width / 2);
            Canvas.SetTop(aulkia, mahaia.Y + Math.Sin(angelua) * erradioaY - aulkia.Height / 2);
            Planoa.Children.Add(aulkia);
        }

        // 2. Mahaia bera
        bool aukeratua = mahaia.Zenbakia == _aukeratutakoMahaia;

        string atzekoKolorea = erreserbatua ? "#F8D8D6" : "#D9B889";
        string ertzKolorea = "#9C7042";
        if (erreserbatua)
        {
            ertzKolorea = "#CA5F54";
        }
        if (aukeratua)
        {
            ertzKolorea = "#5B4BDB";
        }

        Border mahaiGainazala = new Border
        {
            Width = mahaia.Zabalera,
            Height = mahaia.Altuera,
            Background = Kolorea(atzekoKolorea),
            BorderBrush = Kolorea(ertzKolorea),
            BorderThickness = new Thickness(aukeratua ? 4 : 2),
            CornerRadius = new CornerRadius(mahaia.Biribila ? 50 : 11),
            Cursor = erreserbatua ? Cursors.Arrow : Cursors.Hand,
            Tag = mahaia,
            Child = SortuMahaiTestua(mahaia, erreserbatua)
        };

        // Erreserbatuta dauden mahaiak ezin dira aukeratu
        if (!erreserbatua)
        {
            mahaiGainazala.MouseLeftButtonUp += Mahaia_Click;
        }

        Canvas.SetLeft(mahaiGainazala, mahaia.X - mahaia.Zabalera / 2);
        Canvas.SetTop(mahaiGainazala, mahaia.Y - mahaia.Altuera / 2);
        Planoa.Children.Add(mahaiGainazala);
    }

    private StackPanel SortuMahaiTestua(Mahaia mahaia, bool erreserbatua)
    {
        StackPanel testua = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        testua.Children.Add(new TextBlock
        {
            Text = mahaia.Zenbakia.ToString(),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = Kolorea("#4F3623"),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        testua.Children.Add(new TextBlock
        {
            Text = $"{mahaia.Eserlekuak} eserleku",
            FontSize = 10,
            Foreground = Kolorea("#5A4330"),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        testua.Children.Add(new TextBlock
        {
            Text = erreserbatua ? "ERRESERBATUA" : "LIBRE",
            FontSize = 8,
            FontWeight = FontWeights.Bold,
            Foreground = erreserbatua ? Brushes.DarkRed : Kolorea("#237048"),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        return testua;
    }

    // =====================================================================
    //  EKINTZAK
    // =====================================================================

    private void Mahaia_Click(object sender, MouseButtonEventArgs e)
    {
        Mahaia mahaia = (Mahaia)((Border)sender).Tag;
        _aukeratutakoMahaia = mahaia.Zenbakia;
        MahaiaTestua.Text = $"{mahaia.Zenbakia}. mahaia  ·  {mahaia.Eserlekuak} eserleku";
        ErreserbaMezua.Text = "";
        KargatuMahaiak();   // aukeratutako mahaia nabarmentzeko
    }

    private void DataEdoTxanda_Changed(object sender, SelectionChangedEventArgs e)
    {
        // Data edo txanda aldatzean, aurreko aukeraketa ez da baliagarria
        _aukeratutakoMahaia = 0;
        MahaiaTestua.Text = "Aukeratu mahai bat planoan";
        KargatuMahaiak();
    }

    private void Erreserbatu_Click(object sender, RoutedEventArgs e)
    {
        if (_aukeratutakoMahaia == 0)
        {
            ErakutsiMezua(ErreserbaMezua, "Aukeratu mahai bat planoan.", false);
            return;
        }

        if (DataAukeratzailea.SelectedDate == null)
        {
            ErakutsiMezua(ErreserbaMezua, "Aukeratu data bat.", false);
            return;
        }

        DateTime data = DataAukeratzailea.SelectedDate.Value;
        if (data < DateTime.Today)
        {
            ErakutsiMezua(ErreserbaMezua, "Ezin da iraganeko egun batean erreserbatu.", false);
            return;
        }

        string txanda = AukeratutakoTxanda();

        try
        {
            _datuBasea.GordeErreserba(_aukeratutakoMahaia, data, txanda, _erabiltzailea!.Izena);
            ErakutsiMezua(ErreserbaMezua, $"Erreserba gordeta: {_aukeratutakoMahaia}. mahaia, {data:yyyy-MM-dd}, {txanda}.", true);

            _aukeratutakoMahaia = 0;
            MahaiaTestua.Text = "Aukeratu mahai bat planoan";
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(ErreserbaMezua, errorea.Message, false);
        }

        KargatuMahaiak();
    }

    private void EzeztatuErreserba_Click(object sender, RoutedEventArgs e)
    {
        if (ErreserbaZerrenda.SelectedItem is not Erreserba erreserba)
        {
            ErakutsiMezua(ErreserbaMezua, "Aukeratu zerrendako erreserba bat.", false);
            return;
        }

        MessageBoxResult erantzuna = MessageBox.Show(
            $"{erreserba.MahaiZenbakia}. mahaiaren erreserba ezeztatu nahi duzu?",
            "Erreserba ezeztatu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (erantzuna == MessageBoxResult.Yes)
        {
            _datuBasea.EzabatuErreserba(erreserba.Id);
            ErakutsiMezua(ErreserbaMezua, "Erreserba ezeztatuta.", true);
            KargatuMahaiak();
        }
    }
}
