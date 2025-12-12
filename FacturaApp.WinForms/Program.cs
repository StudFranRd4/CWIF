using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FacturaApp.Data;
using FacturaApp.Services;
using FacturaApp.WinForms.Forms;

namespace FacturaApp.WinForms
{
    internal static class Program
    {
        [STAThread]
	
static void Main()
{
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
    Application.ThreadException += (s, e) => HandleException(e.Exception);
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    {
        if (e.ExceptionObject is Exception ex) HandleException(ex);
    };

    var services = new ServiceCollection();
    services.AddLogging(config =>
    {
        config.AddConsole();
        config.AddProvider(new FileLoggerProvider("facturaapp.log"));
    });

    services.AddDbContext<BillingDbContext>();
    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    services.AddScoped<AuthService>();
    services.AddScoped<LoginForm>();
    services.AddTransient<MainForm>();

    var provider = services.BuildServiceProvider();

    using (var scope = provider.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        db.EnsureDatabaseCreatedAndSeed();
    }

    // Bucle de login -> main -> logout -> login...
    while (true)
    {
        Session.CurrentUser = null;

        var login = provider.GetRequiredService<LoginForm>();

        if (login.ShowDialog() != DialogResult.OK || login.UsuarioAutenticado == null)
            break;

        Session.CurrentUser = login.UsuarioAutenticado;

        var main = provider.GetRequiredService<MainForm>();

        // Si el Main se cierra con Logout, seguimos el ciclo
        Application.Run(main);

        if (Session.CurrentUser == null)
            continue;

        break;
    }
}	

        static void HandleException(Exception ex)
        {
            try
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}