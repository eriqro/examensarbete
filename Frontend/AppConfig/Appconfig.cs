using Microsoft.Extensions.Configuration;
using System;
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
            Configuration.GetConnectionString(name) ?? string.Empty;

        public static string GetBackendBaseUrl()
        {
            var baseUrl = Configuration["BackendApiBaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("BackendApiBaseUrl is not configured.");
            return baseUrl;
        }
    }
}
