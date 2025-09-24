using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyWebsite.Application.Servicios;
using MyWebsite.Application.Validators;
using MyWebsite.Core.Entidades;
using MyWebsite.Core.Interfaces;
using MyWebsite.Infrastructure.Repositories;
using System;

class Program
{
    static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services, builder);

        using (var serviceProvider = services.BuildServiceProvider())
        {
            using (var scope = serviceProvider.CreateScope())
            {
                try
                {
                    var generatorService = scope.ServiceProvider.GetRequiredService<IGeneratorService>();
                    var currentDateTime = DateTime.Now; 
                    Console.WriteLine($"Iniciando generación del sitio web a las {currentDateTime:HH:mm:ss} el {currentDateTime:dd/MM/yyyy}...");
                    generatorService.GenerateWebsite("Output");
                    Console.WriteLine("Sitio generado exitosamente en Output/");
                }
                catch (Exception ex)
                {
                    var currentDateTime = DateTime.Now;
                    Console.WriteLine($"Error al generar el sitio a las {currentDateTime:HH:mm:ss} el {currentDateTime:dd/MM/yyyy}: {ex.Message}");
                    File.AppendAllText("log.txt", $"{currentDateTime:yyyy-MM-dd HH:mm:ss}: {ex.Message}\n{ex.StackTrace}\n");
                }
            }
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PersonalDb") ?? throw new InvalidOperationException("Connection string 'PersonalDb' not found.");

        // Repositories with Dapper
        services.AddSingleton<IPersonalInfoRepository>(new PersonalInfoRepository(connectionString));
        services.AddSingleton<IGenealogyRepository>(new GenealogyRepository(connectionString));
        services.AddSingleton<IHobbyRepository>(new HobbyRepository(connectionString));
        services.AddSingleton<IYouTuberRepository>(new YouTuberRepository(connectionString));
        services.AddSingleton<ISerieRepository>(new SerieRepository(connectionString));
        services.AddSingleton<ISocialLinkRepository>(new SocialLinkRepository(connectionString));

        // Services
        services.AddSingleton<IGeneratorService, GeneratorService>();

        // Validators
        services.AddSingleton<IValidator<PersonalInfo>, PersonalInfoValidator>();
    }
}