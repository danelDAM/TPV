namespace TPVelkartea.Models;

public sealed record User(int Id, string Username, string Role);

public sealed record Product(int Id, string Name, decimal Price, int Stock);

public sealed record Reservation(int Id, int TableNumber, DateTime? Date, string Meal, string Username);

public sealed record SaleLine(Product Product, int Quantity)
{
    public decimal Total => Product.Price * Quantity;
}