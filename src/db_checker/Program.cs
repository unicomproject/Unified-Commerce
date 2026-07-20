using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var tenantId = "55555555-0000-4000-8000-000000000001";
        
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        var res = await client.PostAsJsonAsync("http://localhost:5150/api/v1/ecommerce/storefront/auth/login", new { emailOrPhone = "customer1@example.com", password = "password123" });
        var resBody = await res.Content.ReadAsStringAsync();
        Console.WriteLine($"Login: {resBody}");
        
        var doc = JsonDocument.Parse(resBody);
        if (!doc.RootElement.TryGetProperty("data", out var data)) return;
        var token = data.GetProperty("accessToken").GetString();
        
        var req = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5150/api/v1/ecommerce/storefront/checkout/from-cart");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Tenant-Id", tenantId);
        req.Headers.Add("X-Cart-Session-Id", "1832e428-9c04-44d5-839a-c47ed4f3e4db");
        
        var content = new {
            selectedOutletId = "bbbbbbbb-0001-4000-8000-000000000001",
            pickupContactName = "Test",
            pickupContactEmail = "test@example.com",
            pickupContactPhone = "123456"
        };
        req.Content = JsonContent.Create(content);
        
        try {
            var res2 = await client.SendAsync(req);
            var resBody2 = await res2.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {res2.StatusCode}");
            Console.WriteLine($"Body: {resBody2}");
        } catch(Exception e) {
            Console.WriteLine($"Error: {e.Message}");
        }
    }
}
