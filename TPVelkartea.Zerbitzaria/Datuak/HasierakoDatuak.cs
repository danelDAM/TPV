using TPVelkartea.Ereduak;

namespace TPVelkartea.Zerbitzaria.Datuak;

/// <summary>
/// Datu-basea hutsik dagoenean sartzen diren datuak (seed-a).
/// Aplikazioa lehen aldiz abiaraztean bakarrik erabiltzen da.
/// </summary>
public static class HasierakoDatuak
{
    /// <summary>
    /// Hasierako erabiltzaileak: izena, pasahitza eta rola.
    /// KONTUZ: benetako elkarte batean pasahitz hauek aldatu egin behar dira.
    /// </summary>
    public static List<string[]> Erabiltzaileak()
    {
        List<string[]> erabiltzaileak = new List<string[]>();
        erabiltzaileak.Add(new string[] { "admin", "admin", Erabiltzailea.Administratzailea });
        erabiltzaileak.Add(new string[] { "langilea", "langilea", Erabiltzailea.Langilea });
        return erabiltzaileak;
    }

    /// <summary>
    /// Hasierako produktuen katalogoa.
    /// </summary>
    public static List<Produktua> Produktuak()
    {
        List<Produktua> produktuak = new List<Produktua>();

        // --- Platerak ---
        produktuak.Add(Sortu("Patata-tortilla", Produktua.Platerak, 8.50m, 20));
        produktuak.Add(Sortu("Txuleta", Produktua.Platerak, 22.00m, 10));
        produktuak.Add(Sortu("Bakailaoa pil-pilean", Produktua.Platerak, 19.00m, 12));
        produktuak.Add(Sortu("Legatza plantxan", Produktua.Platerak, 17.50m, 14));
        produktuak.Add(Sortu("Oilasko errea", Produktua.Platerak, 14.00m, 15));
        produktuak.Add(Sortu("Etxeko hanburgesa", Produktua.Platerak, 13.00m, 20));
        produktuak.Add(Sortu("Patata brabak", Produktua.Platerak, 7.50m, 25));
        produktuak.Add(Sortu("Etxeko kroketak", Produktua.Platerak, 9.00m, 30));
        produktuak.Add(Sortu("Urdaiazpiko errazioa", Produktua.Platerak, 14.00m, 18));
        produktuak.Add(Sortu("Gazta-ohola", Produktua.Platerak, 12.00m, 16));
        produktuak.Add(Sortu("Entsalada mistoa", Produktua.Platerak, 8.00m, 20));
        produktuak.Add(Sortu("Entsalada errusiarra", Produktua.Platerak, 7.00m, 20));
        produktuak.Add(Sortu("Antxoak", Produktua.Platerak, 9.50m, 18));
        produktuak.Add(Sortu("Olibak", Produktua.Platerak, 3.00m, 40));
        produktuak.Add(Sortu("Ogi artisaua", Produktua.Platerak, 1.20m, 100));

        // --- Edariak ---
        produktuak.Add(Sortu("Garagardoa (kaña)", Produktua.Edariak, 2.20m, 120));
        produktuak.Add(Sortu("Zuritoa", Produktua.Edariak, 1.50m, 100));
        produktuak.Add(Sortu("Garagardo botila", Produktua.Edariak, 2.80m, 72));
        produktuak.Add(Sortu("Alkoholik gabeko garagardoa", Produktua.Edariak, 2.40m, 48));
        produktuak.Add(Sortu("IPA garagardo artisaua", Produktua.Edariak, 4.20m, 24));
        produktuak.Add(Sortu("Sagardo naturala (botila)", Produktua.Edariak, 3.50m, 60));
        produktuak.Add(Sortu("Sagardo naturala (pitxerra)", Produktua.Edariak, 6.00m, 30));
        produktuak.Add(Sortu("Txakolina (kopa)", Produktua.Edariak, 3.50m, 36));
        produktuak.Add(Sortu("Ardo beltza, kriantza (kopa)", Produktua.Edariak, 3.20m, 50));
        produktuak.Add(Sortu("Ardo beltza, kriantza (botila)", Produktua.Edariak, 18.00m, 18));
        produktuak.Add(Sortu("Ardo zuria (kopa)", Produktua.Edariak, 3.00m, 42));
        produktuak.Add(Sortu("Ardo zuria (botila)", Produktua.Edariak, 16.00m, 16));
        produktuak.Add(Sortu("Kaba (kopa)", Produktua.Edariak, 4.50m, 24));
        produktuak.Add(Sortu("Patxarana", Produktua.Edariak, 4.50m, 24));
        produktuak.Add(Sortu("Belar-likorea", Produktua.Edariak, 4.50m, 24));
        produktuak.Add(Sortu("Kola freskagarria", Produktua.Edariak, 2.50m, 60));
        produktuak.Add(Sortu("Laranja freskagarria", Produktua.Edariak, 2.50m, 36));
        produktuak.Add(Sortu("Limoi freskagarria", Produktua.Edariak, 2.50m, 36));
        produktuak.Add(Sortu("Laranja-zukua", Produktua.Edariak, 2.80m, 30));
        produktuak.Add(Sortu("Ur minerala", Produktua.Edariak, 1.80m, 80));
        produktuak.Add(Sortu("Ur gasduna", Produktua.Edariak, 2.00m, 30));
        produktuak.Add(Sortu("Kafea", Produktua.Edariak, 1.50m, 100));
        produktuak.Add(Sortu("Kafesnea", Produktua.Edariak, 1.80m, 80));
        produktuak.Add(Sortu("Tea eta infusioak", Produktua.Edariak, 1.60m, 40));

        // --- Postreak ---
        produktuak.Add(Sortu("Gazta-tarta", Produktua.Postreak, 5.50m, 18));
        produktuak.Add(Sortu("Etxeko flana", Produktua.Postreak, 4.50m, 16));
        produktuak.Add(Sortu("Mamia", Produktua.Postreak, 4.50m, 15));
        produktuak.Add(Sortu("Izozkia", Produktua.Postreak, 4.00m, 24));
        produktuak.Add(Sortu("Txokolatezko brownie-a", Produktua.Postreak, 5.00m, 16));
        produktuak.Add(Sortu("Gazta Idiazabal irasagarrarekin", Produktua.Postreak, 5.50m, 12));

        return produktuak;
    }

    private static Produktua Sortu(string izena, string kategoria, decimal prezioa, int stocka)
    {
        Produktua produktua = new Produktua();
        produktua.Izena = izena;
        produktua.Kategoria = kategoria;
        produktua.Prezioa = prezioa;
        produktua.Stocka = stocka;
        return produktua;
    }
}
