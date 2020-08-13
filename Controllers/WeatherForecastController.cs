using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.IO;
using System.IO.Compression;
using GeoJSON.Net.Converters;
using System.Xml;

namespace desktop.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        static readonly HttpClient client = new HttpClient();
        static async Task<HttpContent> Fetch(string url)
        {
            HttpContent result = null;
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try	
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                result = response.Content;
            }
            catch(HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");	
                Console.WriteLine("Message :{0} ",e.Message);
            }
            return result;
        }

        static string AUTH_KEY = "?Authorization=CWB-E0F06EC3-785A-4058-8B12-7F43A46F4E17";
        static string URI = "https://opendata.cwb.gov.tw/fileapi/v1/opendataapi";

        [HttpGet]
        public async Task<List<string>> Get()
        {

            var httpContent = await Fetch($"{URI}/W-C0034-002.KMZ{AUTH_KEY}");
            var stream = await httpContent.ReadAsStreamAsync();
            ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var kml = archive.Entries
                .Select(x=>x.Name)
                .Where(name=>name.Contains(".kml"))
                .ToList();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            string json = JsonConvert.SerializeXmlNode(doc);

            var result = new GeoJsonConverter().ReadJson();

            return kml;
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
                {
                    _logger = logger;
                }
       
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
