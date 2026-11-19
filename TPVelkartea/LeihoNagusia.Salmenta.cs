using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TPVelkartea.Ereduak;

namespace TPVelkartea;

/// <summary>
/// SALMENTA fitxa: produktuen katalogoa, kopuru-teklatua eta saskia (ticket-a).
/// </summary>
public partial class LeihoNagusia
{
    private readonly List<SaskiLerroa> _saskia = new List<SaskiLerroa>();
    private List<Produktua> _produktuak = new List<Produktua>();   // zerbitzaritik jasotako katalogoa

    private string _kategoria = "";       // "" = kategoria guztiak
    private string _kopuruaTestua = "";   // teklatuarekin idatzitako zenbakia

    // =====================================================================
    //  PRODUKTUEN KATALOGOA
    // =====================================================================

    /// <summary>
    /// Produktuak zerbitzaritik kargatu eta katalogoa erakutsi.
    /// </summary>
    private async Task KargatuProduktuak()
    {
        try
        {
            _produktuak = await _zerbitzaria.LortuProduktuakAsync();
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(SalmentaMezua, errorea.Message, false);
        }

        ErakutsiProduktuak();
    }

    /// <summary>
    /// Memorian dauden produktuak iragazi (kategoria + bilaketa) eta txartelak marraztu.
    /// Ez dio zerbitzariari deitzen, beraz azkarra da.
    /// </summary>
    private void ErakutsiProduktuak()
    {
        ProduktuPanela.Children.Clear();
        string bilaketa = BilaketaKutxa.Text.Trim().ToLower();

        foreach (Produktua produktua in _produktuak)
        {
            bool kategoriaZuzena = _kategoria == "" || produktua.Kategoria == _kategoria;
            bool bilaketaZuzena = produktua.Izena.ToLower().Contains(bilaketa);

            if (kategoriaZuzena && bilaketaZuzena)
            {
                ProduktuPanela.Children.Add(SortuProduktuTxartela(produktua));
            }
        }

        if (ProduktuPanela.Children.Count == 0)
        {
            ProduktuPanela.Children.Add(new TextBlock
            {
                Text = "Ez da produkturik aurkitu.",
                Foreground = Brushes.Gray,
                Margin = new Thickness(4)
            });
        }
    }

    private Border SortuProduktuTxartela(Produktua produktua)
    {
        StackPanel edukia = new StackPanel();

        // Ikonoa
        edukia.Children.Add(new Border
        {
            Height = 80,
            CornerRadius = new CornerRadius(12),
            Background = Kolorea("#EEECFF"),
            Child = new TextBlock
            {
                Text = Ikonoa(produktua.Kategoria),
                FontSize = 38,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        // Izena
        edukia.Children.Add(new TextBlock
        {
            Text = produktua.Izena,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = Kolorea("#172033"),
            TextWrapping = TextWrapping.Wrap,
            Height = 40,
            Margin = new Thickness(0, 10, 0, 4)
        });

        // Prezioa (ezkerrean) eta stocka (eskuinean)
        DockPanel informazioa = new DockPanel();
        TextBlock stocka = new TextBlock
        {
            Text = $"Stocka: {produktua.Stocka}",
            Foreground = produktua.Stocka < 5 ? Brushes.Firebrick : Kolorea("#6B7280")
        };
        DockPanel.SetDock(stocka, Dock.Right);
        informazioa.Children.Add(stocka);
        informazioa.Children.Add(new TextBlock
        {
            Text = Euroak(produktua.Prezioa),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Kolorea("#5B4BDB")
        });
        edukia.Children.Add(informazioa);

        // Gehitu botoia
        Button gehitu = new Button
        {
            Content = produktua.Stocka > 0 ? "GEHITU  +" : "AGORTUTA",
            IsEnabled = produktua.Stocka > 0,
            Tag = produktua,
            Height = 34,
            Margin = new Thickness(0, 10, 0, 0)
        };
        gehitu.Click += Gehitu_Click;
        edukia.Children.Add(gehitu);

        return new Border
        {
            Width = 190,
            Margin = new Thickness(0, 0, 14, 14),
            Padding = new Thickness(12),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(16),
            Child = edukia
        };
    }

    private static string Ikonoa(string kategoria)
    {
        if (kategoria == Produktua.Edariak)
        {
            return "🍺";
        }

        if (kategoria == Produktua.Postreak)
        {
            return "🍰";
        }

        return "🍽";
    }

    private void Kategoria_Click(object sender, RoutedEventArgs e)
    {
        Button sakatutakoa = (Button)sender;
        _kategoria = (string)sakatutakoa.Tag;
        KategoriaTestua.Text = _kategoria == "" ? "Produktu guztiak" : _kategoria;

        // Botoi guztiak kolore arruntean jarri, eta sakatutakoa nabarmendu
        foreach (object elementua in KategoriaPanela.Children)
        {
            if (elementua is Button botoia)
            {
                botoia.Background = Kolorea("#E9E7FF");
                botoia.Foreground = Kolorea("#40369C");
            }
        }
        sakatutakoa.Background = Kolorea("#5B4BDB");
        sakatutakoa.Foreground = Brushes.White;

        ErakutsiProduktuak();
    }

    private void Bilaketa_TextChanged(object sender, TextChangedEventArgs e)
    {
        ErakutsiProduktuak();
    }

    // =====================================================================
    //  KOPURU-TEKLATUA
    // =====================================================================

    private void Tekla_Click(object sender, RoutedEventArgs e)
    {
        string tekla = (string)((Button)sender).Content;

        if (tekla == "C")
        {
            _kopuruaTestua = "";
        }
        else if (tekla == "⌫")
        {
            if (_kopuruaTestua.Length > 0)
            {
                _kopuruaTestua = _kopuruaTestua.Substring(0, _kopuruaTestua.Length - 1);
            }
        }
        else if (_kopuruaTestua.Length < 2)   // gehienez 99 unitate
        {
            _kopuruaTestua += tekla;
        }

        KopuruaTestua.Text = LortuKopurua().ToString();
    }

    /// <summary>
    /// Teklatuan idatzitako kopurua. Ezer idatzi ez bada (edo 0 bada), 1.
    /// </summary>
    private int LortuKopurua()
    {
        if (int.TryParse(_kopuruaTestua, out int kopurua) && kopurua > 0)
        {
            return kopurua;
        }

        return 1;
    }

    // =====================================================================
    //  SASKIA
    // =====================================================================

    private void Gehitu_Click(object sender, RoutedEventArgs e)
    {
        Produktua produktua = (Produktua)((Button)sender).Tag;
        int kopurua = LortuKopurua();

        SaskiLerroa? lerroa = BilatuSaskiLerroa(produktua.Id);
        int saskianDagoena = lerroa == null ? 0 : lerroa.Kopurua;

        if (saskianDagoena + kopurua > produktua.Stocka)
        {
            ErakutsiMezua(SalmentaMezua, $"Ez dago nahikoa stock: {produktua.Izena} ({produktua.Stocka} geratzen dira).", false);
            return;
        }

        if (lerroa == null)
        {
            _saskia.Add(new SaskiLerroa(produktua, kopurua));
        }
        else
        {
            lerroa.Kopurua += kopurua;
        }

        // Kopurua berriz 1era
        _kopuruaTestua = "";
        KopuruaTestua.Text = "1";
        SalmentaMezua.Text = "";
        EguneratuSaskia();
    }

    private SaskiLerroa? BilatuSaskiLerroa(int produktuId)
    {
        foreach (SaskiLerroa lerroa in _saskia)
        {
            if (lerroa.Produktua.Id == produktuId)
            {
                return lerroa;
            }
        }

        return null;
    }

    private decimal SaskiarenGuztira()
    {
        decimal guztira = 0;
        foreach (SaskiLerroa lerroa in _saskia)
        {
            guztira += lerroa.Guztira;
        }
        return guztira;
    }

    private void EguneratuSaskia()
    {
        SaskiaZerrenda.Items.Clear();
        foreach (SaskiLerroa lerroa in _saskia)
        {
            SaskiaZerrenda.Items.Add(lerroa);
        }

        GuztiraTestua.Text = Euroak(SaskiarenGuztira());
    }

    private void KenduLerroa_Click(object sender, RoutedEventArgs e)
    {
        if (SaskiaZerrenda.SelectedItem is SaskiLerroa lerroa)
        {
            _saskia.Remove(lerroa);
            EguneratuSaskia();
            SalmentaMezua.Text = "";
        }
        else
        {
            ErakutsiMezua(SalmentaMezua, "Aukeratu ticket-eko lerro bat kentzeko.", false);
        }
    }

    private void HustuSaskia_Click(object sender, RoutedEventArgs e)
    {
        _saskia.Clear();
        EguneratuSaskia();
        SalmentaMezua.Text = "";
    }

    /// <summary>
    /// Erosketa baieztatu: zerbitzariak salmenta gorde eta stocka jaisten du.
    /// Ondoren ticket-a erakusten da, inprimatu ahal izateko.
    /// </summary>
    private async void Ordaindu_Click(object sender, RoutedEventArgs e)
    {
        if (_saskia.Count == 0)
        {
            ErakutsiMezua(SalmentaMezua, "Saskia hutsik dago.", false);
            return;
        }

        MessageBoxResult erantzuna = MessageBox.Show(
            $"Erosketa baieztatu?\n\nProduktu kopurua: {_saskia.Count}\nGuztira: {Euroak(SaskiarenGuztira())}",
            "Ordaindu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (erantzuna != MessageBoxResult.Yes)
        {
            return;
        }

        OrdainduBotoia.IsEnabled = false;
        try
        {
            Salmenta salmenta = await _zerbitzaria.GordeSalmentaAsync(_saskia);

            ErakutsiMezua(SalmentaMezua, $"Ordainketa eginda. Ticket zk. {salmenta.Id}  ·  Guztira: {Euroak(salmenta.Guztira)}", true);
            _saskia.Clear();
            EguneratuSaskia();

            // Ticket-a erakutsi (eta, nahi bada, inprimatu)
            TicketLeihoa ticketa = new TicketLeihoa(salmenta);
            ticketa.Owner = this;
            ticketa.ShowDialog();
        }
        catch (InvalidOperationException errorea)
        {
            ErakutsiMezua(SalmentaMezua, errorea.Message, false);
        }
        finally
        {
            OrdainduBotoia.IsEnabled = true;
        }

        // Stocka aldatu denez, katalogoa (eta administrazioa) berriz kargatu
        await KargatuProduktuak();
        if (_erabiltzailea != null && _erabiltzailea.AdministratzaileaDa())
        {
            await KargatuAdministrazioa();
        }
    }
}
