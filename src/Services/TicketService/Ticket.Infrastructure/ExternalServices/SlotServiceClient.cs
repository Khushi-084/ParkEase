using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Ticket.Application.DTOs;
using Ticket.Application.Interfaces;

namespace Ticket.Infrastructure.ExternalServices;

public class SlotServiceClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : ISlotServiceClient
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient Client
    {
        get
        {
            var client = httpClientFactory.CreateClient("SlotService");
            var authHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            
            if (!string.IsNullOrEmpty(authHeader))
            {
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", authHeader);
            }
            
            return client;
        }
    }

    public async Task<SlotDto> GetAvailableSlotAsync()
    {
        var response = await Client.GetAsync("api/v1/slots/available/first");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("No available slots found in the parking lot.");

        response.EnsureSuccessStatusCode();

        var slot = await response.Content.ReadFromJsonAsync<SlotDto>(_jsonOpts)
                   ?? throw new InvalidOperationException("Invalid response from SlotService.");
        return slot;
    }

    public async Task MarkSlotOccupiedAsync(Guid slotId)
    {
        var body = new StringContent(
            JsonSerializer.Serialize(new SlotStatusUpdateDto("Occupied")),
            Encoding.UTF8, "application/json");

        var response = await Client.PatchAsync($"api/v1/slots/{slotId}/status", body);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Slot {slotId} not found in SlotService.");

        response.EnsureSuccessStatusCode();
    }

    public async Task MarkSlotAvailableAsync(Guid slotId)
    {
        var body = new StringContent(
            JsonSerializer.Serialize(new SlotStatusUpdateDto("Available")),
            Encoding.UTF8, "application/json");

        var response = await Client.PatchAsync($"api/v1/slots/{slotId}/status", body);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Slot {slotId} not found in SlotService.");

        response.EnsureSuccessStatusCode();
    }
}