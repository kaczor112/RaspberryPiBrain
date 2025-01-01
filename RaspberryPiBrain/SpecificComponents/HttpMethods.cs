using MainComponents;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace RaspberryPiBrain
{
    public partial class NetworkManagement
    {
        public List<DomModel> NetworkModel { get; private set; }

        private int counterError { get; set; } = 0;
        private async Task LoadValuesOfDom()
        {
            try
            {
                string url = ApplicationSettings.MyWebsite + "dom.json";

                // Inicjalizacja HttpClient <- zezwalanie na brak certyfikatu na stronie www
                HttpClientHandler handler = new()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                HttpClient client = new(handler);

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync();

                    List<DomModel>? domList = JsonSerializer.Deserialize<List<DomModel>>(jsonData);

                    if (domList != null) NetworkModel = domList;
                }
                else
                {
                    if(counterError == 0) Logger.Write("Error: Nie można pobrać pliku JSON[Adres:" + ApplicationSettings.MyWebsite + "].");
                    if(counterError++ > 500) counterError = 0;
                }
            }
            catch (Exception ex)
            {
                ExceptionManagement.Log(ex, "HttpMethods", "LoadValuesOfDom");
            }
        }


        public static async Task SendRequest(string param, bool value)
        {
            try
            {
                string url = ApplicationSettings.MyWebsite + "dom.php?keyID=" + ApplicationSettings.KeyID + "&param=" + param + "&value=" + value;

                HttpClientHandler handler = new()
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                HttpClient client = new(handler);
                HttpResponseMessage response = await client.GetAsync(url);
                //response.EnsureSuccessStatusCode();
                //string responseBody =  = await response.Content.ReadAsStringAsync();
                //Console.WriteLine(responseBody);
            }
            catch (Exception ex)
            {
                ExceptionManagement.Log(ex, "HttpMethods", "SendRequest");
            }
        }
    }
}