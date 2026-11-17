using Microsoft.Data.Sqlite;
using System.IO;
using TPVelkartea.Models;

namespace TPVelkartea.Data;

public sealed class DatabaseService
{
    private readonly string _connectionString;
    private readonly object _writeLock = new();

    public DatabaseService()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);
        _connectionString = $"Data Source={Path.Combine(dataDirectory, "tpv.db")}";
        Initialize();
    }

    public User? Authenticate(string username, string password)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, Role FROM Users WHERE Username = $username AND Password = $password";
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$password", password);

        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new User(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
    }

    public List<Product> GetProducts()
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Price, Stock FROM Products ORDER BY Name";

        using SqliteDataReader reader = command.ExecuteReader();
        List<Product> products = new();

        while (reader.Read())
        {
            products.Add(new Product(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetDecimal(2),
                reader.GetInt32(3)));
        }

        return products;
    }

    public void SaveProduct(Product product)
    {
        lock (_writeLock)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = product.Id == 0
                ? "INSERT INTO Products (Name, Price, Stock) VALUES ($name, $price, $stock)"
                : "UPDATE Products SET Name=$name, Price=$price, Stock=$stock WHERE Id=$id";

            command.Parameters.AddWithValue("$name", product.Name);
            command.Parameters.AddWithValue("$price", product.Price);
            command.Parameters.AddWithValue("$stock", product.Stock);

            if (product.Id != 0)
            {
                command.Parameters.AddWithValue("$id", product.Id);
            }

            command.ExecuteNonQuery();
        }
    }

    public void DeleteProduct(int id)
    {
        lock (_writeLock)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Products WHERE Id=$id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }
    }

    public List<User> GetUsers()
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, Role FROM Users ORDER BY Username";

        using SqliteDataReader reader = command.ExecuteReader();
        List<User> users = new();

        while (reader.Read())
        {
            users.Add(new User(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return users;
    }

    public void SaveUser(int id, string username, string password, string role)
    {
        lock (_writeLock)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = id == 0
                ? "INSERT INTO Users (Username, Password, Role) VALUES ($username,$password,$role)"
                : "UPDATE Users SET Username=$username, Password=$password, Role=$role WHERE Id=$id";

            command.Parameters.AddWithValue("$username", username);
            command.Parameters.AddWithValue("$password", password);
            command.Parameters.AddWithValue("$role", role);

            if (id != 0)
            {
                command.Parameters.AddWithValue("$id", id);
            }

            command.ExecuteNonQuery();
        }
    }

    public void DeleteUser(int id)
    {
        lock (_writeLock)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Users WHERE Id=$id";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }
    }

    public void CompleteSale(string username, IReadOnlyCollection<SaleLine> lines)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Saskia hutsik dago.");
        }

        lock (_writeLock)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            try
            {
                foreach (SaleLine line in lines)
                {
                    UpdateStock(connection, transaction, line);
                }

                long saleId = InsertSale(connection, transaction, username, lines);
                InsertSaleLines(connection, transaction, saleId, lines);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    public void AddReservation(int table, DateTime? date, string meal, string username)
    {
        lock (_writeLock)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();
            object dateValue = GetDateText(date);

            if (ReservationExists(connection, transaction, table, dateValue, meal))
            {
                throw new InvalidOperationException("Mahai hori dagoeneko erreserbatuta dago data eta txanda horretarako.");
            }

            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO Reservations (TableNumber, ReservationDate, Meal, Username) VALUES ($table,$date,$meal,$user)";
            command.Parameters.AddWithValue("$table", table);
            command.Parameters.AddWithValue("$date", dateValue);
            command.Parameters.AddWithValue("$meal", meal);
            command.Parameters.AddWithValue("$user", username);
            command.ExecuteNonQuery();
            transaction.Commit();
        }
    }

    public HashSet<int> GetReservedTables(DateTime? date, string meal)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT TableNumber FROM Reservations WHERE Meal=$meal AND (ReservationDate=$date OR ReservationDate IS NULL OR $date IS NULL)";
        command.Parameters.AddWithValue("$meal", meal);
        command.Parameters.AddWithValue("$date", GetDateText(date));

        using SqliteDataReader reader = command.ExecuteReader();
        HashSet<int> tables = new();

        while (reader.Read())
        {
            tables.Add(reader.GetInt32(0));
        }

        return tables;
    }

    private SqliteConnection OpenConnection()
    {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }

    private void Initialize()
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL UNIQUE, Password TEXT NOT NULL, Role TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Products (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Price REAL NOT NULL, Stock INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS Reservations (Id INTEGER PRIMARY KEY AUTOINCREMENT, TableNumber INTEGER NOT NULL, ReservationDate TEXT NULL, Meal TEXT NOT NULL, Username TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Sales (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL, CreatedAt TEXT NOT NULL, Total REAL NOT NULL);
            CREATE TABLE IF NOT EXISTS SaleLines (Id INTEGER PRIMARY KEY AUTOINCREMENT, SaleId INTEGER NOT NULL, ProductId INTEGER NOT NULL, Quantity INTEGER NOT NULL, Price REAL NOT NULL, FOREIGN KEY(SaleId) REFERENCES Sales(Id));
            INSERT OR IGNORE INTO Users (Username, Password, Role) VALUES ('admin', 'admin', 'Admin'), ('user', 'user', 'User');
            INSERT OR IGNORE INTO Products (Id, Name, Price, Stock) VALUES (1, 'Patata tortilla', 8.50, 20), (2, 'Txuleta', 22.00, 10), (3, 'Sagardoa', 3.50, 40);
            """;
        command.ExecuteNonQuery();
        SeedProducts(connection);
    }

    private static void SeedProducts(SqliteConnection connection)
    {
        (string Name, decimal Price, int Stock)[] products =
        {
            ("Cerveza caña", 2.20m, 120), ("Cerveza zurito", 1.50m, 100), ("Cerveza botella", 2.80m, 72),
            ("Cerveza sin alcohol", 2.40m, 48), ("Cerveza tostada", 3.10m, 36), ("Cerveza artesanal IPA", 4.20m, 24),
            ("Sidra natural botella", 3.50m, 60), ("Sidra natural jarra", 6.00m, 30), ("Vino tinto crianza copa", 3.20m, 50),
            ("Vino tinto crianza botella", 18.00m, 18), ("Vino blanco copa", 3.00m, 42), ("Vino blanco botella", 16.00m, 16),
            ("Txakoli copa", 3.50m, 36), ("Cava copa", 4.50m, 24), ("Cava botella", 22.00m, 10),
            ("Whisky", 6.00m, 20), ("Ron", 5.50m, 20), ("Ginebra", 6.00m, 22), ("Vodka", 5.50m, 18),
            ("Licor de hierbas", 4.50m, 24), ("Patxaran", 4.50m, 24), ("Coñac", 6.50m, 12),
            ("Refresco cola", 2.50m, 60), ("Refresco cola zero", 2.50m, 48), ("Naranja", 2.50m, 36),
            ("Limón", 2.50m, 36), ("Agua mineral", 1.80m, 80), ("Agua con gas", 2.00m, 30),
            ("Tónica", 2.50m, 24), ("Mosto", 2.20m, 24), ("Café", 1.50m, 100), ("Café con leche", 1.80m, 80),
            ("Té e infusiones", 1.60m, 40), ("Zumo de naranja", 2.80m, 30),
            ("Patatas bravas", 7.50m, 25), ("Croquetas caseras", 9.00m, 30), ("Ración de jamón", 14.00m, 18),
            ("Tabla de quesos", 12.00m, 16), ("Ensalada mixta", 8.00m, 20), ("Ensaladilla rusa", 7.00m, 20),
            ("Pan artesano", 1.20m, 100), ("Aceitunas", 3.00m, 40), ("Anchoas", 9.50m, 18),
            ("Txuleta", 22.00m, 10), ("Bacalao al pil pil", 19.00m, 12), ("Merluza a la plancha", 17.50m, 14),
            ("Pollo asado", 14.00m, 15), ("Patata tortilla", 8.50m, 20), ("Hamburguesa de la casa", 13.00m, 20),
            ("Tarta de queso", 5.50m, 18), ("Flan casero", 4.50m, 16), ("Helado", 4.00m, 24),
            ("Cuajada", 4.50m, 15), ("Brownie", 5.00m, 16),
            ("Aceite de oliva virgen extra", 8.50m, 12), ("Vinagre", 2.20m, 15), ("Sal", 1.00m, 30),
            ("Aceite de girasol", 3.50m, 12), ("Salsa alioli", 2.80m, 20), ("Salsa brava", 2.80m, 20)
        };

        foreach ((string name, decimal price, int stock) in products)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Products (Name, Price, Stock) SELECT $name, $price, $stock WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Name=$name)";
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$price", price);
            command.Parameters.AddWithValue("$stock", stock);
            command.ExecuteNonQuery();
        }
    }

    private static void UpdateStock(SqliteConnection connection, SqliteTransaction transaction, SaleLine line)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE Products SET Stock=Stock-$quantity WHERE Id=$id AND Stock >= $quantity";
        command.Parameters.AddWithValue("$quantity", line.Quantity);
        command.Parameters.AddWithValue("$id", line.Product.Id);

        if (command.ExecuteNonQuery() != 1)
        {
            throw new InvalidOperationException($"Ez dago nahikoa stock {line.Product.Name} produktuarentzat.");
        }
    }

    private static long InsertSale(SqliteConnection connection, SqliteTransaction transaction, string username, IReadOnlyCollection<SaleLine> lines)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO Sales (Username, CreatedAt, Total) VALUES ($user,$date,$total); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$user", username);
        command.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$total", lines.Sum(line => line.Total));
        return (long)command.ExecuteScalar()!;
    }

    private static void InsertSaleLines(SqliteConnection connection, SqliteTransaction transaction, long saleId, IReadOnlyCollection<SaleLine> lines)
    {
        foreach (SaleLine line in lines)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO SaleLines (SaleId, ProductId, Quantity, Price) VALUES ($sale,$product,$quantity,$price)";
            command.Parameters.AddWithValue("$sale", saleId);
            command.Parameters.AddWithValue("$product", line.Product.Id);
            command.Parameters.AddWithValue("$quantity", line.Quantity);
            command.Parameters.AddWithValue("$price", line.Product.Price);
            command.ExecuteNonQuery();
        }
    }

    private static bool ReservationExists(SqliteConnection connection, SqliteTransaction transaction, int table, object date, string meal)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM Reservations WHERE TableNumber=$table AND Meal=$meal AND (ReservationDate=$date OR ReservationDate IS NULL OR $date IS NULL)";
        command.Parameters.AddWithValue("$table", table);
        command.Parameters.AddWithValue("$meal", meal);
        command.Parameters.AddWithValue("$date", date);
        return (long)command.ExecuteScalar()! > 0;
    }

    private static object GetDateText(DateTime? date)
    {
        return date.HasValue ? date.Value.ToString("yyyy-MM-dd") : DBNull.Value;
    }
}