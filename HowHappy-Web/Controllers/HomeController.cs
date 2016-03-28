using HowHappy_Web.Models;
using HowHappy_Web.ViewModels;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ExifLib;
using Microsoft.Extensions.OptionsModel;
using HowHappy_Web.Logic;

namespace HowHappy_Web.Controllers
{
    public class HomeController : Controller
    {
        //_apiKey: Replace this with your own Project Oxford Emotion API key, please do not use my key. I include it here so you can get up and running quickly but you can get your own key for free at https://www.projectoxford.ai/emotion 
        public const string _apiKey = "1dd1f4e23a5743139399788aa30a7153";

        //_apiUrl: The base URL for the API. Find out what this is for other APIs via the API documentation
        public const string _apiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        IApplicationEnvironment hostingEnvironment;
        AzureAppSettings options;
        DayOfWeekStore dayOfWeekStore;
        DateExtractor dateExtractor;

        public HomeController(IApplicationEnvironment _hostingEnvironment, IOptions<AzureAppSettings> _options, DayOfWeekStore _dayOfWeekStore, DateExtractor _dateExtractor)
        {
            hostingEnvironment = _hostingEnvironment;
            options = _options.Value;
            dayOfWeekStore = _dayOfWeekStore;
            dateExtractor = _dateExtractor;
        }


        public IActionResult Index()
        {
            return View();
        }

        // POST: Home/FileExample
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Result(IFormFile file)
        {
            //initialise vars
            var facesSorted = new List<Face>();
            var base64Image = string.Empty;

            //call emotion api and handle results
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                httpClient.BaseAddress = new Uri(_apiUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
                var content = new StreamContent(file.OpenReadStream());
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //make request
                var responseMessage = await httpClient.PostAsync(_apiUrl, content);

                //read response as a json string
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                //parse json string to object and enumerate
                var faces = new List<Face>();
                var responseArray = JArray.Parse(responseString);
                foreach (var faceResponse in responseArray)
                {
                    //deserialise json to face
                    var face = JsonConvert.DeserializeObject<Face>(faceResponse.ToString());

                    //add display scores
                    face = AddDisplayScores(face);

                    //add face to faces list
                    faces.Add(face);
                }

                //sort list by happiness score
                facesSorted = faces.OrderByDescending(o => o.scores.happiness).ToList();
            }

            //get bytes from image stream and convert to a base 64 string with the required image src prefix
            base64Image = "data:image/png;base64," + FileToBase64String(file);

            //create view model
            var dateTaken = dateExtractor.ReadDateFromImage(file);
            var vm = new ResultViewModel()
            {
                Faces = facesSorted,
                ImagePath = base64Image,
                DateTaken = dateTaken?.ToShortDateString()
            };

            WeekSummary summary;
            if (dateTaken != null)
                summary = dayOfWeekStore.SaveHappinessForEachFace(facesSorted, dateTaken.Value.DayOfWeek);
            else
                summary = dayOfWeekStore.ReadCurrentSummary(); // Picture does not have a date, just return the current summary.
            vm.HappinessSummary = SummariseDayHappinessRatings(summary);

            //return view
            return View(vm);
        }

        private string SummariseDayHappinessRatings(WeekSummary summary)
        {
            var dayTotals = new Dictionary<DayOfWeek, int>();
            foreach (var day in summary)
            {
                if (day.HappinessScores.Count == 0)
                    continue;
                double runningTotal = 0;
                int faceCount = day.HappinessScores.Sum(h => h.Value);

                foreach (var happinessValue in day.HappinessScores)
                    runningTotal += (happinessValue.Key + 1) * happinessValue.Value;

                dayTotals.Add(day.DayOfWeek, (int)(runningTotal / faceCount));
            }
            if (dayTotals.Count == 0)
                return "We don't have enough data to show which days are happiest, is the Azure blob storage connection string set?";
            return string.Join(",", dayTotals.OrderBy(t => t.Value).Select(t => $"{t.Key}: {t.Value}"));
        }

        public IActionResult Error()
        {
            return View();
        }

        private Face AddDisplayScores(Face face)
        {
            face.scores.angerDisplay = Math.Round(face.scores.anger, 2);
            face.scores.contemptDisplay = Math.Round(face.scores.contempt, 2);
            face.scores.disgustDisplay = Math.Round(face.scores.disgust, 2);
            face.scores.fearDisplay = Math.Round(face.scores.fear, 2);
            face.scores.happinessDisplay = Math.Round(face.scores.happiness, 2);
            face.scores.neutralDisplay = Math.Round(face.scores.neutral, 2);
            face.scores.sadnessDisplay = Math.Round(face.scores.sadness, 2);
            face.scores.surpriseDisplay = Math.Round(face.scores.surprise, 2);
            return face;
        }

        private string FileToBase64String(IFormFile file)
        {
            var base64String = string.Empty;
            using (var sourceStream = file.OpenReadStream())
            {
                using (var sourceMemoryStream = new MemoryStream())
                {
                    sourceStream.CopyTo(sourceMemoryStream);
                    var bytes = sourceMemoryStream.ToArray();
                    base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
                }
            }
            return base64String;
        }
    }
}
