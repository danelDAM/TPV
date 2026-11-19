# Erabiltzaileak eta pasahitzak

## Aplikazioko erabiltzaileak

Datu-basea lehen aldiz sortzean (edo `Babeskopiak/elkartea_hasierakoa.db` babeskopia erabiltzean) erabiltzaile hauek daude:

| Erabiltzailea | Pasahitza  | Rola              | Zer egin dezake                                                         |
|---------------|------------|-------------------|--------------------------------------------------------------------------|
| `admin`       | `admin`    | Administratzailea | Dena: salmentak, erreserbak, produktuak/stocka eta erabiltzaileak kudeatu |
| `langilea`    | `langilea` | Langilea          | Salmentak egin eta ticket-a inprimatu; bere erreserbak egin/aldatu/ezabatu |

Pasahitzak datu-basean **SHA-256 hash** moduan gordetzen dira, ez testu arruntean.

## Datu-basea

| Datua            | Balioa                                                                  |
|------------------|-------------------------------------------------------------------------|
| Motorra          | SQLite 3                                                                |
| Fitxategia       | `TPVelkartea.Zerbitzaria\bin\...\Datuak\elkartea.db` (zerbitzariak sortzen du) |
| Erabiltzailea    | — (SQLite-k ez du erabiltzaile edo pasahitzik: fitxategi bat da)        |
| Pasahitza        | —                                                                       |
| Babeskopiak      | `Datuak\Babeskopiak\` (zerbitzaria abiarazten den bakoitzean bat)        |
| Hasierako kopia  | `Babeskopiak/elkartea_hasierakoa.db` (biltegian)                         |

SQLite datu-base batek ez du erabiltzaile-sistemarik; fitxategiaren sarbidea sistema eragileak kontrolatzen du. Datuetarako sarbide guztia **zerbitzariaren bidez** egiten da, eta zerbitzariak egiaztatzen du nork zer egin dezakeen.

## Zerbitzaria

| Datua             | Balioa      |
|-------------------|-------------|
| Protokoloa        | TCP         |
| Ataka (portua)    | `5050`      |
| Gehienezko bezeroak | 10 aldi berean |
