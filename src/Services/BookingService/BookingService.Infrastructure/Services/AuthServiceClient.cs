using System.Net.Http.Json;
using System.Text.Json;

namespace BookingService.Infrastructure.Services;

public interface IAuthServiceClient
{
    Task<UserDetailsResponse?> GetUserDetailsAsync(Guid userId);
}

public record UserDetailsResponse(string Email, string FullName, string Phone);

public class AuthServiceClient(HttpClient client) : IAuthServiceClient
{
    public async Task<UserDetailsResponse?> GetUserDetailsAsync(Guid userId)
    {
        try
        {
            var response = await client.GetAsync($"api/v1/internal/users/{userId}");
            if (!response.IsSuccessStatusCode) return null;

            var user = await response.Content.ReadFromJsonAsync<JsonElement>();
            return new UserDetailsResponse(
                user.GetProperty("email").GetString() ?? "",
                user.GetProperty("fullName").GetString() ?? "",
                user.GetProperty("phone").GetString() ?? ""
            );
        }
        catch 
        {
            return null;
        }
    }
}
