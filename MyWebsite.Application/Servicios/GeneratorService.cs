using FluentValidation;
using HandlebarsDotNet;
using MyWebsite.Core.Entidades;
using MyWebsite.Core.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace MyWebsite.Application.Servicios
{
    public class GeneratorService : IGeneratorService
    {
        private readonly IPersonalInfoRepository _personalRepo;
        private readonly IGenealogyRepository _genealogyRepo;
        private readonly IHobbyRepository _hobbyRepo;
        private readonly IYouTuberRepository _youTuberRepo;
        private readonly ISerieRepository _serieRepo;
        private readonly ISocialLinkRepository _socialLinkRepo;
        private readonly IValidator<PersonalInfo> _personalValidator;
        private readonly HttpClient _httpClient = new HttpClient();

        public GeneratorService(
            IPersonalInfoRepository personalRepo,
            IGenealogyRepository genealogyRepo,
            IHobbyRepository hobbyRepo,
            IYouTuberRepository youTuberRepo,
            ISerieRepository serieRepo,
            ISocialLinkRepository socialLinkRepo,
            IValidator<PersonalInfo> personalValidator)
        {
            _personalRepo = personalRepo ?? throw new ArgumentNullException(nameof(personalRepo));
            _genealogyRepo = genealogyRepo ?? throw new ArgumentNullException(nameof(genealogyRepo));
            _hobbyRepo = hobbyRepo ?? throw new ArgumentNullException(nameof(hobbyRepo));
            _youTuberRepo = youTuberRepo ?? throw new ArgumentNullException(nameof(youTuberRepo));
            _serieRepo = serieRepo ?? throw new ArgumentNullException(nameof(serieRepo));
            _socialLinkRepo = socialLinkRepo ?? throw new ArgumentNullException(nameof(socialLinkRepo));
            _personalValidator = personalValidator ?? throw new ArgumentNullException(nameof(personalValidator));
        }

        public void GenerateWebsite(string outputDir)
        {
            try
            {
                Directory.CreateDirectory(outputDir);
                Directory.CreateDirectory(Path.Combine(outputDir, "pages"));
                Directory.CreateDirectory(Path.Combine(outputDir, "assets/css"));
                Directory.CreateDirectory(Path.Combine(outputDir, "assets/img"));
                Directory.CreateDirectory(Path.Combine(outputDir, "assets/js"));
                Directory.CreateDirectory(Path.Combine(outputDir, "data"));

                // Depuración de la ruta base
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Directorio de ejecución: {baseDir}");
                string basePath = Path.Combine(baseDir, "Templates"); // Intenta primero la ruta de salida
                if (!Directory.Exists(basePath))
                {
                    Console.WriteLine($"Templates no encontrado en {basePath}. Intentando ruta relativa...");
                    basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\MyWebsiteGenerator\Templates");
                    Console.WriteLine($"Nueva ruta tentativa: {basePath}");
                }

                // Verificar si la carpeta existe
                if (!Directory.Exists(basePath))
                {
                    throw new DirectoryNotFoundException($"La carpeta de templates no se encontró en {basePath}. Asegúrate de que 'Templates' esté en el proyecto y configurada para copiarse.");
                }

                Handlebars.RegisterTemplate("navigation", File.ReadAllText(Path.Combine(basePath, "navigation.hbs")));

                var personalList = _personalRepo.GetAll();
                Console.WriteLine($"Personal count: {personalList.Count()}"); // Depuración
                var personal = personalList.FirstOrDefault() ?? throw new Exception("No personal info found");

                var genealogy = _genealogyRepo.GetAll();
                var hobbies = _hobbyRepo.GetAll();
                var youtubers = _youTuberRepo.GetAll().ToList();
                var series = _serieRepo.GetAll();
                var socialLinks = _socialLinkRepo.GetAll();

                DownloadImages(youtubers, outputDir);

                var dataAbout = new { personal.Nombre, personal.Apellido, personal.FechaNacimiento, Genealogy = genealogy, isAbout = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/about.html"), RenderTemplate("about", dataAbout, basePath));
                File.WriteAllText(Path.Combine(outputDir, "data/about.json"), JsonConvert.SerializeObject(dataAbout));

                var dataHobbies = new { Hobbies = hobbies.Select(h => new { h.ID, h.Nombre, h.Descripcion, Imagenes = JsonConvert.DeserializeObject<List<string>>(h.Imagenes) }), isHobbies = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/hobbies.html"), RenderTemplate("hobbies", dataHobbies, basePath));
                File.WriteAllText(Path.Combine(outputDir, "data/hobbies.json"), JsonConvert.SerializeObject(dataHobbies));

                var dataYouTubers = new { YouTubers = youtubers, isYouTubers = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/youtubers.html"), RenderTemplate("youtubers", dataYouTubers, basePath));
                File.WriteAllText(Path.Combine(outputDir, "data/youtubers.json"), JsonConvert.SerializeObject(dataYouTubers));

                var dataSeries = new { Series = series, isSeries = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/series.html"), RenderTemplate("series", dataSeries, basePath));
                File.WriteAllText(Path.Combine(outputDir, "data/series.json"), JsonConvert.SerializeObject(dataSeries));

                var dataContact = new { SocialLinks = socialLinks, isContact = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/contact.html"), RenderTemplate("contact", dataContact, basePath));
                File.WriteAllText(Path.Combine(outputDir, "data/contact.json"), JsonConvert.SerializeObject(dataContact));

                File.WriteAllText(Path.Combine(outputDir, "assets/css/styles.css"), GetCssContent());
                File.WriteAllText(Path.Combine(outputDir, "assets/js/main.js"), GetJsContent());
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, "log.txt");
                var currentDateTime = DateTime.Now; // 05:03 AM AST, 24 Sep 2025
                Console.WriteLine($"Error bonituu a las {currentDateTime:HH:mm:ss} el {currentDateTime:dd/MM/yyyy}: {ex.Message}");
                File.AppendAllText(logPath, $"{currentDateTime:yyyy-MM-dd HH:mm:ss}: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private void DownloadImages(List<YouTubers> youtubers, string outputDir)
        {
            foreach (var yt in youtubers)
            {
                if (!string.IsNullOrEmpty(yt.LogoUrl))
                {
                    try
                    {
                        var bytes = _httpClient.GetByteArrayAsync(yt.LogoUrl).Result;
                        var fileName = Path.GetFileName(new Uri(yt.LogoUrl).LocalPath);
                        var imgPath = Path.Combine(outputDir, "assets/img", fileName);
                        File.WriteAllBytes(imgPath, bytes);
                        yt.LogoUrl = $"/assets/img/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error descargando imagen: {ex.Message}");
                        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                        Directory.CreateDirectory(logDir);
                        var logPath = Path.Combine(logDir, "log.txt");
                        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {ex.Message}\n{ex.StackTrace}\n");
                    }
                }
            }
        }

        private string RenderTemplate(string templateName, object data, string basePath)
        {
            string path = Path.Combine(basePath, $"{templateName}.hbs");
            if (!File.Exists(path)) throw new FileNotFoundException($"No se encontró la plantilla en {path}");
            string content = File.ReadAllText(path);
            var template = Handlebars.Compile(content);
            return template(data);
        }

        private string GetCssContent()
        {
            return @"
        /* Importar Bootstrap (CDN) */
        @import url('https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css');

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #f8f9fa;
            color: #333;
        }

        /* Estilo de la navegación */
        .navbar {
            padding: 1rem;
        }
        .navbar-brand {
            font-size: 1.5rem;
            font-weight: bold;
        }
        .nav-link {
            color: #fff !important;
            transition: color 0.3s ease;
        }
        .nav-link:hover, .nav-link.active {
            color: #ffc107 !important; /* Amarillo Bootstrap */
            font-weight: 500;
        }

        /* Contenido general */
        .container {
            margin-top: 20px;
            padding: 20px;
            background: #fff;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        }

        /* Timeline */
        .timeline {
            border-left: 4px solid #007bff;
            padding-left: 20px;
            position: relative;
        }
        .timeline-item {
            margin-bottom: 20px;
            padding: 10px;
            background: #f1f1f1;
            border-radius: 5px;
            cursor: pointer;
            transition: background 0.3s ease;
        }
        .timeline-item:hover {
            background: #e9ecef;
        }

        /* Galería */
        .gallery img {
            width: 150px;
            height: 150px;
            object-fit: cover;
            border-radius: 5px;
            margin: 5px;
            transition: transform 0.3s ease;
        }
        .gallery img:hover {
            transform: scale(1.05);
        }

        /* Footer (opcional, si lo añades) */
        .footer {
            margin-top: 20px;
            padding: 10px;
            text-align: center;
            background: #333;
            color: #fff;
        }
    ";
        }



        private string GetJsContent()
        {
            return @"
                function validateForm() {
                    var name = document.getElementById('name').value;
                    var email = document.getElementById('email').value;
                    var message = document.getElementById('message').value;
                    if (!name || !email || !message) { alert('Campos obligatorios'); return false; }
                    if (!email.includes('@')) { alert('Correo inválido'); return false; }
                    alert('Enviado (simulación)'); return false;
                }

                document.addEventListener('DOMContentLoaded', function() {
                    document.querySelectorAll('.timeline-item').forEach(item => {
                        item.addEventListener('click', () => item.style.backgroundColor = item.style.backgroundColor === 'lightblue' ? '' : 'lightblue');
                    });
                });
            ";
        }
    }
}