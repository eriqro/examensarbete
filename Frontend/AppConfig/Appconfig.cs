using Microsoft.Extensions.Configuration;
using System.IO;

namespace Tune.Frontend.Helpers
{
    public static class AppConfig
    {
        public static IConfigurationRoot Configuration { get; }

        static AppConfig()
        {
            var basePath = AppContext.BaseDirectory;
            Configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("AppConfig/appsettings.Development.Local.json", optional: true, reloadOnChange: true)
                .Build();
        }

        public static string GetConnectionString(string name) =>
            Configuration.GetConnectionString(name);
    }
}
