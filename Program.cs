using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using WhatsappBusiness.CloudApi.Configurations;
using WhatsappBusiness.CloudApi.Extensions;

namespace whatsapp_ollama_bot
{
    /// <summary>
    /// Entry point for the ASP.NET Core application. Configures dependency injection
    /// for the WhatsApp Business API client and the Ollama client, and sets up
    /// controller routing.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure the app to read from appsettings.json and environment variables.
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Bind WhatsApp configuration and register the client service.
            var waConfigSection = builder.Configuration.GetSection("WhatsAppBusinessCloudApiConfiguration");
            var waConfig = waConfigSection.Get<WhatsAppBusinessCloudApiConfig>() ?? new WhatsAppBusinessCloudApiConfig();
            builder.Services.AddWhatsAppBusinessCloudApiService(waConfig);

            // Register the Ollama client. It will read its own configuration from appsettings.json.
            builder.Services.AddSingleton<Services.OllamaClient>();

            // Register the Google Calendar service.
            builder.Services.AddSingleton<Services.GoogleCalendarService>();

            // Add controllers and JSON support. The WhatsApp client relies on Newtonsoft JSON.
            builder.Services.AddControllers().AddNewtonsoftJson();

            var app = builder.Build();

            // Map controller endpoints (attribute routing).
            app.MapControllers();

            app.Run();
        }
    }
}
