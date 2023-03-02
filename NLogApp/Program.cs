using NLog.Web;
using NLog;
using System.Configuration;
using Refit;
using NLogApp.References.App1API;

namespace NLogApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

            logger.Debug("init main");

            try
            {

                var builder = WebApplication.CreateBuilder(args);

                // Add services to the container.
                builder.Services.AddControllers();

                LogManager.Configuration.Variables["logConnectionString"] =
                    builder.Configuration.GetConnectionString("logConnectionString");

                // NLog: Setup NLog for Dependency injection
                builder.Logging.ClearProviders();
                builder.Host.UseNLog();

                builder.Services.AddRefitClient<IApp1Manager>()
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5081"));


                var app = builder.Build();

                // Configure the HTTP request pipeline.
                app.UseAuthorization();


                app.MapControllers();

                app.Run();
            }
            catch (Exception exception)
            {
                // NLog: catch setup errors
                logger.Error(exception, "Stopped program because of exception");
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }


        }
    }
}