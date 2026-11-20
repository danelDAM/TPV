using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TPVelkartea.Ereduak;
using TPVelkartea.Protokoloa;
using TPVelkartea.Zerbitzaria.Datuak;

namespace TPVelkartea.Zerbitzaria;

public class BezeroKonexioa
{
    // ================================================================
    //  DATOS DE LA CONEXIÓN
    // ================================================================

    // Cliente TCP al que estamos atendiendo.
    private readonly TcpClient _bezeroa;

    // Base de datos compartida por el servidor.
    private readonly DatuBasea _datuBasea;

    // Servidor principal, utilizado para actualizar el contador de clientes.
    private readonly Zerbitzaria _zerbitzaria;

    // Usuario que ha iniciado sesión en esta conexión.
    private Erabiltzailea? _erabiltzailea;

    // Guardamos los objetos que necesita esta conexión.
    public BezeroKonexioa(TcpClient bezeroa, DatuBasea datuBasea, Zerbitzaria zerbitzaria)
    {
        _bezeroa = bezeroa;
        _datuBasea = datuBasea;
        _zerbitzaria = zerbitzaria;
    }

    // ================================================================
    //  ATENDER AL CLIENTE
    // ================================================================

    // Lee peticiones del cliente hasta que se desconecta.
    public void Exekutatu()
    {
        try
        {
            // Obtenemos el canal de comunicación TCP.
            NetworkStream fluxua = _bezeroa.GetStream();

            // Creamos un lector y un escritor de texto.
            StreamReader irakurlea = new StreamReader(fluxua, Encoding.UTF8);
            StreamWriter idazlea = new StreamWriter(fluxua, new UTF8Encoding(false));
            idazlea.AutoFlush = true;

            // Un cliente puede enviar varias peticiones durante su sesión.
            while (true)
            {
                string? lerroa = irakurlea.ReadLine();

                if (lerroa == null)
                {
                    // El cliente ha cerrado la conexión.
                    break;
                }

                // Procesamos la petición y creamos una respuesta.
                Erantzuna erantzuna = Prozesatu(lerroa);

                // Convertimos la respuesta a JSON y la enviamos.
                idazlea.WriteLine(JsonSerializer.Serialize(erantzuna));
            }
        }
        catch (IOException)
        {
            // La conexión se ha cortado de forma inesperada.
        }
        finally
        {
            // Cerramos el cliente y actualizamos el contador del servidor.
            _bezeroa.Close();
            _zerbitzaria.BezeroaDeskonektatu();
        }
    }

    // ================================================================
    //  PROCESAR UNA PETICIÓN
    // ================================================================

    // Convierte el JSON recibido, ejecuta la acción y crea una respuesta.
    private Erantzuna Prozesatu(string lerroa)
    {
        try
        {
            // Convertimos el texto JSON en una petición.
            Eskaera? eskaera = JsonSerializer.Deserialize<Eskaera>(lerroa);

            if (eskaera == null)
            {
                throw new InvalidOperationException("Eskaera hutsa.");
            }

            // Ejecutamos la acción solicitada por el cliente.
            object? emaitza = ExekutatuEkintza(eskaera);

            // Devolvemos una respuesta correcta con el resultado.
            return new Erantzuna
            {
                Ondo = true,
                Datuak = JsonSerializer.Serialize(emaitza)
            };
        }
        catch (InvalidOperationException errorea)
        {
            // Error esperado: se muestra directamente al usuario.
            return new Erantzuna
            {
                Ondo = false,
                Mezua = errorea.Message
            };
        }
        catch (Exception errorea)
        {
            // Error inesperado: se registra en el servidor y se avisa al cliente.
            Zerbitzaria.Idatzi("ERROREA: " + errorea);

            return new Erantzuna
            {
                Ondo = false,
                Mezua = "Zerbitzariko errorea: " + errorea.Message
            };
        }
    }

    // ================================================================
    //  EJECUTAR LAS ACCIONES
    // ================================================================

    // Decide qué operación realizar según la acción recibida.
    private object? ExekutatuEkintza(Eskaera eskaera)
    {
        // La única acción permitida sin iniciar sesión es el login.
        if (eskaera.Ekintza == Ekintzak.SaioaHasi)
        {
            return SaioaHasi(Irakurri<SaioDatuak>(eskaera));
        }

        // Todas las demás acciones necesitan una sesión iniciada.
        if (_erabiltzailea == null)
        {
            throw new InvalidOperationException("Lehenengo saioa hasi behar da.");
        }

        // Escribimos en la consola qué usuario ha realizado la acción.
        Zerbitzaria.Idatzi($"{_erabiltzailea.Izena}: {eskaera.Ekintza}");

        switch (eskaera.Ekintza)
        {
            // ---------------- PRODUCTOS ----------------
            case Ekintzak.Produktuak:
                return _datuBasea.LortuProduktuak();

            case Ekintzak.ProduktuaGorde:
                AdministratzaileaBehar();
                GordeProduktua(Irakurri<Produktua>(eskaera));
                return null;

            case Ekintzak.ProduktuaEzabatu:
                AdministratzaileaBehar();
                _datuBasea.EzabatuProduktua(Irakurri<int>(eskaera));
                return null;

            case Ekintzak.StockaGehitu:
                AdministratzaileaBehar();
                StockDatuak stockDatuak = Irakurri<StockDatuak>(eskaera);
                if (stockDatuak.Kopurua <= 0)
                {
                    throw new InvalidOperationException("Gehitzeko kopuruak positiboa izan behar du.");
                }
                _datuBasea.GehituStocka(stockDatuak.ProduktuId, stockDatuak.Kopurua);
                return null;

            // ---------------- USUARIOS ----------------
            case Ekintzak.Erabiltzaileak:
                AdministratzaileaBehar();
                return _datuBasea.LortuErabiltzaileak();

            case Ekintzak.ErabiltzaileaGorde:
                AdministratzaileaBehar();
                GordeErabiltzailea(Irakurri<ErabiltzaileDatuak>(eskaera));
                return null;

            case Ekintzak.ErabiltzaileaEzabatu:
                AdministratzaileaBehar();
                EzabatuErabiltzailea(Irakurri<int>(eskaera));
                return null;

            // ---------------- VENTAS ----------------
            case Ekintzak.SalmentaGorde:
                return _datuBasea.GordeSalmenta(_erabiltzailea.Izena, Irakurri<List<SaskiLerroa>>(eskaera));

            case Ekintzak.Salmentak:
                AdministratzaileaBehar();
                return _datuBasea.LortuSalmentak();

            // ---------------- RESERVAS ----------------
            case Ekintzak.Erreserbak:
                ErreserbaBilaketa bilaketa = Irakurri<ErreserbaBilaketa>(eskaera);
                return _datuBasea.LortuErreserbak(bilaketa.Data, bilaketa.Txanda);

            case Ekintzak.ErreserbaGorde:
                GordeErreserba(Irakurri<Erreserba>(eskaera));
                return null;

            case Ekintzak.ErreserbaAldatu:
                AldatuErreserba(Irakurri<Erreserba>(eskaera));
                return null;

            case Ekintzak.ErreserbaEzabatu:
                EzabatuErreserba(Irakurri<int>(eskaera));
                return null;

            default:
                throw new InvalidOperationException("Ekintza ezezaguna: " + eskaera.Ekintza);
        }
    }

    // ================================================================
    //  LOGIN Y PERMISOS
    // ================================================================

    // Comprueba las credenciales y guarda el usuario de esta conexión.
    private Erabiltzailea SaioaHasi(SaioDatuak datuak)
    {
        Erabiltzailea? erabiltzailea = _datuBasea.SaioaHasi(datuak.Izena, datuak.Pasahitza);

        if (erabiltzailea == null)
        {
            throw new InvalidOperationException("Erabiltzailea edo pasahitza ez da zuzena.");
        }

        // Guardamos el usuario para conocer su identidad y su rol.
        _erabiltzailea = erabiltzailea;
        Zerbitzaria.Idatzi($"{erabiltzailea.Izena}-k saioa hasi du ({erabiltzailea.Rola}).");

        return erabiltzailea;
    }

    // Solo los administradores pueden realizar ciertas operaciones.
    private void AdministratzaileaBehar()
    {
        if (!_erabiltzailea!.AdministratzaileaDa())
        {
            throw new InvalidOperationException("Ekintza hau administratzaileek bakarrik egin dezakete.");
        }
    }

    // ================================================================
    //  PRODUCTOS Y USUARIOS
    // ================================================================

    // Valida y guarda un producto.
    private void GordeProduktua(Produktua produktua)
    {
        if (produktua.Izena.Trim() == "")
        {
            throw new InvalidOperationException("Produktuaren izena hutsik dago.");
        }

        if (produktua.Prezioa < 0 || produktua.Stocka < 0)
        {
            throw new InvalidOperationException("Prezioa eta stocka ezin dira negatiboak izan.");
        }

        _datuBasea.GordeProduktua(produktua);
    }

    // Valida y guarda un usuario.
    private void GordeErabiltzailea(ErabiltzaileDatuak datuak)
    {
        Erabiltzailea erabiltzailea = datuak.Erabiltzailea;

        if (erabiltzailea.Izena.Trim() == "")
        {
            throw new InvalidOperationException("Erabiltzailearen izena hutsik dago.");
        }

        if (erabiltzailea.Id == 0 && datuak.Pasahitza == "")
        {
            throw new InvalidOperationException("Erabiltzaile berriak pasahitza behar du.");
        }

        if (erabiltzailea.Id == _erabiltzailea!.Id && !erabiltzailea.AdministratzaileaDa())
        {
            throw new InvalidOperationException("Ezin duzu zure administratzaile-rola kendu.");
        }

        _datuBasea.GordeErabiltzailea(erabiltzailea, datuak.Pasahitza);
    }

    // Elimina un usuario, excepto el usuario que está conectado.
    private void EzabatuErabiltzailea(int id)
    {
        if (id == _erabiltzailea!.Id)
        {
            throw new InvalidOperationException("Ezin duzu zure burua ezabatu.");
        }

        _datuBasea.EzabatuErabiltzailea(id);
    }

    // ================================================================
    //  RESERVAS Y PERMISOS SOBRE RESERVAS
    // ================================================================

    // Valida y guarda una reserva nueva.
    private void GordeErreserba(Erreserba erreserba)
    {
        ErreserbaZuzenaDa(erreserba);

        // El servidor decide quién es el usuario de la reserva.
        erreserba.Id = 0;
        erreserba.Erabiltzailea = _erabiltzailea!.Izena;

        _datuBasea.GordeErreserba(erreserba);
    }

    // Modifica una reserva después de comprobar que se puede modificar.
    private void AldatuErreserba(Erreserba berria)
    {
        ErreserbaZuzenaDa(berria);

        // Recuperamos la reserva original para comprobar sus permisos.
        Erreserba zaharra = _datuBasea.LortuErreserba(berria.Id);
        BaimenaErreserbarako(zaharra);

        // Mantenemos el usuario original de la reserva.
        berria.Erabiltzailea = zaharra.Erabiltzailea;
        _datuBasea.GordeErreserba(berria);
    }

    // Elimina una reserva si el usuario tiene permiso.
    private void EzabatuErreserba(int id)
    {
        Erreserba erreserba = _datuBasea.LortuErreserba(id);
        BaimenaErreserbarako(erreserba);
        _datuBasea.EzabatuErreserba(id);
    }

    // Un trabajador solo puede modificar sus reservas.
    // Un administrador puede modificar las reservas de todos.
    private void BaimenaErreserbarako(Erreserba erreserba)
    {
        if (!_erabiltzailea!.AdministratzaileaDa() && erreserba.Erabiltzailea != _erabiltzailea.Izena)
        {
            throw new InvalidOperationException("Beste erabiltzaile baten erreserba ezin duzu aldatu edo ezabatu.");
        }
    }

    // Comprueba que los datos básicos de una reserva son correctos.
    private static void ErreserbaZuzenaDa(Erreserba erreserba)
    {
        if (erreserba.MahaiZenbakia <= 0)
        {
            throw new InvalidOperationException("Aukeratu mahai bat.");
        }

        if (erreserba.Txanda != Erreserba.Bazkaria && erreserba.Txanda != Erreserba.Afaria)
        {
            throw new InvalidOperationException("Txanda ez da zuzena.");
        }

        // Como la fecha tiene el formato yyyy-MM-dd,
        // se puede comparar directamente como texto.
        if (erreserba.Data != "" && string.Compare(erreserba.Data, DateTime.Today.ToString("yyyy-MM-dd")) < 0)
        {
            throw new InvalidOperationException("Ezin da iraganeko egun batean erreserbatu.");
        }
    }

    // ================================================================
    //  LEER LOS DATOS DE UNA PETICIÓN
    // ================================================================

    // Convierte los datos JSON de la petición al tipo necesario.
    private static T Irakurri<T>(Eskaera eskaera)
    {
        T? datuak = JsonSerializer.Deserialize<T>(eskaera.Datuak);

        if (datuak == null)
        {
            throw new InvalidOperationException("Eskaeraren datuak ez dira zuzenak.");
        }

        return datuak;
    }
}
