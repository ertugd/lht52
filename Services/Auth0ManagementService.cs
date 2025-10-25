using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace lht52.Services
{
    public class Auth0ManagementService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;


        public Auth0ManagementService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }


        private async Task<string> GetAccessTokenAsync()
        {
            var domain = _config["Auth0Management:Domain"];
            var clientId = _config["Auth0Management:ClientId"];
            var clientSecret = _config["Auth0Management:ClientSecret"];
            var audience = _config["Auth0Management:Audience"];


            var client = _httpClientFactory.CreateClient();


            var payload = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                audience = audience,
                grant_type = "client_credentials"
            };


            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://{domain}/oauth/token", content);


            response.EnsureSuccessStatusCode();


            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString();
        }


        public async Task SendVerificationEmailAsync(string userId)
        {
            var token = await GetAccessTokenAsync();
            var domain = _config["Auth0Management:Domain"];


            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var payload = new { user_id = userId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");


            var response = await client.PostAsync($"https://{domain}/api/v2/jobs/verification-email", content);
            response.EnsureSuccessStatusCode();
        }


        public async Task<string> CreatePasswordResetTicketAsync(string email)
        {
            var token = await GetAccessTokenAsync();
            var domain = _config["Auth0Management:Domain"];


            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var payload = new { email = email };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");


            var response = await client.PostAsync($"https://{domain}/api/v2/tickets/password-change", content);
            response.EnsureSuccessStatusCode();


            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("ticket", out var ticketProp) && ticketProp.ValueKind == JsonValueKind.String
                ? ticketProp.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
