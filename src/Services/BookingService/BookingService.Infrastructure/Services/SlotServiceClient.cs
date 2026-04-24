using BookingService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Services;

/// <summary>
/// HTTP client that calls SlotService endpoints to manage slot lifecycle.
/// Uses Docker service name "slot-service" as the base URL.
/// </summary>
public class SlotServiceClient(
    HttpClient httpClient,
    ILogger<SlotServiceClient> logger) : ISlotServiceClient
{
    public async Task<bool> ReserveSlotAsync(Guid slotId, Guid correlationId)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/slots/{slotId}/reserve");
            request.Headers.Add("X-Correlation-Id", correlationId.ToString());

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogWarning("ReserveSlot failed for {SlotId}: {Status} {Body}",
                    slotId, response.StatusCode, body);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ReserveSlot HTTP error for slot {SlotId}", slotId);
            return false;
        }
    }

    public async Task<bool> ConfirmSlotAsync(Guid slotId, Guid correlationId)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/slots/{slotId}/confirm");
            request.Headers.Add("X-Correlation-Id", correlationId.ToString());

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogWarning("ConfirmSlot failed for {SlotId}: {Status} {Body}",
                    slotId, response.StatusCode, body);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ConfirmSlot HTTP error for slot {SlotId}", slotId);
            return false;
        }
    }

    public async Task<bool> ReleaseSlotAsync(Guid slotId, Guid correlationId)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/slots/{slotId}/release");
            request.Headers.Add("X-Correlation-Id", correlationId.ToString());

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogWarning("ReleaseSlot failed for {SlotId}: {Status} {Body}",
                    slotId, response.StatusCode, body);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ReleaseSlot HTTP error for slot {SlotId}", slotId);
            return false;
        }
    }
}