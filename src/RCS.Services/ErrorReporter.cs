using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RCS.Services;

public class ErrorReporter
{
    private readonly HttpClient _httpClient = new HttpClient();
    private const string Endpoint = "https://dotnet-error-guard-684278904905.us-west1.run.app/api/errors";

    public async Task<bool> ReportErrorAsync(Exception ex, string severity = "high", string? userId = null)
    {
        Console.WriteLine("--- Starting Error Reporter Diagnostic ---");

        // 1. Test Connectivity first (GET)
        try 
        {
            var ping = await _httpClient.GetStringAsync($"{Endpoint}/ping");
            Console.WriteLine($"Step 1 (Ping): SUCCESS - Received: {ping}");
        } 
        catch (Exception pingEx) 
        {
            Console.WriteLine($"Step 1 (Ping): FAILED - {pingEx.Message}");
            return false; // Stop here if we can't even ping
        }

        // 2. Test Error Reporting (POST)
        var payload = new 
        {
            message = string.IsNullOrWhiteSpace(ex.StackTrace) ? ex.Message : $"{ex.Message}\n\n{ex.StackTrace}",
            severity = severity,
            source = "RCS Cogo Enterprise Modern",
            userId = userId ?? "System",
            url = "/help/ErrorReport"
        };

        // Serialize to JSON
        string json = JsonSerializer.Serialize(payload);
        
        // StringContent automatically sets 'Content-Type: application/json' 
        // AND calculates the 'Content-Length' header correctly.
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try 
        {
            var response = await _httpClient.PostAsync(Endpoint, content);
            string result = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"Step 2 (POST): SUCCESS - Server ID: {result}");
                return true;
            } 
            else 
            {
                Console.WriteLine($"Step 2 (POST): FAILED - Status: {response.StatusCode}");
                Console.WriteLine($"Server Response: {result}");
                return false;
            }
        }
        catch (Exception postEx) 
        {
            Console.WriteLine($"Step 2 (POST): CONNECTION ERROR - {postEx.Message}");
            return false;
        }
    }
}
