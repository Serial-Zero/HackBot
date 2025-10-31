using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HackBot.Modules
{
    public class IPModule : IModule
    {
        public string Name => "IP";
        public string Description => "Fetch and display victim's public IP address";

        public async Task ExecuteAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetStringAsync("https://api.ipify.org/?format=json");
                var jsonDoc = JsonDocument.Parse(response);
                var ip = jsonDoc.RootElement.GetProperty("ip").GetString();
                
                Console.WriteLine($"IP address: {ip}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching IP: {ex.Message}");
            }
        }
    }
}
