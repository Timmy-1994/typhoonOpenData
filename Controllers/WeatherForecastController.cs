using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.IO;
using System.IO.Compression;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

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

        public class FeatureProperties {
            public string name;
            public string description;

            // public string stroke;
            // public string stroke-opacity;
            // public string stroke-width: 4;
            // public string styleHash: "-6e16ae2f";
            // public string styleUrl: "#past-track";
        }

        [HttpGet]
        public async Task<string> Get()
        {
            // request to cwb
            // var httpContent = await Fetch($"{URI}/W-C0034-002.KMZ{AUTH_KEY}");
            // var stream = await httpContent.ReadAsStreamAsync();
            // ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);

            // local muti test
            ZipArchive archive = ZipFile.OpenRead("./Data/muti_test.kmz");

            List<Stream> kmls = archive.Entries
                .Select(x=>x.Open())
                .ToList();
            
            string result = "";

            if(kmls.Any()){
                StreamReader reader = new StreamReader(kmls[0]);
                string xmlStr = await reader.ReadToEndAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlStr);
                
                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("kml","http://www.opengis.net/kml/2.2");
                XmlNodeList Placemark = doc.SelectNodes("//kml:Placemark", ns);

                IEnumerable<Feature> features = Placemark.Cast<XmlElement>()
                    .Select(x=>{
                        
                        IGeometryObject geometry = null;
                        
                        foreach(XmlElement g in x.ChildNodes){
                            if(!new Regex(@"Point|LineString|Polygon").IsMatch(g.Name)){
                                continue;
                            }
                            IEnumerable<double[]> coords = g.InnerText
                                .Trim()
                                .Split("\r\n")
                                .Select(x=>{
                                    try{
                                        return Array.ConvertAll(x.Split(','), Double.Parse);
                                    }catch{
                                        return null;
                                    }
                                });
                                
                            if(coords.Contains(null)){
                                continue;
                            }

                            if(g.Name == "Point"){
                                double lat = coords.ToList()[0][1];
                                double lng = coords.ToList()[0][0];
                                double alt = coords.ToList()[0][2];
                                geometry = new Point(new Position(lat,lng,alt));
                            }else if(g.Name == "LineString"){
                                geometry = new LineString(coords);
                            }else if(g.Name == "Polygon"){
                                geometry = new Polygon(new List<LineString>{new LineString(coords)});
                            }
                        }
                        
                        // XmlSerializer serializer = new XmlSerializer(typeof(FeatureProperties));
                        // var test = (FeatureProperties)serializer.Deserialize(new StringReader(x.InnerXml));
                        // Console.WriteLine(JsonConvert.SerializeObject(test));
                        
                        return new Feature(geometry,new {
                            name= x["name"] == null ? "" : x["name"].InnerText,
                            description = x["description"] == null ? "" : x["description"].InnerText,
                            styleUrl = x["styleUrl"] == null ? "" : x["styleUrl"].InnerText
                        });

                    });
            
                result = JsonConvert.SerializeObject(new FeatureCollection(features.ToList()));

            }
            return result;
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