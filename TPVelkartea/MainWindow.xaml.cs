using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TPVelkartea.Data;
using TPVelkartea.Models;

namespace TPVelkartea;

public partial class MainWindow : Window
{
    private readonly DatabaseService _database = new();
    private readonly List<SaleLine> _cart = new();

    private User? _currentUser;
    private string _selectedCategory = "all";
    private string _productSearch = string.Empty;
    private int _selectedTable;

    public MainWindow()
    {
        InitializeComponent();
        DateBox.SelectedDate = DateTime.Today;
        MealBox.SelectedIndex = 0;
        LoadTables();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameBox.Text.Trim();
        string password = PasswordBox.Password;
        _currentUser = _database.Authenticate(username, password);

        if (_currentUser == null)
        {
            LoginError.Text = "Erabiltzailea edo pasahitza ez da zuzena.";
            return;
        }

        LoginView.Visibility = Visibility.Collapsed;
        AppView.Visibility = Visibility.Visible;
        LoggedUser.Text = $"{_currentUser.Username} ({_currentUser.Role})";
        AdminTab.Visibility = _currentUser.Role == "Admin"
            ? Visibility.Visible
            : Visibility.Collapsed;

        LoadProducts();
        LoadTables();

        if (_currentUser.Role == "Admin")
        {
            LoadAdminData();
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _currentUser = null;
        AppView.Visibility = Visibility.Collapsed;
        LoginView.Visibility = Visibility.Visible;
        PasswordBox.Clear();
    }

    private void LoadProducts()
    {
        ProductPanel.Children.Clear();

        List<Product> products = _database.GetProducts()
            .Where(product => IsProductVisible(product))
            .ToList();

        foreach (Product product in products)
        {
            ProductPanel.Children.Add(CreateProductCard(product));
        }
    }

    private bool IsProductVisible(Product product)
    {
        bool correctCategory = _selectedCategory switch
        {
            "food" => !IsDrink(product.Name) && !IsDessert(product.Name),
            "drink" => IsDrink(product.Name),
            "dessert" => IsDessert(product.Name),
            _ => true
        };

        return correctCategory && product.Name.Contains(
            _productSearch,
            StringComparison.OrdinalIgnoreCase);
    }

    private Border CreateProductCard(Product product)
    {
        Grid imagePanel = new Grid
        {
            Height = 125,
            Background = new SolidColorBrush(Color.FromRgb(238, 236, 255))
        };

        imagePanel.Children.Add(new TextBlock
        {
            Text = ProductEmoji(product.Name),
            FontSize = 48,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        Image image = new Image
        {
            Stretch = Stretch.UniformToFill,
            Source = new BitmapImage(new Uri(ProductImage(product.Name)))
        };
        imagePanel.Children.Add(image);

        Button addButton = new Button
        {
            Content = product.Stock > 0 ? "GEHITU  +" : "AGORTUTA",
            Tag = product,
            Height = 34,
            Margin = new Thickness(0, 12, 0, 0),
            IsEnabled = product.Stock > 0
        };
        addButton.Click += AddToCart_Click;

        StackPanel details = new StackPanel();
        details.Children.Add(imagePanel);
        details.Children.Add(new TextBlock
        {
            Text = product.Name,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 32, 51)),
            Margin = new Thickness(0, 12, 0, 3)
        });
        details.Children.Add(CreateProductInfo(product));
        details.Children.Add(addButton);

        return new Border
        {
            Width = 220,
            Margin = new Thickness(0, 0, 16, 16),
            Padding = new Thickness(12),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(16),
            Child = details
        };
    }

    private static DockPanel CreateProductInfo(Product product)
    {
        DockPanel info = new DockPanel();
        info.Children.Add(new TextBlock
        {
            Text = $"{product.Price:C}",
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(91, 75, 219))
        });
        info.Children.Add(new TextBlock
        {
            Text = $"Stocka: {product.Stock}",
            Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
            HorizontalAlignment = HorizontalAlignment.Right
        });
        return info;
    }

    private static bool IsDrink(string name)
    {
        string[] drinks = { "cerveza", "sidra", "sagardoa", "vino", "txakoli", "cava", "whisky", "ron", "ginebra", "vodka", "licor", "patxaran", "coñac", "refresco", "naranja", "limón", "agua", "tónica", "mosto", "café", "té", "zumo" };
        return drinks.Any(name.Contains);
    }

    private static bool IsDessert(string name)
    {
        string[] desserts = { "tarta", "flan", "helado", "cuajada", "brownie", "postre" };
        return desserts.Any(name.Contains);
    }

    private void SetCategory(string category, string title)
    {
        _selectedCategory = category;
        CategoryTitle.Text = title;
        LoadProducts();
    }

    private void AllCategories_Click(object sender, RoutedEventArgs e) => SetCategory("all", "Produktu guztiak");
    private void FoodCategory_Click(object sender, RoutedEventArgs e) => SetCategory("food", "Platerak");
    private void DrinkCategory_Click(object sender, RoutedEventArgs e) => SetCategory("drink", "Edariak");
    private void DessertCategory_Click(object sender, RoutedEventArgs e) => SetCategory("dessert", "Postreak");

    private void ProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _productSearch = ((TextBox)sender).Text;
        LoadProducts();
    }

    private void Keypad_Click(object sender, RoutedEventArgs e)
    {
    }

    private void LoadTables()
    {
        if (FloorPlan == null)
        {
            return;
        }

        FloorPlan.Children.Clear();
        DrawFloorArea("SUKALDEA", 24, 22, 160, 68, "#E7E5E4");
        DrawFloorArea("BARRA", 24, 112, 160, 105, "#FDECC8");
        DrawFloorArea("SARRERA", 24, 438, 160, 62, "#DBEAFE");
        DrawFloorArea("LEIHOAK", 250, 22, 535, 35, "#DDF4F0");
        AddFloorLabel("ELKARTEKO JANTOKIA", 350, 28, 15, "#41605A");
        AddFloorLabel("Zerbitzu pasabidea", 328, 250, 12, "#8B8174");

        HashSet<int> reservedTables = _database.GetReservedTables(DateBox.SelectedDate, SelectedMeal());
        TableDefinition[] tables = GetTables();

        foreach (TableDefinition table in tables)
        {
            DrawTable(table, reservedTables.Contains(table.Number));
        }
    }

    private void DrawFloorArea(string title, double x, double y, double width, double height, string color)
    {
        Border area = new Border
        {
            Width = width,
            Height = height,
            Background = ToBrush(color),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(14),
            Child = new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = ToBrush("#5B5349"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        Canvas.SetLeft(area, x);
        Canvas.SetTop(area, y);
        FloorPlan.Children.Add(area);
    }

    private void AddFloorLabel(string text, double x, double y, double fontSize, string color)
    {
        TextBlock label = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = ToBrush(color)
        };

        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        FloorPlan.Children.Add(label);
    }

    private void DrawTable(TableDefinition table, bool reserved)
    {
        Brush chairColor = ToBrush("#714F3A");
        double radiusX = table.Width / 2 + 19;
        double radiusY = table.Height / 2 + 18;

        for (int i = 0; i < table.Seats; i++)
        {
            double angle = -Math.PI / 2 + 2 * Math.PI * i / table.Seats;
            Border chair = new Border
            {
                Width = 20,
                Height = 17,
                Background = chairColor,
                BorderBrush = ToBrush("#583B2B"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new RotateTransform(angle * 180 / Math.PI + 90)
            };

            Canvas.SetLeft(chair, table.X + Math.Cos(angle) * radiusX - chair.Width / 2);
            Canvas.SetTop(chair, table.Y + Math.Sin(angle) * radiusY - chair.Height / 2);
            FloorPlan.Children.Add(chair);
        }

        Border tableSurface = new Border
        {
            Width = table.Width,
            Height = table.Height,
            Background = ToBrush(reserved ? "#F8D8D6" : "#D9B889"),
            BorderBrush = ToBrush(reserved ? "#CA5F54" : "#9C7042"),
            BorderThickness = new Thickness(reserved ? 3 : 2),
            CornerRadius = table.Round ? new CornerRadius(50) : new CornerRadius(11),
            Tag = table.Number,
            Cursor = reserved ? System.Windows.Input.Cursors.Arrow : System.Windows.Input.Cursors.Hand,
            IsEnabled = !reserved,
            Child = CreateTableText(table, reserved)
        };

        tableSurface.MouseLeftButtonUp += Table_Click;
        Canvas.SetLeft(tableSurface, table.X - table.Width / 2);
        Canvas.SetTop(tableSurface, table.Y - table.Height / 2);
        Panel.SetZIndex(tableSurface, 2);
        FloorPlan.Children.Add(tableSurface);
    }

    private static StackPanel CreateTableText(TableDefinition table, bool reserved)
    {
        StackPanel text = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        text.Children.Add(new TextBlock { Text = table.Number.ToString(), FontSize = 18, FontWeight = FontWeights.Bold, Foreground = ToBrush("#4F3623"), HorizontalAlignment = HorizontalAlignment.Center });
        text.Children.Add(new TextBlock { Text = $"{table.Seats} eserleku", FontSize = 10, Foreground = ToBrush("#5A4330"), HorizontalAlignment = HorizontalAlignment.Center });
        text.Children.Add(new TextBlock { Text = reserved ? "ERRESERBATUA" : "LIBRE", FontSize = 8, FontWeight = FontWeights.Bold, Foreground = reserved ? Brushes.DarkRed : ToBrush("#237048"), HorizontalAlignment = HorizontalAlignment.Center });
        return text;
    }

    private void Table_Click(object sender, RoutedEventArgs e)
    {
        int tableNumber = (int)((FrameworkElement)sender).Tag;
        _selectedTable = tableNumber;

        TableDefinition table = GetTables().First(table => table.Number == tableNumber);
        SelectedTableLabel.Text = $"Mahaia {table.Number} · {table.Seats} eserleku";
    }

    private static TableDefinition[] GetTables()
    {
        return new[]
        {
            new TableDefinition(1, 2, 290, 135, 74, 58, true),
            new TableDefinition(2, 4, 485, 135, 88, 62, false),
            new TableDefinition(3, 6, 685, 135, 112, 66, false),
            new TableDefinition(4, 4, 290, 350, 88, 62, false),
            new TableDefinition(5, 2, 485, 350, 74, 58, true),
            new TableDefinition(6, 8, 685, 350, 122, 70, false)
        };
    }

    private string SelectedMeal()
    {
        return MealBox.SelectedItem is ComboBoxItem meal
            ? meal.Content.ToString()!
            : "Bazkaria";
    }

    private void ReservationTime_Changed(object sender, SelectionChangedEventArgs e)
    {
        _selectedTable = 0;
        SelectedTableLabel.Text = "Aukeratu libre dagoen mahaia planoan";
        LoadTables();
    }

    private static Brush ToBrush(string color)
    {
        return (Brush)new BrushConverter().ConvertFromString(color)!;
    }

    private static string ProductEmoji(string name)
    {
        if (name.Contains("txuleta", StringComparison.OrdinalIgnoreCase)) return "🥩";
        if (name.Contains("sagardoa", StringComparison.OrdinalIgnoreCase)) return "🍎";
        return "🍳";
    }

    private static string ProductImage(string name)
    {
        if (name.Contains("txuleta", StringComparison.OrdinalIgnoreCase))
        {
            return "https://images.unsplash.com/photo-1544025162-d76694265947?w=500&q=80";
        }

        if (name.Contains("sagardoa", StringComparison.OrdinalIgnoreCase))
        {
            return "https://images.unsplash.com/photo-1567696911980-2eedf3fcf972?w=500&q=80";
        }

        return "https://images.unsplash.com/photo-1565299507177-b0ac66763828?w=500&q=80";
    }

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        Product product = (Product)((Button)sender).Tag;
        int index = _cart.FindIndex(line => line.Product.Id == product.Id);

        if (index >= 0)
        {
            SaleLine line = _cart[index];
            _cart[index] = line with { Quantity = line.Quantity + 1 };
        }
        else
        {
            _cart.Add(new SaleLine(product, 1));
        }

        RefreshCart();
    }

    private void RefreshCart()
    {
        CartList.Items.Clear();

        foreach (SaleLine line in _cart)
        {
            CartList.Items.Add($"{line.Product.Name} x {line.Quantity} = {line.Total:C}");
        }

        CartTotal.Text = $"Guztira: {_cart.Sum(line => line.Total):C}";
    }

    private void CompleteSale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _database.CompleteSale(_currentUser!.Username, _cart);
            SaleMessage.Text = "Ordainketa eginda. Ticket-a: " +
                string.Join("; ", _cart.Select(line => $"{line.Product.Name} x{line.Quantity}")) +
                $" | Guztira: {_cart.Sum(line => line.Total):C}";
            _cart.Clear();
            RefreshCart();
            LoadProducts();
        }
        catch (Exception exception)
        {
            SaleMessage.Text = exception.Message;
        }
    }

    private void Reserve_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTable == 0 || MealBox.SelectedItem is not ComboBoxItem meal)
        {
            ReservationMessage.Text = "Aukeratu mahaia eta otordu mota.";
            return;
        }

        _database.AddReservation(
            _selectedTable,
            DateBox.SelectedDate,
            meal.Content.ToString()!,
            _currentUser!.Username);
        ReservationMessage.Text = "Erreserba gordeta.";
        LoadTables();
    }

    private void LoadAdminData()
    {
        ProductsGrid.ItemsSource = _database.GetProducts();
        UsersGrid.ItemsSource = _database.GetUsers();
    }

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product)
        {
            return;
        }

        ProductNameBox.Text = product.Name;
        ProductPriceBox.Text = product.Price.ToString();
        ProductStockBox.Text = product.Stock.ToString();
    }

    private void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(ProductPriceBox.Text, out decimal price) ||
            !int.TryParse(ProductStockBox.Text, out int stock) ||
            string.IsNullOrWhiteSpace(ProductNameBox.Text))
        {
            return;
        }

        int id = (ProductsGrid.SelectedItem as Product)?.Id ?? 0;
        Product product = new Product(id, ProductNameBox.Text.Trim(), price, stock);
        _database.SaveProduct(product);
        LoadAdminData();
        LoadProducts();
    }

    private void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is Product product)
        {
            _database.DeleteProduct(product.Id);
            LoadAdminData();
            LoadProducts();
        }
    }

    private void SaveUser_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewUsernameBox.Text) ||
            NewPasswordBox.Password.Length == 0 ||
            RoleBox.SelectedItem is not ComboBoxItem role)
        {
            return;
        }

        int id = (UsersGrid.SelectedItem as User)?.Id ?? 0;
        _database.SaveUser(id, NewUsernameBox.Text.Trim(), NewPasswordBox.Password, role.Content.ToString()!);
        LoadAdminData();
    }

    private void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is User user && user.Username != _currentUser?.Username)
        {
            _database.DeleteUser(user.Id);
            LoadAdminData();
        }
    }

    private sealed record TableDefinition(int Number, int Seats, double X, double Y, double Width, double Height, bool Round);
}