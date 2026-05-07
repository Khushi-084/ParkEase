using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ticket.Application.DTOs;
using Ticket.Application.Interfaces;

namespace Ticket.Infrastructure.ExternalServices;

public class SlotServiceClient(IHttpClientFactory httpClientFactory) : ISlotServiceClient
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient Client => httpClientFactory.CreateClient("SlotService");

    public async Task<SlotDTO> GetAvailableSlotAsync()
    {
        var response = await Client.GetAsync("api/v1/slots/available");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("No available slots found in the parking lot.");

        response.EnsureSuccessStatusCode();

        var slot = await response.Content.ReadFromJsonAsync<SlotDTO>(_jsonOpts)
                   ?? throw new InvalidOperationException("Invalid response from SlotService.");
        return slot;
    }

    public async Task<SlotDTO> GetAvailableSlotAsync(string? type)
    {
        var url = string.IsNullOrEmpty(type) 
            ? "api/v1/slots/available" 
            : $"api/v1/slots/available?type={type}";
            
        var response = await Client.GetAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new InvalidOperationException("No available slots found in the parking lot.");

        response.EnsureSuccessStatusCode();

        var slot = await response.Content.ReadFromJsonAsync<SlotDTO>(_jsonOpts)
                   ?? throw new InvalidOperationException("Invalid response from SlotService.");
        return slot;
    }

    public async Task<SlotDTO> GetSlotByIdAsync(Guid slotId)
    {
        var response = await Client.GetAsync($"api/v1/slots/{slotId}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Slot {slotId} not found in SlotService.");

        response.EnsureSuccessStatusCode();

        var slot = await response.Content.ReadFromJsonAsync<SlotDTO>(_jsonOpts)
                   ?? throw new InvalidOperationException("Invalid response from SlotService.");
        return slot;
    }

    public async Task<List<Guid>> GetSlotIdsByLotAsync(Guid lotId)
    {
        var response = await Client.GetAsync($"api/v1/slots/lot/{lotId}");

        response.EnsureSuccessStatusCode();

        var slots = await response.Content.ReadFromJsonAsync<List<SlotDTO>>(_jsonOpts)
                    ?? throw new InvalidOperationException("Invalid response from SlotService.");
        
        return slots.Select(slot => slot.SlotId).ToList();
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
