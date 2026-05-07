using System.Net.Http.Json;
using System.Net.Http;
using ParkingLot.Application.Interfaces;

namespace ParkingLot.Infrastructure.ExternalServices;

/// <summary>
/// FIXED: Calls TicketService to check for active tickets before lot deletion.
/// </summary>
public class TicketServiceClient(IHttpClientFactory httpClientFactory) : ITicketServiceClient
{
    public async Task<bool> HasActiveTicketsForLotAsync(Guid lotId, string bearerToken)
    {
        var client = httpClientFactory.CreateClient("TicketService");

        if (!string.IsNullOrEmpty(bearerToken))
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", bearerToken);
        }

        try
        {
            // GET /api/v1/ticket/lot/{lotId}/active-count  →  { count: N }
            var response = await client.GetAsync($"api/v1/ticket/lot/{lotId}/active-count");

            if (!response.IsSuccessStatusCode)
                return false; // TicketService unreachable — fail-open

            var result = await response.Content.ReadFromJsonAsync<ActiveTicketCountResponse>();
            return result?.Count > 0;
        }
        catch (HttpRequestException)
        {
            return false; // fail-open: let admin delete if service is down
        }
    }

    private record ActiveTicketCountResponse(int Count);
}
