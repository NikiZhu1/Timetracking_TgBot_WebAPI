using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTrackerBot.ApiServices
{
    public class ApiClient
    {
        public HttpClient HttpClient { get; }

        public string BaseUrl { get; }

        public ApiClient()
        {
            HttpClient = new();
            BaseUrl = Environment.GetEnvironmentVariable("BASE_API_URL")
                    ?? ConfigurationManager.AppSettings["BASE_API_URL"];
        }
    }
}
