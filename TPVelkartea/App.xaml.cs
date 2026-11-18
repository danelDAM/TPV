using System.Globalization;
using System.Windows;

namespace TPVelkartea;

/// <summary>
/// Aplikazioaren sarrera-puntua. Abiaraztean LeihoNagusia irekitzen da (ikus App.xaml).
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Zenbakiak eta datak euskarazko formatuan (adibidez: 8,50 €)
        CultureInfo euskara = new CultureInfo("eu-ES");
        CultureInfo.DefaultThreadCurrentCulture = euskara;
        CultureInfo.DefaultThreadCurrentUICulture = euskara;
        CultureInfo.CurrentCulture = euskara;
        CultureInfo.CurrentUICulture = euskara;

        base.OnStartup(e);
    }
}
