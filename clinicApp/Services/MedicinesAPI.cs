using System.Net.Http;

namespace clinicApp.Services
{
    internal class MedicinesAPI
    {
        private readonly HttpClient _httpClient;

        public MedicinesAPI()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri ("https://api.fda.gov/drug/label.json?search=openfda.brand_name:{query}&limit=10\r\n");
        }
    }
}
