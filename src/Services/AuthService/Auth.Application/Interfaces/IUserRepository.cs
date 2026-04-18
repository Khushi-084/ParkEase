using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByUserIdAsync(Guid userId);
    Task<bool>  ExistsByEmailAsync(string email);
    Task        AddAsync(User user);
    Task        SaveChangesAsync();
}