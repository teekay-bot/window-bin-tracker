using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowBinTracker.Interfaces;
using WindowBinTracker.Services;
using WindowBinTracker.UI;

namespace WindowBinTracker
{
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            // Check if running as Windows Service
            if (args.Length > 0 && args[0] == "--service")
            {
                await CreateHostBuilder(args).Build().RunAsync();
                return;
            }

            // Run as Windows Forms application
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            // Initialize system tray service
            var systemTrayService = serviceProvider.GetRequiredService<ISystemTrayService>();
            await systemTrayService.InitializeAsync();

            // Start hosted services manually
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            foreach (var service in hostedServices)
            {
                if (service is RecycleBinMonitorService monitorService)
                {
                    _ = Task.Run(() => monitorService.StartAsync(default));
                }
            }

            Application.Run();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigureServices(services);
                })
                .UseSerilog((hostingContext, loggerConfiguration) => 
                    loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("logs/recyclebin-.txt", rollingInterval: RollingInterval.Day)
                    .CreateLogger());

        static void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            // Configure logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            // Register services
            services.AddSingleton<IRecycleBinService, RecycleBinService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ISystemTrayService, SystemTrayService>();

            // Register hosted services
            services.AddHostedService<RecycleBinMonitorService>();
        }
    }
}
