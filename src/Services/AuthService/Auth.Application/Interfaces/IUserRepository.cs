using Auth.Domain.Entities;
using Auth.Domain.Enums;

namespace Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<User?>              FindByEmailAsync(string email);
    Task<User?>              FindByUserIdAsync(Guid userId);
    Task<bool>               ExistsByEmailAsync(string email);
    Task<IEnumerable<User>>  GetAllUsersAsync(UserRole? role, bool? isActive); // ✅ NEW
    Task                     AddAsync(User user);
    Task                     DeleteAsync(User user);                           // ✅ NEW
    Task                     SaveChangesAsync();
}