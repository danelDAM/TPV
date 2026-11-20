using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TPVelkartea.Ereduak;
using TPVelkartea.Protokoloa;

namespace TPVelkartea;

/// <summary>
/// Zerbitzariarekin TCP bidez hitz egiten duen klasea (bezeroaren aldea).
///
/// Metodo guztiak asinkronoak dira (async/await): zerbitzariaren erantzunaren
/// zain dagoen bitartean, leihoak erantzuten jarraitzen du (ez da "izozten").
/// </summary>
public class ZerbitzariBezeroa
{
    // ================================================================
    //  DATOS DE LA CONEXIÓN
    // ================================================================

    // Conexión TCP con el servidor.
    private TcpClient? _konexioa;

    // Lector para recibir mensajes del servidor.
    private StreamReader? _irakurlea;

    // Escritor para enviar mensajes al servidor.
    private StreamWriter? _idazlea;

    // Solo permitimos una petición a la vez.
    // Así evitamos mezclar las respuestas del servidor.
    private readonly SemaphoreSlim _txanda = new SemaphoreSlim(1, 1);

    // ================================================================
    //  CONECTAR Y DESCONECTAR
    // ================================================================

    // Abre una conexión TCP con el servidor.
    public async Task KonektatuAsync(string helbidea)
    {
        // Si ya había una conexión, la cerramos antes de crear otra.
        Deskonektatu();

        try
        {
            // Creamos el cliente TCP.
            _konexioa = new TcpClient();

            // Nos conectamos utilizando la dirección y el puerto configurado.
            await _konexioa.ConnectAsync(helbidea, Konfigurazioa.Ataka);
        }
        catch (SocketException)
        {
            // Si el servidor no está disponible, mostramos un error entendible.
            _konexioa = null;
            throw new InvalidOperationException($"Ezin da zerbitzarira konektatu ({helbidea}:{Konfigurazioa.Ataka}). Zerbitzaria martxan dago?");
        }

        // Obtenemos el canal de comunicación de la conexión.
        NetworkStream fluxua = _konexioa.GetStream();

        // Preparamos el lector y el escritor de texto.
        _irakurlea = new StreamReader(fluxua, Encoding.UTF8);
        _idazlea = new StreamWriter(fluxua, new UTF8Encoding(false));

        // Los mensajes se envían inmediatamente.
        _idazlea.AutoFlush = true;
    }

    // Cierra la conexión con el servidor.
    public void Deskonektatu()
    {
        if (_konexioa != null)
        {
            _konexioa.Close();
            _konexioa = null;
        }
    }

    // ================================================================
    //  ENVIAR UNA PETICIÓN Y RECIBIR UNA RESPUESTA
    // ================================================================

    // Este es el método común que utilizan productos, usuarios,
    // ventas y reservas.
    private async Task<Erantzuna> BidaliAsync(string ekintza, object? datuak)
    {
        // No se puede enviar nada si no estamos conectados.
        if (_konexioa == null)
        {
            throw new InvalidOperationException("Ez zaude zerbitzarira konektatuta.");
        }

        // Creamos la petición que recibirá el servidor.
        // Ekintza indica qué queremos hacer y Datuak contiene la información.
        Eskaera eskaera = new Eskaera
        {
            Ekintza = ekintza,
            Datuak = JsonSerializer.Serialize(datuak)
        };

        // Esperamos a que termine cualquier petición anterior.
        await _txanda.WaitAsync();

        try
        {
            // Convertimos la petición completa a JSON y la enviamos.
            await _idazlea!.WriteLineAsync(JsonSerializer.Serialize(eskaera));

            // Esperamos una línea de respuesta del servidor.
            string? lerroa = await _irakurlea!.ReadLineAsync();

            if (lerroa == null)
            {
                // Si no recibimos nada, la conexión se ha cerrado.
                throw new IOException();
            }

            // Convertimos el JSON recibido en un objeto Erantzuna.
            Erantzuna erantzuna = JsonSerializer.Deserialize<Erantzuna>(lerroa)!;

            // El servidor puede responder correctamente o devolver un error.
            if (!erantzuna.Ondo)
            {
                throw new InvalidOperationException(erantzuna.Mezua);
            }

            // Devolvemos la respuesta al método que hizo la petición.
            return erantzuna;
        }
        catch (IOException)
        {
            // Si se pierde la conexión, la cerramos y avisamos al cliente.
            Deskonektatu();
            throw new InvalidOperationException("Zerbitzariarekin konexioa galdu da. Hasi saioa berriro.");
        }
        finally
        {
            // Liberamos el bloqueo para permitir otra petición.
            _txanda.Release();
        }
    }

    // ================================================================
    //  CONVERTIR LOS DATOS DE UNA RESPUESTA
    // ================================================================

    // Convierte los datos JSON de la respuesta al tipo que necesitamos.
    // Por ejemplo: List<Produktua>, Salmenta o Erabiltzailea.
    private static T BihurtuDatuak<T>(Erantzuna erantzuna)
    {
        return JsonSerializer.Deserialize<T>(erantzuna.Datuak)!;
    }

    // ================================================================
    //  INICIO DE SESIÓN
    // ================================================================

    // Envía el nombre y la contraseña y recibe el usuario autenticado.
    public async Task<Erabiltzailea> SaioaHasiAsync(string izena, string pasahitza)
    {
        // Preparamos los datos del inicio de sesión.
        SaioDatuak datuak = new SaioDatuak
        {
            Izena = izena,
            Pasahitza = pasahitza
        };

        // Enviamos la petición al servidor.
        Erantzuna erantzuna = await BidaliAsync(
            Ekintzak.SaioaHasi,
            datuak);

        // Convertimos la respuesta en un usuario.
        return BihurtuDatuak<Erabiltzailea>(erantzuna);
    }

    // ================================================================
    //  PRODUCTOS Y STOCK
    // ================================================================

    // Solicita al servidor todos los productos.
    public async Task<List<Produktua>> LortuProduktuakAsync()
    {
        Erantzuna erantzuna = await BidaliAsync(Ekintzak.Produktuak, null);
        return BihurtuDatuak<List<Produktua>>(erantzuna);
    }

    // Envía un producto para guardarlo o modificarlo.
    public async Task GordeProduktuaAsync(Produktua produktua)
    {
        await BidaliAsync(Ekintzak.ProduktuaGorde, produktua);
    }

    // Envía el ID del producto que se quiere eliminar.
    public async Task EzabatuProduktuaAsync(int id)
    {
        await BidaliAsync(Ekintzak.ProduktuaEzabatu, id);
    }

    // Añade unidades al stock de un producto.
    public async Task GehituStockaAsync(int produktuId, int kopurua)
    {
        StockDatuak datuak = new StockDatuak
        {
            ProduktuId = produktuId,
            Kopurua = kopurua
        };

        await BidaliAsync(Ekintzak.StockaGehitu, datuak);
    }

    // ================================================================
    //  USUARIOS
    // ================================================================

    // Solicita la lista de usuarios.
    public async Task<List<Erabiltzailea>> LortuErabiltzaileakAsync()
    {
        Erantzuna erantzuna = await BidaliAsync(Ekintzak.Erabiltzaileak, null);
        return BihurtuDatuak<List<Erabiltzailea>>(erantzuna);
    }

    // Envía un usuario y su contraseña para guardarlo o modificarlo.
    public async Task GordeErabiltzaileaAsync(Erabiltzailea erabiltzailea, string pasahitza)
    {
        ErabiltzaileDatuak datuak = new ErabiltzaileDatuak
        {
            Erabiltzailea = erabiltzailea,
            Pasahitza = pasahitza
        };

        await BidaliAsync(Ekintzak.ErabiltzaileaGorde, datuak);
    }

    // Elimina un usuario utilizando su ID.
    public async Task EzabatuErabiltzaileaAsync(int id)
    {
        await BidaliAsync(Ekintzak.ErabiltzaileaEzabatu, id);
    }

    // ================================================================
    //  VENTAS
    // ================================================================

    // Envía el carrito y recibe la venta guardada.
    public async Task<Salmenta> GordeSalmentaAsync(List<SaskiLerroa> saskia)
    {
        Erantzuna erantzuna = await BidaliAsync(Ekintzak.SalmentaGorde, saskia);
        return BihurtuDatuak<Salmenta>(erantzuna);
    }

    // Solicita el historial completo de ventas.
    public async Task<List<Salmenta>> LortuSalmentakAsync()
    {
        Erantzuna erantzuna = await BidaliAsync(Ekintzak.Salmentak, null);
        return BihurtuDatuak<List<Salmenta>>(erantzuna);
    }

    // ================================================================
    //  RESERVAS
    // ================================================================

    // Solicita las reservas de una fecha y un turno concretos.
    public async Task<List<Erreserba>> LortuErreserbakAsync(string data, string txanda)
    {
        ErreserbaBilaketa bilaketa = new ErreserbaBilaketa
        {
            Data = data,
            Txanda = txanda
        };

        Erantzuna erantzuna = await BidaliAsync(Ekintzak.Erreserbak, bilaketa);
        return BihurtuDatuak<List<Erreserba>>(erantzuna);
    }

    // Guarda una reserva nueva.
    public async Task GordeErreserbaAsync(Erreserba erreserba)
    {
        await BidaliAsync(Ekintzak.ErreserbaGorde, erreserba);
    }

    // Modifica una reserva existente.
    public async Task AldatuErreserbaAsync(Erreserba erreserba)
    {
        await BidaliAsync(Ekintzak.ErreserbaAldatu, erreserba);
    }

    // Elimina una reserva utilizando su ID.
    public async Task EzabatuErreserbaAsync(int id)
    {
        await BidaliAsync(Ekintzak.ErreserbaEzabatu, id);
    }
}
