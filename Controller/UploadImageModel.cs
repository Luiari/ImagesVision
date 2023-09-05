using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageVision.Controllers
{
    public class UploadImageModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public UploadImageModel(IConfiguration configuration)
        {
            _configuration = configuration;
            AnalysisResult = new AnalysisResultModel();
        }

        public AnalysisResultModel AnalysisResult { get; set; }

        // POST förfrågan när en bild laddas upp
        public async Task<IActionResult> OnPostAsync(IFormFile imageFile) // IFORMFILE är för att hantera filuppladdningar
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Skapa en ny instans av ComputerVisionClient med autentisering
                    var credentials = new ApiKeyServiceClientCredentials(_configuration["CognitiveServiceKey"]);
                    var cvClient = new ComputerVisionClient(credentials)
                    {
                        Endpoint = _configuration["CognitiveServicesEndpoint"]
                    };
                    // Analysera bilden med Azure Cognitive Service
                    await AnalyzeImage(cvClient, memoryStream);
                }
            }
            return Page();
        }
        //Analyserar bilden med Azure Cognitive Service
        private async Task AnalyzeImage(ComputerVisionClient cvClient, Stream imageStream)
        {
            Console.WriteLine("Analyzing image...");

            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>
            {
                //Funktionen som ska visas från bilden
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Adult

            };
            //Analyserar bilden
            var analysis = await cvClient.AnalyzeImageInStreamAsync(imageStream, features);

            // Skapa en modell för att lagra analysresultaten
            AnalysisResult = new AnalysisResultModel
            {
                ImageUrl = "URL_TILL_DEN_UPPLADDA_BILDEN", // Lägg till URL till den uppladdade bilden här
                Description = analysis.Description.Captions.FirstOrDefault()?.Text,
                Tags = analysis.Tags.Select(tag => tag.Name).ToList(),
                Categories = analysis.Categories.Select(category => category.Name).ToList(),
                Objects = analysis.Objects.Select(obj => obj.ObjectProperty).ToList(),
                Adult = new List<string> { $"Is Adult Content: {analysis.Adult.IsAdultContent}, Adult Score: {analysis.Adult.AdultScore.ToString("P")}" }


            };

            Console.WriteLine("Image Analysis Results:");
            // Get image captions
            foreach (var caption in analysis.Description.Captions)
            {
                Console.WriteLine($"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})");
            }

        }

        //Modell för att lagra analysresultat
        public class AnalysisResultModel
        {
            public string? ImageUrl { get; set; } // Lägg till egenskap för bild-URL
            public string? Description { get; set; }
            public List<string>? Tags { get; set; }
            public List<String>? Categories { get; set; }
            public List<String>? Objects { get; set; }
            public List<String>? Adult { get; set; }
        }
    }
}