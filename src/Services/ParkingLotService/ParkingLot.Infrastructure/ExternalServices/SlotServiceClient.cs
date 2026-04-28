using System.Net.Http.Json;
using ParkingLot.Application.Interfaces;

namespace ParkingLot.Infrastructure.ExternalServices;

public class SlotServiceClient(IHttpClientFactory httpClientFactory) : ISlotServiceClient
{
    public async Task BulkCreateSlotsAsync(Guid lotId, int count, decimal pricePerHour, string bearerToken)
    {
        var client = httpClientFactory.CreateClient("SlotService");

        if (!string.IsNullOrEmpty(bearerToken))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", bearerToken);
        }

        var request = new
        {
            lotId        = lotId,
            type         = "Car",
            count        = count,
            prefix       = "S",
            pricePerHour = pricePerHour
        };

        try
        {
            var response = await client.PostAsJsonAsync("api/v1/slots/bulk", request);
            // Log but don't fail lot creation if slot creation fails
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[SlotServiceClient] Bulk slot creation returned {response.StatusCode}: {error}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[SlotServiceClient] Failed to create slots: {ex.Message}");
            // Fail silent — lot creation should still succeed
        }
    }
    public async Task UpdateLotPricesAsync(Guid lotId, decimal newPrice, string bearerToken)
    {
        var client = httpClientFactory.CreateClient("SlotService");

        if (!string.IsNullOrEmpty(bearerToken))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", bearerToken);
        }

        try
        {
            var response = await client.PutAsJsonAsync($"api/v1/slots/lot/{lotId}/price", newPrice);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[SlotServiceClient] Update lot prices returned {response.StatusCode}: {error}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[SlotServiceClient] Failed to update lot prices: {ex.Message}");
        }
    }
}
