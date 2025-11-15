using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Threading.Tasks;
using WindowNativeTemplate.Services;
using WindowNativeTemplate.Interfaces;

namespace WindowNativeTemplate
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting Window Native Template application");

                var host = CreateHostBuilder(args).Build();
                
                // Get the window service and start monitoring
                var windowService = host.Services.GetRequiredService<IWindowService>();
                
                await windowService.StartMonitoringAsync();
                
                Log.Information("Application started successfully. Press Ctrl+C to exit.");
                
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IWindowService, WindowService>();
                    services.AddSingleton<IProcessService, ProcessService>();
                    services.AddHostedService<WindowMonitorService>();
                });
    }
}
