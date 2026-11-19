# TPV Elkartea

Elkarte gastronomiko baterako **salmenta-puntuko terminala (TPV)**, mahaigaineko ordenagailuetan instalatzeko.
**Bezero-zerbitzari** arkitektura du: zerbitzari batek SQLite datu-basea kudeatzen du, eta hainbat bezerok (gehienez 10 aldi berean) TCP bidez konektatzen dira.

- 🛒 **Salmentak**: produktuak ikus-osagaien bidez aukeratu, ticket-a ikusi eta inprimatu; stocka automatikoki jaisten da.
- 📅 **Erreserbak**: jantokiaren planoan mahai bat aukeratu eta erreserbatu (data zehaztuta edo data gabe, bazkaria edo afaria); aldatu eta ezabatu.
- ⚙ **Administrazioa**: produktu berriak, biltegiko stocka (gehitu/aldatu/ezabatu), erabiltzaileak (sortu/aldatu/ezabatu) eta salmenten historia.

## Proiektuak

| Proiektua                | Zer da                                                                 |
|--------------------------|------------------------------------------------------------------------|
| `TPVelkartea.Zerbitzaria` | Kontsola-aplikazioa. TCP zerbitzaria + SQLite datu-basea.              |
| `TPVelkartea`             | WPF bezeroa (leihoak). Zerbitzariarekin TCP bidez hitz egiten du.      |
| `TPVelkartea.Komuna`      | Biek partekatzen dutena: ereduak (Produktua, Erreserba...) eta protokoloa. |

## Martxan jartzea

Behar dena: **Windows** eta **.NET 10 SDK** (`dotnet --list-sdks`).

**1. Zerbitzaria abiarazi** (lehenengo, eta irekita utzi):

```powershell
dotnet run --project .\TPVelkartea.Zerbitzaria\TPVelkartea.Zerbitzaria.csproj
```

Lehen aldian datu-basea sortzen du hasierako datuekin, eta abiarazten den bakoitzean babeskopia bat egiten du.

**2. Bezeroa abiarazi** (nahi adina, gehienez 10 aldi berean):

```powershell
dotnet run --project .\TPVelkartea\TPVelkartea.csproj
```

Saio-hasieran idatzi zerbitzariaren IP helbidea (ordenagailu berean bada: `127.0.0.1`).

Visual Studion: ireki `TPVelkartea.slnx`, eskuin-klika soluzioan → *Configure Startup Projects* → *Multiple startup projects* → `TPVelkartea.Zerbitzaria` eta `TPVelkartea` biak **Start**.

## Erabiltzaileak

| Erabiltzailea | Pasahitza  | Rola              |
|---------------|------------|-------------------|
| `admin`       | `admin`    | Administratzailea |
| `langilea`    | `langilea` | Langilea          |

Xehetasun gehiago (datu-basea, babeskopiak): [ERABILTZAILEAK.md](ERABILTZAILEAK.md).
Datu-basearen hasierako babeskopia: [Babeskopiak/elkartea_hasierakoa.db](Babeskopiak/elkartea_hasierakoa.db).

## Dokumentazioa

Dokumentazio osoa euskaraz (gida teknikoa + administratzaile eta erabiltzaileentzako gida): **[docs/DOKUMENTAZIOA.md](docs/DOKUMENTAZIOA.md)**
