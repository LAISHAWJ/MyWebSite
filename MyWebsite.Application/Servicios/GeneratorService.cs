using FluentValidation;
using HandlebarsDotNet;
using MyWebsite.Core.Entidades;
using MyWebsite.Core.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                Handlebars.RegisterTemplate("navigation", File.ReadAllText("Templates/navigation.hbs"));

                var personalList = _personalRepo.GetAll();
                Console.WriteLine($"Personal count: {personalList.Count()}"); // Depuración
                var personal = personalList.FirstOrDefault() ?? throw new Exception("No personal info found");

                var genealogy = _genealogyRepo.GetAll();
                var hobbies = _hobbyRepo.GetAll();
                var youtubers = _youTuberRepo.GetAll().ToList(); // ToList para modificar LogoUrl
                var series = _serieRepo.GetAll();
                var socialLinks = _socialLinkRepo.GetAll();

                DownloadImages(youtubers, outputDir);

                var dataAbout = new { personal.Nombre, personal.Apellido, personal.FechaNacimiento, Genealogy = genealogy, isAbout = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/about.html"), RenderTemplate("about", dataAbout));
                File.WriteAllText(Path.Combine(outputDir, "data/about.json"), JsonConvert.SerializeObject(dataAbout));

                var dataHobbies = new { Hobbies = hobbies.Select(h => new { h.ID, h.Nombre, h.Descripcion, Imagenes = JsonConvert.DeserializeObject<List<string>>(h.Imagenes) }), isHobbies = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/hobbies.html"), RenderTemplate("hobbies", dataHobbies));
                File.WriteAllText(Path.Combine(outputDir, "data/hobbies.json"), JsonConvert.SerializeObject(dataHobbies));

                var dataYouTubers = new { YouTubers = youtubers, isYouTubers = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/youtubers.html"), RenderTemplate("youtubers", dataYouTubers));
                File.WriteAllText(Path.Combine(outputDir, "data/youtubers.json"), JsonConvert.SerializeObject(dataYouTubers));

                var dataSeries = new { Series = series, isSeries = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/series.html"), RenderTemplate("series", dataSeries));
                File.WriteAllText(Path.Combine(outputDir, "data/series.json"), JsonConvert.SerializeObject(dataSeries));

                var dataContact = new { SocialLinks = socialLinks, isContact = true };
                File.WriteAllText(Path.Combine(outputDir, "pages/contact.html"), RenderTemplate("contact", dataContact));
                File.WriteAllText(Path.Combine(outputDir, "data/contact.json"), JsonConvert.SerializeObject(dataContact));

                File.WriteAllText(Path.Combine(outputDir, "assets/css/styles.css"), GetCssContent());
                File.WriteAllText(Path.Combine(outputDir, "assets/js/main.js"), GetJsContent());
            }
            catch (Exception ex)
            {
                var currentDateTime = DateTime.Now; // 02:55 AM AST, 24 Sep 2025
                Console.WriteLine($"Error amigable a las {currentDateTime:HH:mm:ss} el {currentDateTime:dd/MM/yyyy}: {ex.Message}");
                File.AppendAllText("log.txt", $"{currentDateTime:yyyy-MM-dd HH:mm:ss}: {ex.Message}\n{ex.StackTrace}\n");
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
                        File.AppendAllText("log.txt", ex.ToString());
                    }
                }
            }
        }

        private string RenderTemplate(string templateName, object data)
        {
            string path = $"Templates/{templateName}.hbs";
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            string content = File.ReadAllText(path);
            var template = Handlebars.Compile(content);
            return template(data);
        }

        private string GetCssContent()
        {
            return @"
                body { font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }
                nav { background-color: #333; padding: 10px; }
                nav a { color: white; margin-right: 15px; text-decoration: none; }
                nav a.active { color: yellow; font-weight: bold; }
                .timeline { border-left: 4px solid #007bff; padding-left: 20px; }
                .timeline-item { margin-bottom: 20px; cursor: pointer; }
                .gallery img { width: 100px; margin-right: 5px; }
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
