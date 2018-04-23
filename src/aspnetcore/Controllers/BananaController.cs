using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Bananaornot.Controllers
{
    [Route("api/[controller]")]
    public class BananaController : Controller
    {
        private readonly IConfiguration config;
        private readonly HttpClient httpClient;

        public BananaController(IConfiguration config, HttpClient httpClient)
        {
            this.config = config;
            this.httpClient = httpClient;
        }
        
        [HttpGet]
        public async Task<BananaResult> Get(string url)
        {
            var hasBanana = await ContainsBanana(url);
            //var hasNoBanana = await ContainsNoBanana(url);

            return new BananaResult
            {
                HasBanana = hasBanana,
                //HasNoBanana = hasNoBanana
            };
        }

        private async Task<bool> ContainsBanana(string url)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new { Url = url }));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.Add("Prediction-Key", config["CUSTOM_VISION_API_KEY"]);

            var response = await httpClient.PostAsync(config["CUSTOM_VISION_API_URL"], content);
            var result = JsonConvert.DeserializeObject<CustomVisionResult>(await response.Content.ReadAsStringAsync());

            var bananaPrediction = result.Predictions.FirstOrDefault(p => p.Tag == "banana");
            var bananaProbability = bananaPrediction?.Probability ?? 0;
            return bananaProbability > 0.7m;
        }

        private async Task<bool> ContainsNoBanana(string url)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new { url }));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.Add("Ocp-Apim-Subscription-Key", config["COMP_VISION_API_KEY"]);

            var response = await httpClient.PostAsync(config["COMP_VISION_API_URL"], content);
            var resultJson = await response.Content.ReadAsStringAsync();

            return Regex.IsMatch(resultJson, @"\bnot-banana\b", RegexOptions.IgnoreCase);
        }

        private class Prediction
        {
            public string Tag { get; set; }
            public decimal Probability { get; set; }
        }

        private class CustomVisionResult
        {
            public Prediction[] Predictions { get; set; }
        }

        public class BananaResult
        {
            public bool HasBanana { get; set; }
            //public bool HasNoBanana { get; set; }
        }
    }
}

