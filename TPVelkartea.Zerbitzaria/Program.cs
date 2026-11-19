using System.Text;
using TPVelkartea.Protokoloa;
using TPVelkartea.Zerbitzaria;
using TPVelkartea.Zerbitzaria.Datuak;

// =====================================================================
//  TPV ELKARTEA - ZERBITZARIA
//  Abiarazi hau lehenengo, eta gero bezeroak (TPVelkartea aplikazioa).
// =====================================================================

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "TPV Elkartea - Zerbitzaria";

// 1. Datu-basea ireki (lehen aldian sortu eta hasierako datuak sartzen dira)
string fitxategia = Path.Combine(AppContext.BaseDirectory, "Datuak", "elkartea.db");
DatuBasea datuBasea = new DatuBasea(fitxategia);
Zerbitzaria.Idatzi("Datu-basea: " + fitxategia);

// 2. Abiaraztean, babeskopia bat egin
Zerbitzaria.Idatzi("Babeskopia eginda: " + datuBasea.EginBabeskopia());

// 3. Bezeroen zain geratu (programa ez da hemendik irteten)
Zerbitzaria zerbitzaria = new Zerbitzaria(datuBasea, Konfigurazioa.Ataka);
zerbitzaria.Abiarazi();
