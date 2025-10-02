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

        private readonly HttpClient _httpClient = new HttpClient();

        public GeneratorService(
            IPersonalInfoRepository personalRepo,
            IGenealogyRepository genealogyRepo,
            IHobbyRepository hobbyRepo,
            IYouTuberRepository youTuberRepo,
            ISerieRepository serieRepo,
            ISocialLinkRepository socialLinkRepo)
           
        {
            _personalRepo = personalRepo ?? throw new ArgumentNullException(nameof(personalRepo));
            _genealogyRepo = genealogyRepo ?? throw new ArgumentNullException(nameof(genealogyRepo));
            _hobbyRepo = hobbyRepo ?? throw new ArgumentNullException(nameof(hobbyRepo));
            _youTuberRepo = youTuberRepo ?? throw new ArgumentNullException(nameof(youTuberRepo));
            _serieRepo = serieRepo ?? throw new ArgumentNullException(nameof(serieRepo));
            _socialLinkRepo = socialLinkRepo ?? throw new ArgumentNullException(nameof(socialLinkRepo));
            
        }

        public void GenerateWebsite(string outputDir)
        {
            try
            {
                // Estructura de carpetas de salida
                Directory.CreateDirectory(outputDir);
                Directory.CreateDirectory(Path.Combine(outputDir, "pages"));
                Directory.CreateDirectory(Path.Combine(outputDir, "assets/css"));
                Directory.CreateDirectory(Path.Combine(outputDir, "assets/img"));
                Directory.CreateDirectory(Path.Combine(outputDir, "assets/js"));
                Directory.CreateDirectory(Path.Combine(outputDir, "data"));

                // Buscar templates
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string basePath = Path.Combine(baseDir, "Templates");
                if (!Directory.Exists(basePath))
                {
                    basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\MyWebsiteGenerator\Templates");
                }

                if (!Directory.Exists(basePath))
                {
                    throw new DirectoryNotFoundException($"La carpeta de templates no se encontró en {basePath}. Asegúrate de que 'Templates' esté en el proyecto y configurada para copiarse.");
                }

                // Registrar navegación como parcial
                Handlebars.RegisterTemplate("navigation", File.ReadAllText(Path.Combine(basePath, "navigation.hbs")));

                // Obtener datos
                var personalList = _personalRepo.GetAll();
                var personal = personalList.FirstOrDefault() ?? throw new Exception("No personal info found");

                var genealogy = _genealogyRepo.GetAll();
                var hobbies = _hobbyRepo.GetAll();
                var youtubers = _youTuberRepo.GetAll().ToList();
                var series = _serieRepo.GetAll();
                var socialLinks = _socialLinkRepo.GetAll();

                // Procesa la foto de perfil si es una ruta local
                if (!string.IsNullOrEmpty(personal.FotoUrl) && !personal.FotoUrl.StartsWith("http"))
                {
                    try
                    {
                        // Construir la ruta completa desde Templates/assets/img
                        string fileName = Path.GetFileName(personal.FotoUrl);
                        string sourcePath = Path.Combine(basePath, "assets", "img", fileName);
                        string destPath = Path.Combine(outputDir, "assets", "img", fileName);

                        if (File.Exists(sourcePath))
                        {
                            File.Copy(sourcePath, destPath, true);

                            // Ruta relativa para el HTML
                            personal.FotoUrl = $"../assets/img/{fileName}";
                        }
                        else
                        {

                            personal.FotoUrl = ""; //imagen por defecto
                        }
                    }
                    catch
                    {
                        // Ignorar errores aquí para que no bloquee la generación
                        personal.FotoUrl = "";
                    }
                }

                    // Descargar imágenes de Youtubers
                    DownloadImages(youtubers, outputDir);

                // Generar páginas
                var dataAbout = new { personal.Nombre, personal.Apellido, personal.FechaNacimiento, personal.FotoUrl, Genealogy = genealogy, isAbout = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/about.html"), RenderTemplate("about", dataAbout, basePath));
                File.WriteAllText(Path.Combine(outputDir, "data/about.json"), JsonConvert.SerializeObject(dataAbout));

                var dataHobbies = new
                {
                    Hobbies = hobbies.Select(h => new
                    {
                        h.ID,
                        h.Nombre,
                        h.Descripcion,
                        Imagenes = JsonConvert.DeserializeObject<List<string>>(h.Imagenes)
                    }),
                    isHobbies = true
                };
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

                // Copiar CSS y JS desde Templates/assets a outputDir/assets
                CopyStaticAssets(basePath, outputDir);
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);
                var logPath = Path.Combine(logDir, "log.txt");
                var currentDateTime = DateTime.Now;
                Console.WriteLine($"Error a las {currentDateTime:HH:mm:ss} el {currentDateTime:dd/MM/yyyy}: {ex.Message}");
                File.AppendAllText(logPath, $"{currentDateTime:yyyy-MM-dd HH:mm:ss}: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private void DownloadImages(List<YouTubers> youtubers, string outputDir)
        {
            var imgDir = Path.Combine(outputDir, "assets", "img");
            Directory.CreateDirectory(imgDir);

            foreach (var yt in youtubers)
            {
                if (!string.IsNullOrEmpty(yt.LogoUrl))
                {
                    try
                    {
                        var bytes = _httpClient.GetByteArrayAsync(yt.LogoUrl).Result;
                        var fileName = Path.GetFileName(new Uri(yt.LogoUrl).LocalPath);

                        // Asegurar extensión
                        if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
                        {
                            fileName += ".jpg";
                        }

                        var imgPath = Path.Combine(imgDir, fileName);
                        File.WriteAllBytes(imgPath, bytes);

                        // Actualiza la ruta para que el HTML use la copia local
                        yt.LogoUrl = $"../assets/img/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error descargando imagen: {yt.NombreCanal} -> {ex.Message}");

                        // fallback a un placeholder
                        yt.LogoUrl = "/assets/img/placeholder.png";

                        // log de error
                        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                        Directory.CreateDirectory(logDir);
                        var logPath = Path.Combine(logDir, "log.txt");
                        File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: Error {yt.NombreCanal} -> {ex.Message}\n{ex.StackTrace}\n");
                    }
                }
                else
                {
                    // Si no tiene logo asignado, usar placeholder
                    yt.LogoUrl = "/assets/img/placeholder.png";
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

        private void CopyStaticAssets(string basePath, string outputDir)
        {
            string sourceAssets = Path.Combine(basePath, "assets");
            string targetAssets = Path.Combine(outputDir, "assets");

            if (Directory.Exists(sourceAssets))
            {
                foreach (string dirPath in Directory.GetDirectories(sourceAssets, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceAssets, targetAssets));
                }

                foreach (string newPath in Directory.GetFiles(sourceAssets, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourceAssets, targetAssets), true);
                }
            }
            else
            {
                Console.WriteLine("No se encontraron assets en Templates/assets. Asegúrate de tener styles.css y main.js ahí.");
            }
        }
    }
}